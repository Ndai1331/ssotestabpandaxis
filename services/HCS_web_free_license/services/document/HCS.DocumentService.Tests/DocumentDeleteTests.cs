using System.Security.Claims;
using HCS.DocumentService.Documents;
using HCS.DocumentService.Signing;
using HCS.DocumentService.Storage;
using HCS.DocumentService.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace HCS.DocumentService.Tests;

public sealed class DocumentDeleteTests
{
    private static readonly DateTime Now = new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
    private const string Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Delete_removes_rows_and_enqueues_blobs_without_waiting_for_storage()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DocumentServiceDbContext(options);
        db.Database.EnsureCreated();

        var userId = Guid.NewGuid();
        var document = new DocumentAggregate(Guid.NewGuid(), "CV-DEL", "To delete", null, userId, Now);
        var file = document.AddFile(Guid.NewGuid(), "a.pdf", "application/pdf", 12, Sha256, "documents/a", userId, Now);
        db.Documents.Add(document);
        var attempt = new SigningAttempt(Guid.NewGuid(), document.Id, file.Id, userId, SigningKind.Electronic,
            Sha256, "idem-del", Now);
        attempt.Complete(Sha256, "signing/out", Now);
        db.SigningAttempts.Add(attempt);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var cleanup = new RecordingBlobCleanup();
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString("D"))
                    ],
                    authenticationType: "test"))
            }
        };
        var service = new DocumentAppService(db, accessor, cleanup);

        await service.DeleteAsync(document.Id);

        Assert.False(await db.Documents.AnyAsync(x => x.Id == document.Id));
        Assert.False(await db.DocumentFiles.AnyAsync(x => x.DocumentId == document.Id));
        Assert.False(await db.SigningAttempts.AnyAsync(x => x.DocumentId == document.Id));
        Assert.Equal(["documents/a"], cleanup.DocumentBlobNames);
        Assert.Equal(["signing/out"], cleanup.SigningBlobNames);
    }

    [Fact]
    public async Task Delete_submission_removes_an_untouched_running_workflow()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var (document, instance) = await SeedRunningSubmissionAsync(db, userId);
        var service = CreateService(db, userId);

        await service.DeleteSubmissionAsync(document.Id);

        Assert.False(await db.Documents.AnyAsync(x => x.Id == document.Id));
        Assert.False(await db.WorkflowInstances.AnyAsync(x => x.Id == instance.Id));
        Assert.False(await db.ApprovalTasks.AnyAsync(x => x.InstanceId == instance.Id));
    }

    [Fact]
    public async Task Delete_submission_rejects_a_started_workflow()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var (document, instance) = await SeedRunningSubmissionAsync(db, userId);
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps)
            .SingleAsync(x => x.Id == instance.DefinitionId);
        var existingTaskIds = instance.Tasks.Select(x => x.Id).ToHashSet();
        instance.Decide(instance.Tasks.Single().Id, true, userId, null, "decision",
            definition.Steps.OrderBy(x => x.Order).ToList(), Now);
        WorkflowAppService.TrackNewApprovalTasks(db, instance, existingTaskIds);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db, userId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteSubmissionAsync(document.Id));

        Assert.Equal("Document:CannotDeleteStartedSubmission", exception.Code);
        Assert.True(await db.Documents.AnyAsync(x => x.Id == document.Id));
    }

    [Fact]
    public async Task Delete_document_still_rejects_a_running_workflow()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var (document, _) = await SeedRunningSubmissionAsync(db, userId);
        var service = CreateService(db, userId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(document.Id));

        Assert.Equal("Document:CannotDeleteWithRunningWorkflow", exception.Code);
        Assert.True(await db.Documents.AnyAsync(x => x.Id == document.Id));
    }

    private static DocumentServiceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new DocumentServiceDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static DocumentAppService CreateService(DocumentServiceDbContext db, Guid userId) =>
        new(db, new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString("D"))],
                    authenticationType: "test"))
            }
        }, new RecordingBlobCleanup());

    private static async Task<(DocumentAggregate Document, WorkflowInstance Instance)> SeedRunningSubmissionAsync(
        DocumentServiceDbContext db, Guid userId)
    {
        var document = new DocumentAggregate(Guid.NewGuid(), "WF-DEL", "Submission", null, userId, Now,
            DocumentSourceType.Workflow);
        document.SetWorkflowSubmitter(userId);
        var definition = new WorkflowDefinition(Guid.NewGuid(), "sign", "Sign",
        [
            new WorkflowStepInput("sign", "Sign", 1, "Documents.Approve", "SIGN")
        ], Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), document.Id, definition, $"start-{document.Id:N}", Now);
        db.Documents.Add(document);
        db.WorkflowDefinitions.Add(definition);
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        instance = await db.WorkflowInstances.Include(x => x.Tasks).SingleAsync(x => x.Id == instance.Id);
        return (document, instance);
    }

    private sealed class RecordingBlobCleanup : IDocumentBlobCleanup
    {
        public IReadOnlyList<string> DocumentBlobNames { get; private set; } = [];
        public IReadOnlyList<string> SigningBlobNames { get; private set; } = [];

        public void Enqueue(IEnumerable<string> documentBlobNames, IEnumerable<string> signingBlobNames)
        {
            DocumentBlobNames = documentBlobNames.ToArray();
            SigningBlobNames = signingBlobNames.ToArray();
        }
    }
}

using System.Security.Claims;
using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Users;

namespace HCS.WorkManagementService.Tests;

public sealed class DocumentLinkedTaskTests
{
    [Fact]
    public async Task Summaries_group_duplicate_links_and_keep_tasks_the_viewer_does_not_own()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var viewer = Guid.NewGuid();
        var other = Guid.NewGuid();
        var project = new Project(Guid.NewGuid(), "P-1", "Project", DateTime.UtcNow, DateTime.UtcNow.AddDays(2), "Active", null, other);
        db.Projects.Add(project);
        var documentA = Guid.NewGuid();
        var documentB = Guid.NewGuid();
        var older = CreateTask(project.Id, "T-OLD", "Older task", other, "New", DateTime.UtcNow.AddMinutes(-10));
        var completed = CreateTask(project.Id, "T-DONE", "Finished task", other, WorkConsts.CompletedStatus, DateTime.UtcNow);
        db.ProjectTasks.AddRange(older, completed);
        db.ProjectTaskDocuments.AddRange(
            new ProjectTaskDocument(Guid.NewGuid(), completed.Id, documentA, "VB-1", other),
            new ProjectTaskDocument(Guid.NewGuid(), completed.Id, documentA, "VB-1", other),
            new ProjectTaskDocument(Guid.NewGuid(), older.Id, documentA, "VB-1", other),
            new ProjectTaskDocument(Guid.NewGuid(), older.Id, documentB, "VB-2", other));
        await db.SaveChangesAsync(ct);

        var summaries = await CreateService(db, viewer).GetByDocumentsAsync([documentA, Guid.Empty, documentA], ct);

        Assert.Equal(2, summaries.Count);
        Assert.All(summaries, item => Assert.Equal(documentA, item.DocumentId));
        Assert.Equal(completed.Id, summaries[0].Id);
        Assert.Equal(WorkConsts.CompletedStatus, summaries[0].Status);
        Assert.Equal(100, summaries[0].ProgressPercent);
        Assert.Equal(older.Id, summaries[1].Id);
    }

    [Fact]
    public async Task More_than_one_page_of_document_ids_is_rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var ids = Enumerable.Range(0, ProjectTaskAppService.MaxDocumentIds + 1).Select(_ => Guid.NewGuid()).ToArray();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService(db, Guid.NewGuid()).GetByDocumentsAsync(ids, ct));

        Assert.Equal("Work:TooManyDocumentIds", exception.Code);
    }

    private static ProjectTaskAppService CreateService(WorkManagementDbContext db, Guid userId) =>
        new(db, new WorkRecordAuthorization(db, new TestCurrentUser(userId)), null!);

    private static ProjectTask CreateTask(Guid projectId, string code, string title, Guid creatorId, string status, DateTime createdAt)
    {
        var task = new ProjectTask(Guid.NewGuid(), projectId, null, code, title, null,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Normal", status, status == WorkConsts.CompletedStatus ? 40 : 0);
        task.SetCreatedBy(creatorId);
        task.CreationTime = createdAt;
        return task;
    }

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? Id => userId;
        public string? UserName => "viewer";
        public string? Name => "Viewer";
        public string? SurName => null;
        public string? Email => null;
        public bool EmailVerified => false;
        public string? PhoneNumber => null;
        public bool PhoneNumberVerified => false;
        public Guid? TenantId => null;
        public string[] Roles => [];
        public Claim? FindClaim(string claimType) => null;
        public Claim[] FindClaims(string claimType) => [];
        public Claim[] GetAllClaims() => [];
        public bool IsInRole(string roleName) => false;
    }
}

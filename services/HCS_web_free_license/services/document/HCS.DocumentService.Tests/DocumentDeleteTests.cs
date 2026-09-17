using System.Security.Claims;
using HCS.DocumentService.Documents;
using HCS.DocumentService.Signing;
using HCS.DocumentService.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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

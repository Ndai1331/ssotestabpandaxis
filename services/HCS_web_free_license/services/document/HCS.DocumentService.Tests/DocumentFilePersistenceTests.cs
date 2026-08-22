using HCS.DocumentService.Documents;
using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Tests;

public sealed class DocumentFilePersistenceTests
{
    private static readonly DateTime Now = new(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Uploading_to_a_loaded_document_tracks_new_file_and_history_as_added()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new DocumentServiceDbContext(options);
        db.Database.EnsureCreated();

        var document = new DocumentAggregate(Guid.NewGuid(), "WF-001", "Workflow", null, Guid.NewGuid(), Now,
            DocumentSourceType.Workflow);
        document.AddFile(Guid.NewGuid(), "template.pdf", "application/pdf", 10, new string('a', 64), "documents/template", null, Now);
        db.Documents.Add(document);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await db.Documents.Include(x => x.Files).Include(x => x.History)
            .SingleAsync(x => x.Id == document.Id);
        var existingFileIds = loaded.Files.Select(x => x.Id).ToHashSet();
        var existingHistoryIds = loaded.History.Select(x => x.Id).ToHashSet();
        var uploaded = loaded.AddFile(Guid.NewGuid(), "photo.jpg", "image/jpeg", 10, new string('b', 64),
            "documents/photo", Guid.NewGuid(), Now.AddMinutes(1));

        DocumentFileService.TrackNewChildren(db, loaded, existingFileIds, existingHistoryIds);

        Assert.Equal(EntityState.Added, db.Entry(uploaded).State);
        Assert.Contains(loaded.History, history => history.Action == "FileAdded" &&
            db.Entry(history).State == EntityState.Added);
        await db.SaveChangesAsync();

        var saved = await db.Documents.AsNoTracking().Include(x => x.Files).Include(x => x.History)
            .SingleAsync(x => x.Id == document.Id);
        Assert.Equal(2, saved.Files.Count);
        Assert.Equal(2, saved.History.Count(x => x.Action == "FileAdded"));
    }
}

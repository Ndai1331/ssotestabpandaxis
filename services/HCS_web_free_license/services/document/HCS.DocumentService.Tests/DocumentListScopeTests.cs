using System.Security.Claims;
using HCS.DocumentService.Documents;
using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Tests;

public sealed class DocumentListScopeTests
{
    private static readonly DateTime Now = new(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Archive_list_is_empty_for_view_only_employees()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        await using var db = CreateDb(
            Archive("mine", user),
            Archive("theirs", other),
            Personal("personal", user));

        var ids = await DocumentAccess.FilterBySource(db.Documents, 0, user, mine: false, Viewer())
            .Select(x => x.Number).ToListAsync();

        Assert.Empty(ids);
    }

    [Fact]
    public async Task Archive_list_returns_every_archive_document_for_managers()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        await using var db = CreateDb(
            Archive("mine", user),
            Archive("theirs", other),
            Personal("personal", user));

        var ids = await DocumentAccess.FilterBySource(db.Documents, 0, user, mine: false, Manager())
            .Select(x => x.Number).OrderBy(x => x).ToListAsync();

        Assert.Equal(["mine", "theirs"], ids);
    }

    [Fact]
    public async Task Personal_list_returns_only_documents_created_by_the_current_user()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var assignedToMe = Personal("assigned", other);
        assignedToMe.Assign(Guid.NewGuid(), user, "VIEW", other, Now);
        await using var db = CreateDb(
            Personal("mine", user),
            Personal("theirs", other),
            assignedToMe,
            Archive("archive", user));

        var ids = await DocumentAccess.FilterBySource(db.Documents, 1, user, mine: true, Viewer())
            .Select(x => x.Number).ToListAsync();

        Assert.Equal(["mine"], ids);
    }

    [Fact]
    public async Task Sent_to_me_list_returns_current_inbox_views()
    {
        var user = Guid.NewGuid();
        var sender = Guid.NewGuid();
        var received = Archive("received", sender);
        received.Send(user, null, sender, Now);
        var revoked = Archive("revoked", sender);
        revoked.Send(user, null, sender, Now);
        revoked.RevokeInbox(sender, Now.AddMinutes(1));
        var mine = Personal("mine", user);
        await using var db = CreateDb(received, revoked, mine);

        var ids = await DocumentAccess.FilterBySource(db.Documents, 2, user, mine: false, Viewer())
            .Select(x => x.Number).ToListAsync();

        Assert.Equal(["received"], ids);
    }

    [Fact]
    public void Archive_documents_are_viewable_by_creators_managers_or_inbox_recipients()
    {
        var owner = Guid.NewGuid();
        var viewer = Guid.NewGuid();
        var document = Archive("cv", owner);
        Assert.True(DocumentAccess.CanView(document, owner, Viewer()));
        Assert.False(DocumentAccess.CanView(document, viewer, Viewer()));
        Assert.True(DocumentAccess.CanView(document, viewer, Manager()));
        Assert.False(DocumentAccess.CanManage(document, viewer, Viewer()));
        Assert.True(DocumentAccess.CanManage(document, viewer, WithPermission(DocumentPermissions.Update)));
        document.Send(viewer, null, owner, Now);
        Assert.True(DocumentAccess.CanView(document, viewer, Viewer()));
    }

    [Fact]
    public void Personal_documents_are_viewable_only_by_the_creator_or_inbox_recipient()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var document = Personal("mine", owner);
        Assert.True(DocumentAccess.CanView(document, owner, Viewer()));
        Assert.False(DocumentAccess.CanView(document, other, Viewer()));
        document.Send(recipient, null, owner, Now);
        Assert.True(DocumentAccess.CanView(document, recipient, Viewer()));
        Assert.False(DocumentAccess.CanManage(document, recipient, WithPermission(DocumentPermissions.Update)));
        Assert.True(DocumentAccess.CanSend(document, recipient, Viewer()));
        Assert.True(DocumentAccess.CanInboxViewAssign(document, recipient, "VIEW"));
        Assert.False(DocumentAccess.CanInboxViewAssign(document, recipient, "reviewer"));
        Assert.False(DocumentAccess.CanSend(document, other, Viewer()));
        Assert.True(DocumentAccess.CanManage(document, owner, Viewer()));
    }

    private static DocumentAggregate Archive(string number, Guid creator) =>
        new(Guid.NewGuid(), number, number, null, creator, Now, DocumentSourceType.Archive);

    private static DocumentAggregate Personal(string number, Guid creator) =>
        new(Guid.NewGuid(), number, number, null, creator, Now, DocumentSourceType.Personal);

    private static DocumentServiceDbContext CreateDb(params DocumentAggregate[] documents)
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new DocumentServiceDbContext(options);
        db.Database.EnsureCreated();
        db.Documents.AddRange(documents);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
    }

    private static ClaimsPrincipal Viewer() => WithPermission(DocumentPermissions.View);

    private static ClaimsPrincipal Manager() => WithPermission(DocumentPermissions.Create);

    private static ClaimsPrincipal WithPermission(string permission) => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("permission", permission)
        ],
        "test", ClaimTypes.Name, ClaimTypes.Role));
}

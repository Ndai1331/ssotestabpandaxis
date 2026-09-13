using System.Security.Claims;
using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;
using Volo.Abp.Users;

namespace HCS.WorkManagementService.Tests;

public sealed class EventAppServiceTests
{
    [Fact]
    public async Task Public_check_in_requires_an_authenticated_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var currentUser = new TestCurrentUser();
        await using var db = CreateDb();
        var item = CreateEvent();
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, currentUser);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CheckInPublicAsync(
            item.Code, item.QrToken, new(), cancellationToken));
        var attendees = await db.EventAttendees.Where(x => x.EventId == item.Id).ToListAsync(cancellationToken);

        Assert.Equal("Work:EventCheckInLoginRequired", exception.Code);
        Assert.Empty(attendees);
    }

    [Fact]
    public async Task Authenticated_public_check_in_creates_attendee_linked_to_current_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var currentUser = AuthenticatedUser(userId);
        await using var db = CreateDb();
        var item = CreateEvent();
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, currentUser);

        await service.CheckInPublicAsync(item.Code, item.QrToken,
            new(), cancellationToken);
        var attendee = await db.EventAttendees.SingleAsync(x => x.EventId == item.Id, cancellationToken);

        Assert.Equal(userId, attendee.UserId);
        Assert.Equal("long", attendee.Username);
        Assert.Equal("Nguyen Long", attendee.FullName);
        Assert.Equal("0900000000", attendee.PhoneNumber);
        Assert.Equal("long@example.test", attendee.Email);

        await service.CheckInPublicAsync(item.Code, item.QrToken, new(), cancellationToken);
        Assert.Equal(1, await db.EventAttendees.CountAsync(x => x.EventId == item.Id, cancellationToken));
        Assert.Equal(EventRegistrationStatuses.Confirmed, attendee.RegistrationStatus);
        Assert.Equal(EventCheckInStatuses.CheckedIn, attendee.CheckInStatus);
    }

    [Fact]
    public async Task Public_event_details_include_its_attachments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent();
        db.ManagedEvents.Add(item);
        db.EventAttachments.Add(new EventAttachment(Guid.NewGuid(), item.Id, Guid.NewGuid(),
            $"events/{item.Id:N}/file", "agenda.pdf", "application/pdf", 2048));
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, new TestCurrentUser());

        var result = await service.GetPublicAsync(item.Code, item.QrToken, cancellationToken);

        var attachment = Assert.Single(result.Attachments);
        Assert.Equal("agenda.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal(2048, attachment.Size);
        Assert.Equal(ManagedEventStatuses.Ongoing, result.Status);
        Assert.Equal("Agenda notes", result.Content);
        Assert.Equal("Bring badge", result.Description);
    }

    [Fact]
    public async Task Public_confirm_on_preparing_event_does_not_check_in()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        var item = CreateEvent(ManagedEventStatuses.Preparing);
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(userId));

        var result = await service.ConfirmPublicAsync(item.Code, item.QrToken, new(), cancellationToken);
        var attendee = await db.EventAttendees.SingleAsync(x => x.EventId == item.Id, cancellationToken);

        Assert.Equal("Nguyen Long", result.FullName);
        Assert.Equal(EventRegistrationStatuses.Confirmed, result.RegistrationStatus);
        Assert.Equal(EventRegistrationStatuses.Confirmed, attendee.RegistrationStatus);
        Assert.Equal(EventCheckInStatuses.NotCheckedIn, attendee.CheckInStatus);
        Assert.Null(attendee.CheckedInAt);
    }

    [Fact]
    public async Task Public_decline_on_preparing_event_does_not_check_in()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        var item = CreateEvent(ManagedEventStatuses.Preparing);
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(userId));

        var result = await service.DeclinePublicAsync(item.Code, item.QrToken, new(), cancellationToken);
        var attendee = await db.EventAttendees.SingleAsync(x => x.EventId == item.Id, cancellationToken);

        Assert.Equal("Nguyen Long", result.FullName);
        Assert.Equal(EventRegistrationStatuses.Declined, result.RegistrationStatus);
        Assert.Equal(EventRegistrationStatuses.Declined, attendee.RegistrationStatus);
        Assert.Equal(EventCheckInStatuses.NotCheckedIn, attendee.CheckInStatus);
        Assert.Null(attendee.CheckedInAt);
    }

    [Fact]
    public async Task Public_decline_can_replace_a_previous_confirmation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent(ManagedEventStatuses.Preparing);
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        await service.ConfirmPublicAsync(item.Code, item.QrToken, new(), cancellationToken);
        var result = await service.DeclinePublicAsync(item.Code, item.QrToken, new(), cancellationToken);
        var attendee = await db.EventAttendees.SingleAsync(x => x.EventId == item.Id, cancellationToken);

        Assert.Equal(EventRegistrationStatuses.Declined, result.RegistrationStatus);
        Assert.Equal(EventRegistrationStatuses.Declined, attendee.RegistrationStatus);
        Assert.Equal(EventCheckInStatuses.NotCheckedIn, attendee.CheckInStatus);
    }

    [Fact]
    public async Task Public_confirm_is_rejected_when_event_is_ongoing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent();
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ConfirmPublicAsync(item.Code, item.QrToken, new(), cancellationToken));

        Assert.Equal("Work:EventConfirmNotOpen", exception.Code);
        Assert.Empty(await db.EventAttendees.Where(x => x.EventId == item.Id).ToListAsync(cancellationToken));
    }

    [Fact]
    public async Task Public_check_in_is_rejected_when_event_is_preparing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent(ManagedEventStatuses.Preparing);
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CheckInPublicAsync(item.Code, item.QrToken, new(), cancellationToken));

        Assert.Equal("Work:EventCheckInNotOpen", exception.Code);
        Assert.Empty(await db.EventAttendees.Where(x => x.EventId == item.Id).ToListAsync(cancellationToken));
    }

    private static EventAppService CreateService(WorkManagementDbContext db, ICurrentUser currentUser) =>
        new(db, new WorkRecordAuthorization(db, currentUser),
            NullLogger<EventAppService>.Instance, currentUser);

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ManagedEvent CreateEvent(string status = ManagedEventStatuses.Ongoing) =>
        new(Guid.NewGuid(), "EVT-TEST", "General", "Test event", "Agenda notes", "Bring badge", null,
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), status, "qr-token", Guid.NewGuid());

    private static TestCurrentUser AuthenticatedUser(Guid userId) => new()
    {
        IsAuthenticated = true,
        Id = userId,
        UserName = "long",
        Name = "Long",
        SurName = "Nguyen",
        Email = "long@example.test",
        PhoneNumber = "0900000000"
    };

    private sealed class TestCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }
        public Guid? Id { get; init; }
        public string? UserName { get; init; }
        public string? Name { get; init; }
        public string? SurName { get; init; }
        public string? PhoneNumber { get; init; }
        public bool PhoneNumberVerified => false;
        public string? Email { get; init; }
        public bool EmailVerified => false;
        public Guid? TenantId => null;
        public string[] Roles => [];
        public Claim? FindClaim(string claimType) => null;
        public Claim[] FindClaims(string claimType) => [];
        public Claim[] GetAllClaims() => [];
        public bool IsInRole(string roleName) => false;
    }
}

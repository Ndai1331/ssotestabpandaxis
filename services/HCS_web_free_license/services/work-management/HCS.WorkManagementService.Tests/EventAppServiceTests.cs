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
    public async Task Guest_public_check_in_creates_attendee_without_user_id()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent();
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, new TestCurrentUser());
        var input = new PublicEventCheckInDto("Tran Khach", "0912345678", "khach@example.test", "From QR");

        await service.CheckInPublicAsync(item.Code, item.QrToken, input, cancellationToken);
        var attendee = await db.EventAttendees.SingleAsync(x => x.EventId == item.Id, cancellationToken);

        Assert.Null(attendee.UserId);
        Assert.Equal("Tran Khach", attendee.FullName);
        Assert.Equal("0912345678", attendee.PhoneNumber);
        Assert.Equal("khach@example.test", attendee.Email);
        Assert.Equal("From QR", attendee.Note);
        Assert.Equal(EventCheckInStatuses.CheckedIn, attendee.CheckInStatus);

        await service.CheckInPublicAsync(item.Code, item.QrToken, input, cancellationToken);
        Assert.Equal(1, await db.EventAttendees.CountAsync(x => x.EventId == item.Id, cancellationToken));
    }

    [Fact]
    public async Task Guest_public_check_in_does_not_link_an_authenticated_user()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent();
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        await service.CheckInPublicAsync(item.Code, item.QrToken,
            new("Guest Name", "0987654321", "guest@example.test"), cancellationToken);
        var attendee = await db.EventAttendees.SingleAsync(x => x.EventId == item.Id, cancellationToken);

        Assert.Null(attendee.UserId);
        Assert.Equal("Guest Name", attendee.FullName);
    }

    [Fact]
    public async Task Get_public_returns_guest_attendance_status_by_phone_and_email()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateUpcomingEvent(ManagedEventStatuses.Preparing);
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, new TestCurrentUser());
        var input = new PublicEventCheckInDto("Tran Khach", "0912345678", "khach@example.test");

        await service.ConfirmPublicAsync(item.Code, item.QrToken, input, cancellationToken);
        var result = await service.GetPublicAsync(item.Code, item.QrToken, "0912345678", "khach@example.test",
            cancellationToken);

        Assert.Equal(EventRegistrationStatuses.Confirmed, result.RegistrationStatus);
        Assert.Equal(EventCheckInStatuses.NotCheckedIn, result.CheckInStatus);
    }

    [Fact]
    public async Task Incomplete_guest_fields_are_rejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent();
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, new TestCurrentUser());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CheckInPublicAsync(
            item.Code, item.QrToken, new("Only name"), cancellationToken));

        Assert.Equal("Work:EventAttendeeRequiredFields", exception.Code);
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
        var item = CreateUpcomingEvent(ManagedEventStatuses.Preparing);
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
        var item = CreateUpcomingEvent(ManagedEventStatuses.Preparing);
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
        var item = CreateUpcomingEvent(ManagedEventStatuses.Preparing);
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
        var item = CreateUpcomingEvent(ManagedEventStatuses.Preparing);
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CheckInPublicAsync(item.Code, item.QrToken, new(), cancellationToken));

        Assert.Equal("Work:EventCheckInNotOpen", exception.Code);
        Assert.Empty(await db.EventAttendees.Where(x => x.EventId == item.Id).ToListAsync(cancellationToken));
    }

    [Fact]
    public async Task Public_check_in_opens_when_start_time_arrives()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent(ManagedEventStatuses.Preparing, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddHours(1));
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        await service.CheckInPublicAsync(item.Code, item.QrToken, new(), cancellationToken);

        Assert.Equal(ManagedEventStatuses.Ongoing, item.Status);
        Assert.Equal(EventCheckInStatuses.CheckedIn,
            (await db.EventAttendees.SingleAsync(x => x.EventId == item.Id, cancellationToken)).CheckInStatus);
    }

    [Fact]
    public async Task Public_check_in_stays_closed_when_a_cancelled_event_reaches_start_time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent(ManagedEventStatuses.Cancelled, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddHours(1));
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CheckInPublicAsync(item.Code, item.QrToken, new(), cancellationToken));

        Assert.Equal("Work:EventCheckInNotOpen", exception.Code);
        Assert.Equal(ManagedEventStatuses.Cancelled, item.Status);
        Assert.Empty(await db.EventAttendees.Where(x => x.EventId == item.Id).ToListAsync(cancellationToken));
    }

    [Fact]
    public async Task Get_event_completes_status_after_end_time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var item = CreateEvent(ManagedEventStatuses.Ongoing, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddMinutes(-1));
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, AuthenticatedUser(Guid.NewGuid()));

        var result = await service.GetAsync(item.Id, cancellationToken);

        Assert.Equal(ManagedEventStatuses.Completed, result.Status);
        Assert.Equal(ManagedEventStatuses.Completed, item.Status);
    }

    private static EventAppService CreateService(WorkManagementDbContext db, ICurrentUser currentUser) =>
        new(db, new WorkRecordAuthorization(db, currentUser),
            NullLogger<EventAppService>.Instance, currentUser, new ManagedEventStatusSynchronizer(db));

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ManagedEvent CreateUpcomingEvent(string status) =>
        CreateEvent(status, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));

    private static ManagedEvent CreateEvent(string status = ManagedEventStatuses.Ongoing,
        DateTime? startTime = null, DateTime? endTime = null)
    {
        var start = startTime ?? DateTime.UtcNow;
        return new(Guid.NewGuid(), "EVT-TEST", "General", "Test event", "Agenda notes", "Bring badge", null,
            start, endTime ?? start.AddHours(1), status, "qr-token", Guid.NewGuid());
    }

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

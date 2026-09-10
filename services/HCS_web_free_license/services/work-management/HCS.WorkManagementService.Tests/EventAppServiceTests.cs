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
        var currentUser = new TestCurrentUser
        {
            IsAuthenticated = true,
            Id = userId,
            UserName = "long",
            Name = "Long",
            SurName = "Nguyen",
            Email = "long@example.test",
            PhoneNumber = "0900000000"
        };
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

    private static EventAppService CreateService(WorkManagementDbContext db, ICurrentUser currentUser) =>
        new(db, new WorkRecordAuthorization(db, currentUser),
            NullLogger<EventAppService>.Instance, currentUser);

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ManagedEvent CreateEvent() =>
        new(Guid.NewGuid(), "EVT-TEST", "General", "Test event", null, null, null,
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), ManagedEventStatuses.Ongoing, "qr-token", Guid.NewGuid());

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

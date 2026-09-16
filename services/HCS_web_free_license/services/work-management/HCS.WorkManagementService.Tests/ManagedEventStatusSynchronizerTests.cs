using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HCS.WorkManagementService.Tests;

public sealed class ManagedEventStatusSynchronizerTests
{
    [Fact]
    public async Task Synchronize_moves_started_events_to_ongoing_and_ended_events_to_completed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var started = CreateEvent("EVT-START", ManagedEventStatuses.Preparing,
            DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddHours(1));
        var ended = CreateEvent("EVT-END", ManagedEventStatuses.Ongoing,
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddMinutes(-1));
        var cancelled = CreateEvent("EVT-CANCEL", ManagedEventStatuses.Cancelled,
            DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddHours(1));
        var upcoming = CreateEvent("EVT-NEXT", ManagedEventStatuses.Preparing,
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        db.ManagedEvents.AddRange(started, ended, cancelled, upcoming);
        await db.SaveChangesAsync(cancellationToken);

        var changed = await new ManagedEventStatusSynchronizer(db).SynchronizeAsync(cancellationToken);

        Assert.Equal(2, changed);
        Assert.Equal(ManagedEventStatuses.Ongoing, started.Status);
        Assert.Equal(ManagedEventStatuses.Completed, ended.Status);
        Assert.Equal(ManagedEventStatuses.Cancelled, cancelled.Status);
        Assert.Equal(ManagedEventStatuses.Preparing, upcoming.Status);
    }

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ManagedEvent CreateEvent(string code, string status, DateTime start, DateTime end) =>
        new(Guid.NewGuid(), code, "General", code, null, null, null, start, end, status, "token", Guid.NewGuid());
}

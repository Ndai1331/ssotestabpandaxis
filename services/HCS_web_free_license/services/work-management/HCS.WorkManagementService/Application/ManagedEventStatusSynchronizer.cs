using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace HCS.WorkManagementService.Application;

public sealed class ManagedEventStatusSynchronizer(WorkManagementDbContext db) : ITransientDependency
{
    public async Task<int> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await db.ManagedEvents
            .Where(x => x.Status != ManagedEventStatuses.Cancelled
                        && x.Status != ManagedEventStatuses.Completed
                        && (x.EndTime <= now
                            || (x.Status == ManagedEventStatuses.Preparing && x.StartTime <= now)))
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var item in items)
        {
            if (item.ApplyScheduledStatus(now))
                changed++;
        }

        if (changed > 0)
            await db.SaveChangesAsync(cancellationToken);

        return changed;
    }

    public async Task EnsureCurrentAsync(ManagedEvent item, CancellationToken cancellationToken = default)
    {
        if (!item.ApplyScheduledStatus(DateTime.UtcNow))
            return;
        await db.SaveChangesAsync(cancellationToken);
    }
}

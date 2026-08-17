using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DistributedEvents;

namespace hanhchinhso.WorkflowService.Data;

[ConnectionStringName(DatabaseName)]
public class WorkflowServiceDbContext : AbpDbContext<WorkflowServiceDbContext>, IHasEventInbox, IHasEventOutbox
{
    public const string DatabaseName = "WorkflowService";

    public DbSet<IncomingEventRecord> IncomingEvents { get; set; }
    public DbSet<OutgoingEventRecord> OutgoingEvents { get; set; }

    public WorkflowServiceDbContext(DbContextOptions<WorkflowServiceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureEventInbox();
        builder.ConfigureEventOutbox();
    }
}

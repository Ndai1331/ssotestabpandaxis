using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DistributedEvents;
using Volo.AIManagement.ApplicationWorkspaceProviders;
using Volo.AIManagement.EntityFrameworkCore;
using Volo.AIManagement.Workspaces;
using Volo.AIManagement.WorkspaceDataSources;
using Volo.AIManagement.DocumentChunks;
using Volo.AIManagement.McpServers;

namespace hanhchinhso.AIManagementService.Data;

[ConnectionStringName(DatabaseName)]
[ReplaceDbContext(typeof(IAIManagementDbContext))]
public class AIManagementServiceDbContext :
    AbpDbContext<AIManagementServiceDbContext>,
    IAIManagementDbContext,
    IHasEventInbox,
    IHasEventOutbox
{
    public const string DbTablePrefix = "";
    public const string DbSchema = null;
    
    public const string DatabaseName = "AIManagementService";
    
    public DbSet<IncomingEventRecord> IncomingEvents { get; set; }
    public DbSet<OutgoingEventRecord> OutgoingEvents { get; set; }

    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<ApplicationAIProvider> ApplicationWorkspaceProviders { get; set; }
    public DbSet<WorkspaceDataSource> WorkspaceDataSources { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<McpServer> McpServers { get; set; }

    public AIManagementServiceDbContext(DbContextOptions<AIManagementServiceDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureEventInbox();
        builder.ConfigureEventOutbox();
        builder.ConfigureAIManagement();
    }
}

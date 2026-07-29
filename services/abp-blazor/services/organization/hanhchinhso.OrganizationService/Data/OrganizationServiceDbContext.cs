using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DistributedEvents;
using Volo.Abp.EntityFrameworkCore.Modeling;
using hanhchinhso.OrganizationService.MasterData;
using hanhchinhso.OrganizationService.Organization;

namespace hanhchinhso.OrganizationService.Data;

[ConnectionStringName(DatabaseName)]
public class OrganizationServiceDbContext :
    AbpDbContext<OrganizationServiceDbContext>,
    IHasEventInbox,
    IHasEventOutbox
{
    public const string DbTablePrefix = "";
    public const string DbSchema = null;
    
    public const string DatabaseName = "OrganizationService";
    
    public DbSet<IncomingEventRecord> IncomingEvents { get; set; }
    public DbSet<OutgoingEventRecord> OutgoingEvents { get; set; }

    public DbSet<MasterDataItem> MasterDataItems => Set<MasterDataItem>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Position> Positions => Set<Position>();

    public OrganizationServiceDbContext(DbContextOptions<OrganizationServiceDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureEventInbox();
        builder.ConfigureEventOutbox();
        builder.Entity<MasterDataItem>(entity =>
        {
            entity.ToTable("OrganizationMasterData");
            entity.ConfigureByConvention();
            entity.Property(x => x.Type).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Code).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(256);
            entity.Property(x => x.SortOrder).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Type, x.Code })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(x => new { x.Type, x.Code })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"IsDeleted\" = false");
        });
        ConfigureCodedAggregate<Unit>(builder, "OrganizationUnits");
        ConfigureCodedAggregate<Position>(builder, "OrganizationPositions");
    }

    private static void ConfigureCodedAggregate<TEntity>(ModelBuilder builder, string tableName)
        where TEntity : CodedOrganizationAggregate
    {
        builder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.ConfigureByConvention();
            entity.Property(x => x.Code).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(256);
            entity.HasIndex(x => new { x.TenantId, x.Code })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(x => x.Code)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"IsDeleted\" = false");
        });
    }
}

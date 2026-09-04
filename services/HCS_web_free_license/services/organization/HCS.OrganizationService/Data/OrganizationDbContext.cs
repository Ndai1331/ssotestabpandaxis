using HCS.OrganizationService.Domain;
using HCS.OrganizationService.Integration;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace HCS.OrganizationService.Data;

[ConnectionStringName(ConnectionStringName)]
public sealed class OrganizationDbContext : AbpDbContext<OrganizationDbContext>
{
    public const string ConnectionStringName = "Organization";
    public const string Schema = "hcs_organization";

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<MasterDataItem> MasterDataItems => Set<MasterDataItem>();
    public DbSet<Icd10> Icd10s => Set<Icd10>();
    public DbSet<BloodPressureRange> BloodPressureRanges => Set<BloodPressureRange>();
    public DbSet<BloodGlucoseRange> BloodGlucoseRanges => Set<BloodGlucoseRange>();
    public DbSet<BmiRange> BmiRanges => Set<BmiRange>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Commune> Communes => Set<Commune>();
    public DbSet<UserOrganizationMapping> UserOrganizationMappings => Set<UserOrganizationMapping>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        ConfigureCoded<Department>(builder, "Departments");
        ConfigureCoded<Unit>(builder, "Units");
        ConfigureCoded<Position>(builder, "Positions");
        ConfigureCoded<MasterDataItem>(builder, "MasterDataItems", uniqueCode: false);
        ConfigureCodedReference<Icd10>(builder, "Icd10");
        ConfigureTitledReference<BloodPressureRange>(builder, "BloodPressureRanges");
        ConfigureTitledReference<BloodGlucoseRange>(builder, "BloodGlucoseRanges");
        ConfigureTitledReference<BmiRange>(builder, "BmiRanges");
        ConfigureCodedReference<Country>(builder, "Countries");
        ConfigureCodedReference<Province>(builder, "Provinces");
        ConfigureCodedReference<Commune>(builder, "Communes");

        builder.Entity<Icd10>(b =>
        {
            b.Property(x => x.DiseaseGroup).IsRequired().HasMaxLength(OrganizationConsts.MaxDiseaseGroupLength);
            b.Property(x => x.IsChronic).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
        });
        builder.Entity<BloodPressureRange>(b =>
        {
            b.Property(x => x.HATTMin).IsRequired();
            b.Property(x => x.HATTMax).IsRequired();
            b.Property(x => x.HATTrMin).IsRequired();
            b.Property(x => x.HATTrMax).IsRequired();
        });
        builder.Entity<BloodGlucoseRange>(b =>
        {
            b.Property(x => x.MinValue).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.MaxValue).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.BeforeMeal).IsRequired();
        });
        builder.Entity<BmiRange>(b =>
        {
            b.Property(x => x.Gender).IsRequired().HasMaxLength(OrganizationConsts.MaxGenderLength);
            b.Property(x => x.MinValue).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.MaxValue).HasPrecision(18, 2).IsRequired();
        });
        builder.Entity<Country>(b =>
        {
            b.Property(x => x.CountryCode).IsRequired().HasMaxLength(OrganizationConsts.MaxCountryCodeLength);
            b.HasIndex(x => x.CountryCode).IsUnique();
        });
        builder.Entity<Province>(b =>
        {
            b.Property(x => x.CountryId).IsRequired();
            b.HasIndex(x => x.CountryId);
            b.HasOne<Country>().WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Commune>(b =>
        {
            b.Property(x => x.ProvinceId).IsRequired();
            b.HasIndex(x => x.ProvinceId);
            b.HasOne<Province>().WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Department>(b =>
        {
            b.Property(x => x.ParentId);
            b.HasIndex(x => x.ParentId);
            b.HasOne<Department>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Unit>(b =>
        {
            b.Property(x => x.DepartmentId).IsRequired();
            b.HasIndex(x => x.DepartmentId);
            b.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Position>(b => b.Property(x => x.SignOrder).IsRequired());
        builder.Entity<MasterDataItem>(b =>
        {
            b.Property(x => x.Type).IsRequired().HasMaxLength(OrganizationConsts.MaxTypeLength);
            b.HasIndex(x => new { x.Type, x.Code }).IsUnique();
        });

        builder.Entity<UserOrganizationMapping>(b =>
        {
            b.ToTable("UserOrganizationMappings");
            b.ConfigureByConvention();
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.DepartmentId).IsRequired();
            b.HasIndex(x => new { x.UserId, x.DepartmentId, x.UnitId, x.PositionId }).IsUnique();
            b.HasIndex(x => x.UserId).IsUnique()
                .HasFilter("\"IsPrimary\" = true");
            b.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne<Position>().WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages"); b.HasKey(x => x.Id);
            b.Property(x => x.EventName).HasMaxLength(512);
            b.Property(x => x.CorrelationId).HasMaxLength(128);
            b.Property(x => x.Payload).HasColumnType("jsonb");
            b.Property(x => x.LastError).HasMaxLength(1000);
            b.HasIndex(x => new { x.PublishedAt, x.DeadLetteredAt, x.LeaseUntil, x.NextAttemptAt, x.CreationTime });
        });
        builder.Entity<InboxMessage>(b =>
        {
            b.ToTable("InboxMessages"); b.HasKey(x => new { x.EventId, x.Handler });
            b.Property(x => x.Handler).HasMaxLength(512);
        });
    }

    private static void ConfigureCoded<TEntity>(ModelBuilder builder, string table, bool uniqueCode = true)
        where TEntity : CodedAggregate
    {
        builder.Entity<TEntity>(b =>
        {
            b.ToTable(table);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(OrganizationConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(OrganizationConsts.MaxNameLength);
            b.Property(x => x.SortOrder).IsRequired();
            if (uniqueCode) b.HasIndex(x => x.Code).IsUnique();
        });
    }

    private static void ConfigureCodedReference<TEntity>(ModelBuilder builder, string table)
        where TEntity : CodedReferenceAggregate
    {
        builder.Entity<TEntity>(b =>
        {
            b.ToTable(table);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(OrganizationConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(OrganizationConsts.MaxNameLength);
            b.Property(x => x.SortOrder).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
        });
    }

    private static void ConfigureTitledReference<TEntity>(ModelBuilder builder, string table)
        where TEntity : TitledReferenceAggregate
    {
        builder.Entity<TEntity>(b =>
        {
            b.ToTable(table);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(OrganizationConsts.MaxTitleLength);
            b.Property(x => x.Description).IsRequired().HasMaxLength(OrganizationConsts.MaxDescriptionLength);
            b.Property(x => x.SortOrder).IsRequired();
        });
    }
}

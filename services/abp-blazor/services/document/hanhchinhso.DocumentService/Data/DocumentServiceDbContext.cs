using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DistributedEvents;
using Volo.Abp.EntityFrameworkCore.Modeling;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Workflows;
using hanhchinhso.DocumentService.Signing;

namespace hanhchinhso.DocumentService.Data;

[ConnectionStringName(DatabaseName)]
public class DocumentServiceDbContext :
    AbpDbContext<DocumentServiceDbContext>,
    IHasEventInbox,
    IHasEventOutbox
{
    public const string DbTablePrefix = "";
    public const string DbSchema = null;
    
    public const string DatabaseName = "DocumentService";
    
    public DbSet<IncomingEventRecord> IncomingEvents { get; set; }
    public DbSet<OutgoingEventRecord> OutgoingEvents { get; set; }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<DocumentBlobCleanup> DocumentBlobCleanups => Set<DocumentBlobCleanup>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowStepTemplate> WorkflowStepTemplates => Set<WorkflowStepTemplate>();
    public DbSet<WorkflowStepAssignmentConfiguration> WorkflowStepAssignmentConfigurations =>
        Set<WorkflowStepAssignmentConfiguration>();
    public DbSet<WorkflowStepAssignmentUser> WorkflowStepAssignmentUsers =>
        Set<WorkflowStepAssignmentUser>();
    public DbSet<WorkflowStepAssignmentOrganizationUnit> WorkflowStepAssignmentOrganizationUnits =>
        Set<WorkflowStepAssignmentOrganizationUnit>();
    public DbSet<DocumentWorkflowInstance> DocumentWorkflowInstances =>
        Set<DocumentWorkflowInstance>();
    public DbSet<DocumentWorkflowCommittedStep> DocumentWorkflowCommittedSteps =>
        Set<DocumentWorkflowCommittedStep>();
    public DbSet<DocumentWorkflowCommittedReceiver> DocumentWorkflowCommittedReceivers =>
        Set<DocumentWorkflowCommittedReceiver>();
    public DbSet<DocumentWorkflowCommittedViewScope> DocumentWorkflowCommittedViewScopes =>
        Set<DocumentWorkflowCommittedViewScope>();
    public DbSet<DocumentAssignment> DocumentAssignments => Set<DocumentAssignment>();
    public DbSet<DocumentWorkflowInstanceLog> DocumentWorkflowInstanceLogs =>
        Set<DocumentWorkflowInstanceLog>();
    public DbSet<DocumentHistory> DocumentHistories => Set<DocumentHistory>();
    public DbSet<SignatureSetting> SignatureSettings => Set<SignatureSetting>();
    public DbSet<UserSignature> UserSignatures => Set<UserSignature>();
    public DbSet<SigningAsset> SigningAssets => Set<SigningAsset>();
    public DbSet<SigningBlobCleanup> SigningBlobCleanups =>
        Set<SigningBlobCleanup>();
    public DbSet<SigningAttempt> SigningAttempts => Set<SigningAttempt>();

    public DocumentServiceDbContext(DbContextOptions<DocumentServiceDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureEventInbox();
        builder.ConfigureEventOutbox();
        builder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.ConfigureByConvention();
            entity.Property(x => x.Number).HasMaxLength(DocumentConsts.NumberMaxLength);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(DocumentConsts.TitleMaxLength);
            entity.Property(x => x.CurrentStatus).HasMaxLength(DocumentConsts.StatusMaxLength);
            entity.Property(x => x.StorageNumber).IsRequired().HasMaxLength(DocumentConsts.StorageNumberMaxLength);
            entity.HasIndex(x => new { x.TenantId, x.Number })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL AND \"Number\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(x => x.Number)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"Number\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(x => new { x.TenantId, x.OrganizationUnitId });
            entity.HasIndex(x => new { x.TenantId, x.SourceType, x.IncomingDate });
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(x => x.ParentDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DocumentFile>(entity =>
        {
            entity.ToTable("DocumentFiles");
            entity.ConfigureByConvention();
            entity.Property(x => x.DisplayName).IsRequired().HasMaxLength(DocumentConsts.FileNameMaxLength);
            entity.Property(x => x.BlobName).IsRequired().HasMaxLength(DocumentConsts.BlobNameMaxLength);
            entity.Property(x => x.MimeType).IsRequired().HasMaxLength(DocumentConsts.MimeTypeMaxLength);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.HasIndex(x => new { x.TenantId, x.DocumentId, x.BlobName }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.BlobDeletionPending, x.LastModificationTime });
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentFile>()
                .WithMany()
                .HasForeignKey(x => x.SourceFileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DocumentBlobCleanup>(entity =>
        {
            entity.ToTable("DocumentBlobCleanups");
            entity.ConfigureByConvention();
            entity.Property(x => x.BlobName)
                .IsRequired()
                .HasMaxLength(DocumentConsts.BlobNameMaxLength);
            entity.HasIndex(x => new { x.TenantId, x.BlobName }).IsUnique();
            entity.HasIndex(x => x.CreationTime);
        });
        ConfigureWorkflowCatalog(builder);
        ConfigureWorkflowRuntime(builder);
        ConfigureSigning(builder);
    }

    private static void ConfigureSigning(ModelBuilder builder)
    {
        builder.Entity<SignatureSetting>(entity =>
        {
            entity.ToTable("SignatureSettings");
            entity.ConfigureByConvention();
            entity.Property(x => x.ProviderCode).IsRequired()
                .HasMaxLength(SigningConsts.ProviderCodeMaxLength);
            entity.Property(x => x.ApiEndpoint).IsRequired()
                .HasMaxLength(SigningConsts.EndpointMaxLength);
            entity.Property(x => x.SignedFileSuffix).IsRequired()
                .HasMaxLength(SigningConsts.SignedFileSuffixMaxLength);
            entity.HasIndex(x => new { x.TenantId, x.ProviderCode })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(x => x.ProviderCode)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"IsDeleted\" = false");
            entity.HasOne<SigningAsset>()
                .WithMany()
                .HasForeignKey(x => x.LayoutAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_SignatureSetting_ProviderType",
                    "\"ProviderType\" IN (0, 1, 2)");
                table.HasCheckConstraint(
                    "CK_SignatureSetting_DefaultSignatureType",
                    "\"DefaultSignatureType\" IN (0, 1)");
                table.HasCheckConstraint(
                    "CK_SignatureSetting_ExecutionLimits",
                    "\"ApiTimeoutSeconds\" BETWEEN 1 AND 600 AND " +
                    "\"SignWidth\" BETWEEN 1 AND 2000 AND " +
                    "\"SignHeight\" BETWEEN 1 AND 2000");
            });
        });
        builder.Entity<UserSignature>(entity =>
        {
            entity.ToTable("UserSignatures");
            entity.ConfigureByConvention();
            entity.Property(x => x.ProviderCode).IsRequired()
                .HasMaxLength(SigningConsts.ProviderCodeMaxLength);
            entity.Property(x => x.TokenReference)
                .HasMaxLength(SigningConsts.TokenReferenceMaxLength);
            entity.Property(x => x.ProtectedSecret).HasColumnType("text");
            entity.HasIndex(x => new
            {
                x.TenantId,
                x.IdentityUserId,
                x.SignatureType,
                x.IsActive
            });
            entity.HasIndex(x => new { x.TenantId, x.ProviderCode });
            entity.HasOne<SignatureSetting>()
                .WithMany()
                .HasForeignKey(x => x.SignatureSettingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SigningAsset>()
                .WithMany()
                .HasForeignKey(x => x.SignatureAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SigningAsset>()
                .WithMany()
                .HasForeignKey(x => x.SealAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_UserSignature_Type",
                    "\"SignatureType\" IN (0, 1)");
                table.HasCheckConstraint(
                    "CK_UserSignature_Validity",
                    "\"ValidFromUtc\" IS NULL OR \"ValidToUtc\" IS NULL OR " +
                    "\"ValidToUtc\" >= \"ValidFromUtc\"");
                table.HasCheckConstraint(
                    "CK_UserSignature_DigitalCredential",
                    "\"SignatureType\" <> 1 OR NOT \"IsActive\" OR " +
                    "(\"TokenReference\" IS NOT NULL AND " +
                    "length(trim(\"TokenReference\")) > 0 AND " +
                    "\"ProtectedSecret\" IS NOT NULL AND " +
                    "length(trim(\"ProtectedSecret\")) > 0)");
                table.HasCheckConstraint(
                    "CK_UserSignature_ElectronicCredential",
                    "\"SignatureType\" <> 0 OR " +
                    "(\"TokenReference\" IS NULL AND " +
                    "\"ProtectedSecret\" IS NULL)");
            });
        });
        builder.Entity<SigningAsset>(entity =>
        {
            entity.ToTable("SigningAssets");
            entity.ConfigureByConvention();
            entity.Property(x => x.DisplayName).IsRequired()
                .HasMaxLength(255);
            entity.Property(x => x.BlobName).IsRequired()
                .HasMaxLength(SigningConsts.BlobNameMaxLength);
            entity.Property(x => x.MimeType).IsRequired()
                .HasMaxLength(127);
            entity.Property(x => x.Sha256).IsRequired()
                .HasMaxLength(64);
            entity.HasIndex(x => new { x.TenantId, x.BlobName })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => x.BlobName)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
            entity.HasIndex(x => new
                { x.TenantId, x.OwnerUserId, x.Kind });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SigningAsset_KindOwner",
                "\"Kind\" IN (0, 1, 2) AND \"Size\" > 0 AND " +
                "(\"Kind\" = 2 OR \"OwnerUserId\" IS NOT NULL)"));
        });
        builder.Entity<SigningBlobCleanup>(entity =>
        {
            entity.ToTable("SigningBlobCleanups");
            entity.ConfigureByConvention();
            entity.Property(x => x.BlobName).IsRequired()
                .HasMaxLength(SigningConsts.BlobNameMaxLength);
            entity.HasIndex(x => new { x.TenantId, x.BlobName })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => x.BlobName)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
            entity.HasIndex(x => x.CreationTime);
        });
        builder.Entity<SigningAttempt>(entity =>
        {
            entity.ToTable("SigningAttempts");
            entity.ConfigureByConvention();
            entity.Property(x => x.IdempotencyKey).IsRequired()
                .HasMaxLength(64);
            entity.Property(x => x.SourceSha256).IsRequired()
                .HasMaxLength(64);
            entity.Property(x => x.UserSignatureConcurrencyStamp)
                .IsRequired().HasMaxLength(40);
            entity.Property(x => x.FailureCode).HasMaxLength(100);
            entity.Property(x => x.PendingResultBlobName)
                .HasMaxLength(DocumentConsts.BlobNameMaxLength);
            entity.HasIndex(x => new
                { x.Status, x.PendingResultFileId, x.StartedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
            entity.HasIndex(x => new
                { x.TenantId, x.AssignmentId, x.Status });
            entity.HasOne<DocumentWorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentAssignment>()
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentFile>()
                .WithMany()
                .HasForeignKey(x => x.SourceFileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentFile>()
                .WithMany()
                .HasForeignKey(x => x.ResultFileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UserSignature>()
                .WithMany()
                .HasForeignKey(x => x.UserSignatureId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_SigningAttempt_Status",
                    "\"Status\" IN (0, 1, 2, 3, 4)");
                table.HasCheckConstraint(
                    "CK_SigningAttempt_SignatureType",
                    "\"SignatureType\" IN (0, 1)");
                table.HasCheckConstraint(
                    "CK_SigningAttempt_PendingResultPair",
                    "(\"PendingResultFileId\" IS NULL) = " +
                    "(\"PendingResultBlobName\" IS NULL)");
                table.HasCheckConstraint(
                    "CK_SigningAttempt_StateShape",
                    "(\"Status\" = 0 AND \"AttemptCount\" = 0 AND " +
                    "\"StartedAtUtc\" IS NULL AND " +
                    "\"FinishedAtUtc\" IS NULL AND " +
                    "\"ResultFileId\" IS NULL AND " +
                    "\"PendingResultFileId\" IS NULL AND " +
                    "\"PendingResultBlobName\" IS NULL AND " +
                    "\"FailureCode\" IS NULL) OR " +
                    "(\"Status\" = 1 AND \"AttemptCount\" > 0 AND " +
                    "\"StartedAtUtc\" IS NOT NULL AND " +
                    "\"FinishedAtUtc\" IS NULL AND " +
                    "\"ResultFileId\" IS NULL AND " +
                    "\"FailureCode\" IS NULL) OR " +
                    "(\"Status\" = 2 AND \"AttemptCount\" > 0 AND " +
                    "\"StartedAtUtc\" IS NOT NULL AND " +
                    "\"FinishedAtUtc\" IS NOT NULL AND " +
                    "\"ResultFileId\" IS NOT NULL AND " +
                    "\"PendingResultFileId\" IS NULL AND " +
                    "\"PendingResultBlobName\" IS NULL AND " +
                    "\"FailureCode\" IS NULL) OR " +
                    "(\"Status\" = 3 AND \"AttemptCount\" > 0 AND " +
                    "\"StartedAtUtc\" IS NOT NULL AND " +
                    "\"FinishedAtUtc\" IS NOT NULL AND " +
                    "\"ResultFileId\" IS NULL AND " +
                    "\"FailureCode\" IS NOT NULL) OR " +
                    "(\"Status\" = 4 AND \"FinishedAtUtc\" IS NOT NULL)");
            });
        });
    }

    private static void ConfigureWorkflowRuntime(ModelBuilder builder)
    {
        builder.Entity<DocumentWorkflowInstance>(entity =>
        {
            entity.ToTable("DocumentWorkflowInstances");
            entity.ConfigureByConvention();
            entity.HasIndex(x => new { x.TenantId, x.DocumentId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.DocumentId })
                .IsUnique()
                .HasFilter(
                    "\"TenantId\" IS NOT NULL AND \"Status\" IN (1, 2)");
            entity.HasIndex(x => x.DocumentId)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"Status\" IN (1, 2)");
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Workflow>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WorkflowTemplate>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentWorkflowInstance>()
                .WithOne()
                .HasForeignKey<DocumentWorkflowInstance>(x => x.PreviousInstanceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentWorkflowCommittedStep>()
                .WithMany()
                .HasForeignKey(x => x.CurrentCommittedStepId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentFile>()
                .WithMany()
                .HasForeignKey(x => x.SourceFileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentFile>()
                .WithMany()
                .HasForeignKey(x => x.CurrentSignedFileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new
            {
                x.TenantId,
                x.SourceFileId
            });
            entity.HasMany(x => x.Steps)
                .WithOne()
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_WorkflowInstance_ExtensionCounters",
                    "\"ExtensionCount\" >= 0 AND " +
                    "\"TotalExtensionBusinessDays\" >= 0");
                table.HasCheckConstraint(
                    "CK_WorkflowInstance_FinishedShape",
                    "((\"Status\" IN (0, 1, 2) AND " +
                    "\"FinishedAtUtc\" IS NULL) OR " +
                    "(\"Status\" IN (3, 4, 5, 6) AND " +
                    "\"FinishedAtUtc\" IS NOT NULL))");
                table.HasCheckConstraint(
                    "CK_WorkflowInstance_OverdueShape",
                    "((\"Status\" = 2 AND \"OverdueAtUtc\" IS NOT NULL) OR " +
                    "(\"Status\" <> 2 AND \"OverdueAtUtc\" IS NULL))");
            });
        });
        builder.Entity<DocumentWorkflowCommittedStep>(entity =>
        {
            entity.ToTable("DocumentWorkflowCommittedSteps");
            entity.ConfigureByConvention();
            entity.Property(x => x.Name).IsRequired().HasColumnType("text");
            entity.HasIndex(x => new { x.TenantId, x.InstanceId, x.Order })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => new { x.InstanceId, x.Order })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
            entity.HasIndex(x => new
            {
                x.TenantId,
                x.InstanceId,
                x.TemplateStepId
            }).IsUnique().HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => new { x.InstanceId, x.TemplateStepId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
            entity.HasOne<WorkflowStepTemplate>()
                .WithMany()
                .HasForeignKey(x => x.TemplateStepId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Receivers)
                .WithOne()
                .HasForeignKey(x => x.CommittedStepId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.ViewScopes)
                .WithOne()
                .HasForeignKey(x => x.CommittedStepId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<DocumentWorkflowCommittedReceiver>(entity =>
        {
            entity.ToTable("DocumentWorkflowCommittedReceivers");
            entity.ConfigureByConvention();
            entity.HasIndex(x => new { x.TenantId, x.CommittedStepId, x.UserId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => new { x.CommittedStepId, x.UserId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
        });
        builder.Entity<DocumentWorkflowCommittedViewScope>(entity =>
        {
            entity.ToTable("DocumentWorkflowCommittedViewScopes");
            entity.ConfigureByConvention();
            entity.HasIndex(x => new
                { x.TenantId, x.CommittedStepId, x.OrganizationUnitId })
                .IsUnique()
                .HasFilter(
                    "\"TenantId\" IS NOT NULL AND \"OrganizationUnitId\" IS NOT NULL");
            entity.HasIndex(x => new
                { x.TenantId, x.CommittedStepId, x.UserId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL AND \"UserId\" IS NOT NULL");
            entity.HasIndex(x => new { x.CommittedStepId, x.OrganizationUnitId })
                .IsUnique()
                .HasFilter(
                    "\"TenantId\" IS NULL AND \"OrganizationUnitId\" IS NOT NULL");
            entity.HasIndex(x => new { x.CommittedStepId, x.UserId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"UserId\" IS NOT NULL");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_CommittedViewScope_OneTarget",
                "(\"OrganizationUnitId\" IS NOT NULL AND \"UserId\" IS NULL) OR " +
                "(\"OrganizationUnitId\" IS NULL AND \"UserId\" IS NOT NULL)"));
        });
        builder.Entity<DocumentAssignment>(entity =>
        {
            entity.ToTable("DocumentAssignments");
            entity.ConfigureByConvention();
            entity.HasIndex(x => new
            {
                x.TenantId,
                x.ReceiverUserId,
                x.Status,
                x.IsCurrent
            });
            entity.HasIndex(x => new { x.TenantId, x.InstanceId, x.CommittedStepId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => new { x.InstanceId, x.CommittedStepId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
            entity.HasOne<DocumentWorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentWorkflowCommittedStep>()
                .WithMany()
                .HasForeignKey(x => x.CommittedStepId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentFile>()
                .WithMany()
                .HasForeignKey(x => x.DocumentFileResultId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_DocumentAssignment_ProcessedShape",
                    "((\"Status\" = 0 AND \"ProcessedAtUtc\" IS NULL) OR " +
                    "(\"Status\" IN (1, 2, 3) AND " +
                    "\"ProcessedAtUtc\" IS NOT NULL))");
                table.HasCheckConstraint(
                    "CK_DocumentAssignment_CurrentPending",
                    "NOT \"IsCurrent\" OR \"Status\" = 0");
            });
        });
        builder.Entity<DocumentWorkflowInstanceLog>(entity =>
        {
            entity.ToTable("DocumentWorkflowInstanceLogs");
            entity.ConfigureByConvention();
            entity.Property(x => x.Note).HasMaxLength(2000);
            entity.HasIndex(x => new { x.TenantId, x.InstanceId, x.OccurredAtUtc });
            entity.HasOne<DocumentWorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentAssignment>()
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DocumentHistory>(entity =>
        {
            entity.ToTable("DocumentHistories");
            entity.ConfigureByConvention();
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.HasIndex(x => new { x.TenantId, x.DocumentId, x.OccurredAtUtc });
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DocumentWorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureWorkflowCatalog(ModelBuilder builder)
    {
        builder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("WorkflowDefinitions");
            entity.ConfigureByConvention();
            ConfigureCatalog(entity);
        });
        builder.Entity<Workflow>(entity =>
        {
            entity.ToTable("Workflows");
            entity.ConfigureByConvention();
            ConfigureCatalog(entity);
            entity.HasIndex(x => new { x.TenantId, x.WorkflowDefinitionId });
            entity.HasOne<WorkflowDefinition>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WorkflowTemplate>(entity =>
        {
            entity.ToTable("WorkflowTemplates");
            entity.ConfigureByConvention();
            ConfigureCatalog(entity);
            entity.Property(x => x.WordTemplatePath).HasColumnType("text");
            entity.Property(x => x.PdfTemplatePath).HasColumnType("text");
            entity.Property(x => x.ContentSchema).HasColumnType("text");
            entity.HasIndex(x => new { x.TenantId, x.WorkflowId });
            entity.HasOne<Workflow>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WorkflowStepTemplate>(entity =>
        {
            entity.ToTable("WorkflowStepTemplates");
            entity.ConfigureByConvention();
            entity.Property(x => x.Name).IsRequired().HasColumnType("text");
            entity.HasIndex(x => new { x.TenantId, x.WorkflowTemplateId, x.Order })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(x => new { x.WorkflowTemplateId, x.Order })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"IsDeleted\" = false");
            entity.HasOne<WorkflowTemplate>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WorkflowStepAssignmentConfiguration>(entity =>
        {
            entity.ToTable("WorkflowStepAssignmentConfigurations");
            entity.ConfigureByConvention();
            entity.HasIndex(x => new { x.TenantId, x.WorkflowStepTemplateId });
            entity.HasIndex(x => new { x.TenantId, x.WorkflowStepTemplateId, x.IsPrimary })
                .IsUnique()
                .HasFilter(
                    "\"TenantId\" IS NOT NULL AND \"IsPrimary\" = true AND \"IsActive\" = true AND \"IsDeleted\" = false");
            entity.HasIndex(x => new { x.WorkflowStepTemplateId, x.IsPrimary })
                .IsUnique()
                .HasFilter(
                    "\"TenantId\" IS NULL AND \"IsPrimary\" = true AND \"IsActive\" = true AND \"IsDeleted\" = false");
            entity.HasOne<WorkflowStepTemplate>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowStepTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Users)
                .WithOne()
                .HasForeignKey(x => x.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.OrganizationUnits)
                .WithOne()
                .HasForeignKey(x => x.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<WorkflowStepAssignmentUser>(entity =>
        {
            entity.ToTable("WorkflowStepAssignmentUsers");
            entity.ConfigureByConvention();
            entity.HasIndex(x => new { x.TenantId, x.ConfigurationId, x.UserId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => new { x.ConfigurationId, x.UserId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");
        });
        builder.Entity<WorkflowStepAssignmentOrganizationUnit>(entity =>
        {
            entity.ToTable("WorkflowStepAssignmentOrganizationUnits");
            entity.ConfigureByConvention();
            entity.HasIndex(x => new
            {
                x.TenantId,
                x.ConfigurationId,
                x.OrganizationUnitId
            }).IsUnique().HasFilter("\"TenantId\" IS NOT NULL");
            entity.HasIndex(x => new
            {
                x.ConfigurationId,
                x.OrganizationUnitId
            }).IsUnique().HasFilter("\"TenantId\" IS NULL");
        });
    }

    private static void ConfigureCatalog<TEntity>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : WorkflowCatalogAggregate
    {
        entity.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(WorkflowCatalogConsts.CodeMaxLength);
        entity.Property(x => x.Name).IsRequired().HasColumnType("text");
        entity.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");
        entity.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"TenantId\" IS NULL AND \"IsDeleted\" = false");
    }
}

using hanhchinhso.DocumentService.Documents;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Documents;

public class Document : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string? Number { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? CurrentStatus { get; private set; }
    public DateTime? CompletedTime { get; private set; }
    public string StorageNumber { get; private set; } = string.Empty;
    public DateTime IncomingDate { get; private set; }
    public Guid? FieldId { get; private set; }
    public Guid? UnitId { get; private set; }
    public Guid? StatusId { get; private set; }
    public Guid? TypeId { get; private set; }
    public Guid? UrgencyLevelId { get; private set; }
    public Guid? SecrecyLevelId { get; private set; }
    public DocumentSourceType SourceType { get; private set; }
    public Guid? OrganizationUnitId { get; private set; }
    public Guid? FromUserId { get; private set; }
    public Guid? ReceiverUserId { get; private set; }
    public Guid? ParentDocumentId { get; private set; }

    protected Document() { }

    public Document(Guid id, Guid? tenantId, CreateUpdateDocumentDto input, Guid? creatorId) : base(id)
    {
        TenantId = tenantId;
        FromUserId = creatorId;
        Update(input);
    }

    public void Update(CreateUpdateDocumentDto input)
    {
        Number = input.Number.IsNullOrWhiteSpace()
            ? null
            : Check.Length(input.Number.Trim(), nameof(input.Number), DocumentConsts.NumberMaxLength);
        Title = Check.NotNullOrWhiteSpace(input.Title, nameof(input.Title), DocumentConsts.TitleMaxLength);
        CurrentStatus = input.CurrentStatus.IsNullOrWhiteSpace()
            ? null
            : Check.Length(input.CurrentStatus.Trim(), nameof(input.CurrentStatus), DocumentConsts.StatusMaxLength);
        StorageNumber = Check.NotNullOrWhiteSpace(
            input.StorageNumber, nameof(input.StorageNumber), DocumentConsts.StorageNumberMaxLength);
        CompletedTime = input.CompletedTime;
        IncomingDate = input.IncomingDate;
        FieldId = input.FieldId;
        UnitId = input.UnitId;
        StatusId = input.StatusId;
        TypeId = input.TypeId;
        UrgencyLevelId = input.UrgencyLevelId;
        SecrecyLevelId = input.SecrecyLevelId;
        SourceType = input.SourceType;
        OrganizationUnitId = input.OrganizationUnitId;
        ReceiverUserId = input.ReceiverUserId;
        ParentDocumentId = input.ParentDocumentId;
    }

    public void SetWorkflowStatus(string status, DateTime? completedTime = null)
    {
        CurrentStatus = Check.NotNullOrWhiteSpace(
            status,
            nameof(status),
            DocumentConsts.StatusMaxLength);
        CompletedTime = completedTime;
    }
}

public class DocumentFile : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid DocumentId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string? Hash { get; private set; }
    public bool IsSigned { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid? SourceDocxFileId { get; private set; }
    public Guid? DerivedPdfFileId { get; private set; }
    public Guid? SourceFileId { get; private set; }
    public bool BlobDeletionPending { get; private set; }

    protected DocumentFile() { }

    public DocumentFile(Guid id, Guid? tenantId, Guid documentId, string displayName,
        string blobName, string mimeType, long size, string? hash = null) : base(id)
    {
        TenantId = tenantId;
        DocumentId = documentId;
        DisplayName = Check.NotNullOrWhiteSpace(displayName, nameof(displayName), DocumentConsts.FileNameMaxLength);
        BlobName = Check.NotNullOrWhiteSpace(blobName, nameof(blobName), DocumentConsts.BlobNameMaxLength);
        MimeType = Check.NotNullOrWhiteSpace(mimeType, nameof(mimeType), DocumentConsts.MimeTypeMaxLength);
        Size = size;
        Hash = hash;
        UploadedAt = DateTime.UtcNow;
    }

    public void MarkBlobDeletionPending()
    {
        BlobDeletionPending = true;
    }

    public void MarkSigned(Guid sourceFileId)
    {
        SourceFileId = Check.NotDefaultOrNull<Guid>(
            sourceFileId, nameof(sourceFileId));
        IsSigned = true;
    }
}

public class DocumentBlobCleanup : BasicAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string BlobName { get; private set; } = string.Empty;
    public DateTime CreationTime { get; private set; }

    protected DocumentBlobCleanup() { }

    public DocumentBlobCleanup(Guid id, Guid? tenantId, string blobName) : base(id)
    {
        TenantId = tenantId;
        BlobName = Check.NotNullOrWhiteSpace(
            blobName, nameof(blobName), DocumentConsts.BlobNameMaxLength);
        CreationTime = DateTime.UtcNow;
    }
}

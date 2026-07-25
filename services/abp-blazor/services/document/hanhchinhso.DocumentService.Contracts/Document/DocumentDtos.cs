using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.DocumentService.Documents;

public enum DocumentSourceType
{
    Archive = 0,
    Personal = 1,
    SentToMe = 2,
    Workflow = 3
}

public static class DocumentConsts
{
    public const int NumberMaxLength = 50;
    public const int TitleMaxLength = 500;
    public const int StatusMaxLength = 30;
    public const int StorageNumberMaxLength = 50;
    public const int FileNameMaxLength = 255;
    public const int MimeTypeMaxLength = 127;
    public const int BlobNameMaxLength = 512;
}

public class DocumentListInput : PagedAndSortedResultRequestDto
{
    public string? FilterText { get; set; }
    public string? Number { get; set; }
    public string? CurrentStatus { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public DocumentSourceType? SourceType { get; set; }
}

public class DocumentDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public string? Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CurrentStatus { get; set; }
    public DateTime? CompletedTime { get; set; }
    public string StorageNumber { get; set; } = string.Empty;
    public DateTime IncomingDate { get; set; }
    public Guid? FieldId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? TypeId { get; set; }
    public Guid? UrgencyLevelId { get; set; }
    public Guid? SecrecyLevelId { get; set; }
    public DocumentSourceType SourceType { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public Guid? FromUserId { get; set; }
    public Guid? ReceiverUserId { get; set; }
    public Guid? ParentDocumentId { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdateDocumentDto : IHasConcurrencyStamp
{
    [StringLength(DocumentConsts.NumberMaxLength)]
    public string? Number { get; set; }

    [Required, StringLength(DocumentConsts.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [StringLength(DocumentConsts.StatusMaxLength)]
    public string? CurrentStatus { get; set; }

    public DateTime? CompletedTime { get; set; }

    [Required, StringLength(DocumentConsts.StorageNumberMaxLength)]
    public string StorageNumber { get; set; } = string.Empty;

    public DateTime IncomingDate { get; set; }
    public Guid? FieldId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? TypeId { get; set; }
    public Guid? UrgencyLevelId { get; set; }
    public Guid? SecrecyLevelId { get; set; }
    public DocumentSourceType SourceType { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public Guid? ReceiverUserId { get; set; }
    public Guid? ParentDocumentId { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class DocumentFileDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public Guid DocumentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Hash { get; set; }
    public bool IsSigned { get; set; }
    public DateTime UploadedAt { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

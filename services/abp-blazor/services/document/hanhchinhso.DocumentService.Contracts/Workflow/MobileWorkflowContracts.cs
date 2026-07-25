using System.Text.Json.Serialization;
using hanhchinhso.DocumentService.Documents;
using Volo.Abp.Application.Dtos;

namespace hanhchinhso.DocumentService.Workflows;

[JsonConverter(typeof(StrictMobileSigningFilterModeJsonConverter))]
public enum MobileSigningFilterMode
{
    All = 0,
    SentToMe = 1,
    SentByMe = 2,
    Following = 3
}

public sealed class StrictMobileSigningFilterModeJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);

public class MobileSigningListInput : PagedAndSortedResultRequestDto
{
    public string? FilterText { get; set; }
    public MobileSigningFilterMode FilterMode { get; set; }
    public DateTime? FromDateUtc { get; set; }
    public DateTime? ToDateUtc { get; set; }
    public DocumentWorkflowStatus? Status { get; set; }
}

public class MobileSigningItemDto
{
    public Guid DocumentId { get; set; }
    public string? DocumentNumber { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string StorageNumber { get; set; } = string.Empty;
    public Guid WorkflowInstanceId { get; set; }
    public DocumentWorkflowStatus WorkflowStatus { get; set; }
    public string? CurrentStepName { get; set; }
    public int? CurrentStepOrder { get; set; }
    public int TotalSteps { get; set; }
    public Guid? MyAssignmentId { get; set; }
    public DocumentAssignmentStatus? MyAssignmentStatus { get; set; }
    public bool CanAct { get; set; }
    public bool CanResubmit { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? DeadlineAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}

public class MobileSigningPageResultDto
{
    public long TotalCount { get; set; }
    public int AllCount { get; set; }
    public int SentToMeCount { get; set; }
    public int SentByMeCount { get; set; }
    public int FollowingCount { get; set; }
    public List<MobileSigningItemDto> Items { get; set; } = [];
}

public class MobileWorkflowLogDto
{
    public Guid Id { get; set; }
    public Guid? AssignmentId { get; set; }
    public WorkflowRuntimeAction Action { get; set; }
    public Guid? ActorUserId { get; set; }
    public DocumentWorkflowStatus? FromStatus { get; set; }
    public DocumentWorkflowStatus? ToStatus { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Note { get; set; }
}

public class MobileDocumentHistoryDto
{
    public Guid Id { get; set; }
    public WorkflowRuntimeAction Action { get; set; }
    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Comment { get; set; }
}

public class MobileWorkflowDetailDto
{
    public WorkflowRuntimeStatusDto Runtime { get; set; } = new();
    public DocumentDto Document { get; set; } = new();
    public List<MobileWorkflowLogDto> Logs { get; set; } = [];
    public List<MobileDocumentHistoryDto> History { get; set; } = [];
    public List<DocumentFileDto> Files { get; set; } = [];
}

public interface IMobileWorkflowQueryAppService
{
    Task<MobileSigningPageResultDto> GetSigningListAsync(
        MobileSigningListInput input);
    Task<MobileWorkflowDetailDto> GetDetailAsync(Guid instanceId);
}

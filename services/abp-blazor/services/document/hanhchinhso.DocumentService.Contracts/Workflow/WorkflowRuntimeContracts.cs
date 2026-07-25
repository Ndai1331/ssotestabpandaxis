using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.DocumentService.Workflows;

[JsonConverter(typeof(StrictWorkflowAssigneeTypeJsonConverter))]
public enum WorkflowAssigneeType
{
    SpecificUser = 0,
    RoleInSubmitterOrganizationUnit = 1,
    ScopedAssignee = 2
}

[JsonConverter(typeof(StrictDocumentWorkflowStatusJsonConverter))]
public enum DocumentWorkflowStatus
{
    Draft = 0,
    InProgress = 1,
    Overdue = 2,
    Completed = 3,
    Rejected = 4,
    Returned = 5,
    Cancelled = 6
}

[JsonConverter(typeof(StrictDocumentAssignmentActionJsonConverter))]
public enum DocumentAssignmentAction
{
    Process = 0,
    Sign = 1,
    View = 2
}

[JsonConverter(typeof(StrictDocumentAssignmentStatusJsonConverter))]
public enum DocumentAssignmentStatus
{
    Pending = 0,
    Done = 1,
    Rejected = 2,
    Revoked = 3
}

[JsonConverter(typeof(StrictWorkflowRuntimeActionJsonConverter))]
public enum WorkflowRuntimeAction
{
    Submit = 0,
    Approve = 1,
    RequestSign = 2,
    ConfirmSign = 3,
    Return = 4,
    Reject = 5,
    Cancel = 6,
    AssignUser = 7,
    UpdateSigner = 8,
    MarkOverdue = 9,
    Extend = 10,
    Complete = 11,
    Resubmit = 12
}

public sealed class StrictWorkflowAssigneeTypeJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);
public sealed class StrictDocumentWorkflowStatusJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);
public sealed class StrictDocumentAssignmentActionJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);
public sealed class StrictDocumentAssignmentStatusJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);
public sealed class StrictWorkflowRuntimeActionJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);

public class WorkflowStepAssignmentConfigurationDto :
    FullAuditedEntityDto<Guid>,
    IHasConcurrencyStamp
{
    public Guid WorkflowStepTemplateId { get; set; }
    public WorkflowAssigneeType AssigneeType { get; set; }
    public Guid? RoleId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<Guid> UserIds { get; set; } = [];
    public IReadOnlyList<Guid> OrganizationUnitIds { get; set; } = [];
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdateWorkflowStepAssignmentConfigurationDto : IHasConcurrencyStamp
{
    public Guid WorkflowStepTemplateId { get; set; }

    [Required]
    public WorkflowAssigneeType? AssigneeType { get; set; }

    public Guid? RoleId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> UserIds { get; set; } = [];
    public List<Guid> OrganizationUnitIds { get; set; } = [];
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class WorkflowStepAssignmentConfigurationListInput :
    PagedAndSortedResultRequestDto
{
    public Guid? WorkflowStepTemplateId { get; set; }
    public WorkflowAssigneeType? AssigneeType { get; set; }
    public bool? IsActive { get; set; }
}

public interface IWorkflowStepAssignmentConfigurationAppService
{
    Task<WorkflowStepAssignmentConfigurationDto> GetAsync(Guid id);
    Task<PagedResultDto<WorkflowStepAssignmentConfigurationDto>> GetListAsync(
        WorkflowStepAssignmentConfigurationListInput input);
    Task<WorkflowStepAssignmentConfigurationDto> CreateAsync(
        CreateUpdateWorkflowStepAssignmentConfigurationDto input);
    Task<WorkflowStepAssignmentConfigurationDto> UpdateAsync(
        Guid id,
        CreateUpdateWorkflowStepAssignmentConfigurationDto input);
    Task DeleteAsync(Guid id, string concurrencyStamp);
}

public class WorkflowSubmitSelectionDto
{
    public Guid WorkflowStepTemplateId { get; set; }
    public Guid UserId { get; set; }
}

public class WorkflowSubmitPreviewInput
{
    public Guid DocumentId { get; set; }
    public Guid SourceFileId { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public Guid? PreviousInstanceId { get; set; }
    public Guid? InitiatorUserId { get; set; }
    public List<WorkflowSubmitSelectionDto> Selections { get; set; } = [];
}

public class WorkflowSubmitInput : WorkflowSubmitPreviewInput
{
    [Required]
    public string PreviewToken { get; set; } = string.Empty;
    [Required]
    public string DocumentConcurrencyStamp { get; set; } = string.Empty;
}

public class WorkflowCandidateDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsPrimary { get; set; }
    public Guid? ProvenanceOrganizationUnitId { get; set; }
    public Guid? ProvenanceRoleId { get; set; }
}

public class WorkflowStepSubmitPreviewDto
{
    public Guid WorkflowStepTemplateId { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public bool AllowReturn { get; set; }
    public int? SlaDays { get; set; }
    public Guid? PrimaryOrganizationUnitId { get; set; }
    public List<WorkflowCandidateDto> Candidates { get; set; } = [];
    public List<Guid> ViewUserIds { get; set; } = [];
    public List<Guid> ViewOrganizationUnitIds { get; set; } = [];
}

public class WorkflowSubmitPreviewDto
{
    public Guid DocumentId { get; set; }
    public Guid SourceFileId { get; set; }
    public string SourceFileSha256 { get; set; } = string.Empty;
    public Guid WorkflowId { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public WorkflowSignMode SignMode { get; set; }
    public List<WorkflowStepSubmitPreviewDto> Steps { get; set; } = [];
    public string PreviewToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public class DocumentWorkflowInstanceDto : EntityDto<Guid>, IHasConcurrencyStamp
{
    public Guid DocumentId { get; set; }
    public Guid SourceFileId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public Guid InitiatorUserId { get; set; }
    public WorkflowSignMode SignMode { get; set; }
    public DocumentWorkflowStatus Status { get; set; }
    public Guid? CurrentCommittedStepId { get; set; }
    public Guid? CurrentSignedFileId { get; set; }
    public Guid? PreviousInstanceId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? DeadlineAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public DateTime? OverdueAtUtc { get; set; }
    public int ExtensionCount { get; set; }
    public int TotalExtensionBusinessDays { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public interface IWorkflowSubmissionAppService
{
    Task<WorkflowSubmitPreviewDto> PreviewAsync(WorkflowSubmitPreviewInput input);
    Task<DocumentWorkflowInstanceDto> SubmitAsync(WorkflowSubmitInput input);
}

public class WorkflowAssignmentActionInput
{
    [Required]
    public string AssignmentConcurrencyStamp { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? Comment { get; set; }
}

public class WorkflowCancelInput
{
    [Required]
    public string InstanceConcurrencyStamp { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? Comment { get; set; }
}

public class WorkflowSignerReplacementInput
{
    public Guid NewSignerUserId { get; set; }
    [Required]
    public string AssignmentConcurrencyStamp { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? Comment { get; set; }
}

public class WorkflowMarkOverdueInput
{
    [Required]
    public string InstanceConcurrencyStamp { get; set; } = string.Empty;
}

public class WorkflowExtensionInput
{
    [Range(1, 365)]
    public int BusinessDays { get; set; }
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
    [Required]
    public string InstanceConcurrencyStamp { get; set; } = string.Empty;
}

public interface IWorkflowActionAppService
{
    Task<DocumentWorkflowInstanceDto> ApproveAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input);
    Task<DocumentWorkflowInstanceDto> RequestSignAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input);
    Task<DocumentWorkflowInstanceDto> ReturnAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input);
    Task<DocumentWorkflowInstanceDto> RejectAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input);
    Task<DocumentWorkflowInstanceDto> CancelAsync(
        Guid instanceId,
        WorkflowCancelInput input);
    Task<DocumentWorkflowInstanceDto> ReplaceSignerAsync(
        Guid assignmentId,
        WorkflowSignerReplacementInput input);
    Task<DocumentWorkflowInstanceDto> MarkOverdueAsync(
        Guid instanceId,
        WorkflowMarkOverdueInput input);
    Task<DocumentWorkflowInstanceDto> ExtendAsync(
        Guid instanceId,
        WorkflowExtensionInput input);
}

public class DocumentAssignmentDto : EntityDto<Guid>, IHasConcurrencyStamp
{
    public Guid InstanceId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid CommittedStepId { get; set; }
    public Guid ReceiverUserId { get; set; }
    public DocumentAssignmentAction Action { get; set; }
    public DocumentAssignmentStatus Status { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public Guid? DocumentFileResultId { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class WorkflowCommittedStepStatusDto : EntityDto<Guid>
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public bool AllowReturn { get; set; }
    public bool IsViewUnlocked { get; set; }
    public List<Guid> ViewUserIds { get; set; } = [];
    public List<Guid> ViewOrganizationUnitIds { get; set; } = [];
}

public class WorkflowRuntimeStatusDto
{
    public DocumentWorkflowInstanceDto Instance { get; set; } = new();
    public List<WorkflowCommittedStepStatusDto> Steps { get; set; } = [];
    public List<DocumentAssignmentDto> Assignments { get; set; } = [];
}

public class MyWorkflowAssignmentListInput : PagedAndSortedResultRequestDto
{
    public DocumentAssignmentStatus? Status { get; set; }
    public bool? IsCurrent { get; set; }
}

public interface IWorkflowRuntimeQueryAppService
{
    Task<WorkflowRuntimeStatusDto> GetAsync(Guid instanceId);
    Task<PagedResultDto<DocumentAssignmentDto>> GetMyAssignmentsAsync(
        MyWorkflowAssignmentListInput input);
}

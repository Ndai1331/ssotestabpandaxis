namespace HCS.DocumentService.Workflows;

public enum WorkflowInstanceStatus { Running, Completed, Rejected, Cancelled }
public enum ApprovalTaskStatus { Pending, Approved, Rejected, Cancelled }

public sealed record WorkflowStepInput(string Code, string Name, int Order, string RequiredPermission,
    string Type = "PROCESS", Guid? AssigneeUserId = null);
public sealed record CreateWorkflowDefinitionRequest(string Code, string Name, IReadOnlyList<WorkflowStepInput> Steps);
public sealed record UpdateWorkflowDefinitionRequest(string Name, IReadOnlyList<WorkflowStepInput> Steps);
public sealed record WorkflowStepDto(Guid Id, string Code, string Name, int Order, string RequiredPermission,
    string Type, Guid? AssigneeUserId);
public sealed record WorkflowDefinitionDto(Guid Id, string Code, string Name,
    IReadOnlyList<WorkflowStepDto> Steps, DateTime CreationTime);
public sealed record CreateWorkflowTemplateRequest(string Code, string Name, Guid DefinitionId, int Version, string TemplateJson);
public sealed record WorkflowTemplateDto(Guid Id, string Code, string Name, Guid DefinitionId, int Version, bool IsActive,
    DateTime CreationTime, Guid? WordFileId, string? WordFileName, Guid? PdfFileId, string? PdfFileName);
public sealed record StartWorkflowRequest(Guid DocumentId, Guid DefinitionId, string IdempotencyKey);
public sealed record DecideApprovalTaskRequest(bool Approve, string? Comment, string IdempotencyKey);
public sealed record ApprovalTaskDto(Guid Id, Guid InstanceId, string StepCode, ApprovalTaskStatus Status, Guid? DecidedBy,
    DateTime? DecidedAt, Guid? AssigneeUserId);
public sealed record WorkflowInstanceDto(Guid Id, Guid DocumentId, Guid DefinitionId, WorkflowInstanceStatus Status,
    int CurrentStep, IReadOnlyList<ApprovalTaskDto> Tasks, DateTime CreationTime);

public interface IWorkflowAppService
{
    Task<IReadOnlyList<WorkflowDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<WorkflowDefinitionDto?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<WorkflowTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowInstanceDto>> GetInstancesAsync(Guid? documentId = null,
        WorkflowInstanceStatus? status = null, CancellationToken cancellationToken = default);
    Task<WorkflowInstanceDto?> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateDefinitionAsync(CreateWorkflowDefinitionRequest input, CancellationToken cancellationToken = default);
    Task UpdateDefinitionAsync(Guid id, UpdateWorkflowDefinitionRequest input, CancellationToken cancellationToken = default);
    Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest input, CancellationToken cancellationToken = default);
    Task<WorkflowTemplateDto> SetTemplateActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task<WorkflowTemplateDto> UploadTemplateFileAsync(Guid id, string kind, string fileName, string contentType,
        Stream content, long size, CancellationToken cancellationToken = default);
    Task<(string FileName, string ContentType, Stream Content)> OpenTemplateFileAsync(Guid id, string kind,
        CancellationToken cancellationToken = default);
    Task<WorkflowInstanceDto> StartAsync(StartWorkflowRequest input, CancellationToken cancellationToken = default);
    Task<WorkflowInstanceDto> DecideAsync(Guid taskId, DecideApprovalTaskRequest input, CancellationToken cancellationToken = default);
}

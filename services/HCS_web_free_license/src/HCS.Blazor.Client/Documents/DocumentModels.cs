using System;
using System.Collections.Generic;

namespace HCS.Blazor.Client.Documents;

public enum DocumentStatus { Draft, Submitted, InReview, Approved, Rejected, Archived }
public enum WorkflowInstanceStatus { Running, Completed, Rejected, Cancelled }
public enum ApprovalTaskStatus { Pending, Approved, Rejected, Cancelled }
public enum SigningStatus { Pending, Completed, Failed }

public sealed record PagedDocumentsResponse(long TotalCount, List<DocumentDto> Items);

public sealed record DocumentFileDto(Guid Id, string FileName, string ContentType, long Size, string Sha256, DateTime CreationTime);
public sealed record DocumentAssignmentDto(Guid Id, Guid AssigneeUserId, string Responsibility, DateTime AssignedAt);
public sealed record DocumentHistoryDto(Guid Id, string Action, Guid? ActorUserId, string? Detail, DateTime OccurredAt);
public sealed record DocumentDto(
    Guid Id, string Number, string Title, string? Description, DocumentStatus Status,
    Guid? DocumentTypeId, Guid? SectorId, Guid? UrgencyId, Guid? ConfidentialityId,
    List<DocumentFileDto> Files, List<DocumentAssignmentDto> Assignments,
    List<DocumentHistoryDto> History, DateTime CreationTime);

public sealed record CreateDocumentRequest(
    string Number, string Title, string? Description,
    Guid? DocumentTypeId, Guid? SectorId, Guid? UrgencyId, Guid? ConfidentialityId);
public sealed record UpdateDocumentRequest(
    string Title, string? Description,
    Guid? DocumentTypeId, Guid? SectorId, Guid? UrgencyId, Guid? ConfidentialityId);
public sealed record AssignDocumentRequest(Guid AssigneeUserId, string Responsibility);

public sealed record WorkflowStepInput(string Code, string Name, int Order, string RequiredPermission, string Type = "PROCESS", Guid? AssigneeUserId = null);
public sealed record WorkflowStepDto(Guid Id, string Code, string Name, int Order, string RequiredPermission, string Type, Guid? AssigneeUserId);
public sealed record WorkflowDefinitionDto(Guid Id, string Code, string Name, List<WorkflowStepDto> Steps, DateTime CreationTime);
public sealed record CreateWorkflowDefinitionRequest(string Code, string Name, List<WorkflowStepInput> Steps);
public sealed record UpdateWorkflowDefinitionRequest(string Name, List<WorkflowStepInput> Steps);
public sealed record WorkflowTemplateDto(Guid Id, string Code, string Name, Guid DefinitionId, int Version, bool IsActive, DateTime CreationTime, Guid? WordFileId, string? WordFileName, Guid? PdfFileId, string? PdfFileName);
public sealed record CreateWorkflowTemplateRequest(string Code, string Name, Guid DefinitionId, int Version, string TemplateJson);
public sealed record ApprovalTaskDto(Guid Id, Guid InstanceId, string StepCode, ApprovalTaskStatus Status, Guid? DecidedBy, DateTime? DecidedAt, Guid? AssigneeUserId);
public sealed record WorkflowInstanceDto(
    Guid Id, Guid DocumentId, Guid DefinitionId, WorkflowInstanceStatus Status, int CurrentStep,
    List<ApprovalTaskDto> Tasks, DateTime CreationTime);
public sealed record StartWorkflowRequest(Guid DocumentId, Guid DefinitionId, string IdempotencyKey);
public sealed record DecideApprovalTaskRequest(bool Approve, string? Comment, string IdempotencyKey);

public sealed record SigningCredentialDto(Guid Id, int Kind, string Endpoint, string MaskedSecret, DateTime UpdatedAt);
public sealed record ConfigureSigningCredentialRequest(int Kind, string Endpoint, string Secret);
public sealed record SignDocumentRequest(Guid DocumentId, Guid FileId, int Kind, string IdempotencyKey);
public sealed record SigningAttemptDto(
    Guid Id, Guid DocumentId, Guid FileId, int Kind, SigningStatus Status, string InputSha256,
    string? OutputSha256, string? Error, DateTime CreationTime, DateTime? CompletedAt);
public sealed record SigningReportDto(Guid DocumentId, int Completed, int Failed, List<SigningAttemptDto> Attempts);
public sealed record UserSignatureDto(Guid Id, string FileName, string ContentType, long Size, bool IsDefault, DateTime CreationTime);

public sealed record DocumentListQuery(string? Filter, string? Status, bool Mine, int SkipCount, int MaxResultCount, int? SourceType = null);

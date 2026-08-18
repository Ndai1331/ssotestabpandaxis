namespace HCS.DocumentService.Documents;

public enum DocumentStatus { Draft, Submitted, InReview, Approved, Rejected, Archived }

public static class DocumentPermissions
{
    public const string View = "Documents.View";
    public const string Create = "Documents.Create";
    public const string Update = "Documents.Update";
    public const string Assign = "Documents.Assign";
    public const string ManageFiles = "Documents.ManageFiles";
    public const string WorkflowView = "Documents.Workflow.View";
    public const string WorkflowManage = "Documents.Workflow.Manage";
    public const string WorkflowStart = "Documents.Workflow.Start";
    public const string WorkflowDecide = "Documents.Workflow.Decide";
    public const string SigningConfigure = "Documents.Signing.Configure";
    public const string SigningExecute = "Documents.Signing.Execute";
    public const string SigningReport = "Documents.Signing.Report";
}

public sealed record CreateDocumentRequest(string Number, string Title, string? Description,
    Guid? DocumentTypeId = null, Guid? SectorId = null, Guid? UrgencyId = null, Guid? ConfidentialityId = null);
public sealed record UpdateDocumentRequest(string Title, string? Description,
    Guid? DocumentTypeId = null, Guid? SectorId = null, Guid? UrgencyId = null, Guid? ConfidentialityId = null);
public sealed record AddDocumentFileRequest(string FileName, string ContentType, long Size, string Sha256);
public sealed record AssignDocumentRequest(Guid AssigneeUserId, string Responsibility);
public sealed record DocumentFileDto(Guid Id, string FileName, string ContentType, long Size, string Sha256, DateTime CreationTime);
public sealed record DocumentAssignmentDto(Guid Id, Guid AssigneeUserId, string Responsibility, DateTime AssignedAt);
public sealed record DocumentHistoryDto(Guid Id, string Action, Guid? ActorUserId, string? Detail, DateTime OccurredAt);
public sealed record DocumentDto(Guid Id, string Number, string Title, string? Description, DocumentStatus Status,
    Guid? DocumentTypeId, Guid? SectorId, Guid? UrgencyId, Guid? ConfidentialityId,
    IReadOnlyList<DocumentFileDto> Files, IReadOnlyList<DocumentAssignmentDto> Assignments,
    IReadOnlyList<DocumentHistoryDto> History, DateTime CreationTime);
public sealed record PagedDocumentsDto(long TotalCount, IReadOnlyList<DocumentDto> Items);

public interface IDocumentAppService
{
    Task<PagedDocumentsDto> GetListAsync(string? filter = null, DocumentStatus? status = null,
        bool mine = false, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<DocumentDto> CreateAsync(CreateDocumentRequest input, CancellationToken cancellationToken = default);
    Task<DocumentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentRequest input, CancellationToken cancellationToken = default);
    Task<DocumentDto> AssignAsync(Guid id, AssignDocumentRequest input, CancellationToken cancellationToken = default);
    Task<DocumentDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default);
}

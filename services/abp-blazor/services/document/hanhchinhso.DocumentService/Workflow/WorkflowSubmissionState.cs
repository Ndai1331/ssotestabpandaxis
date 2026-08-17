using hanhchinhso.DocumentService.Documents;
using hanhchinhso.IdentityService.Internal;

namespace hanhchinhso.DocumentService.Workflows;

internal sealed record WorkflowSubmissionState(
    Document Document,
    DocumentFile SourceFile,
    string SourceFileSha256,
    Workflow Workflow,
    WorkflowTemplate Template,
    Guid? PreviousInstanceId,
    WorkflowSignMode SignMode,
    Guid InitiatorUserId,
    IReadOnlyList<WorkflowStepTemplate> Steps,
    IReadOnlyList<WorkflowStepAssignmentConfiguration> Configurations,
    WorkflowAssigneeResolutionResult IdentityResolution,
    IReadOnlyList<WorkflowStepSubmitPreviewDto> PreviewSteps,
    string CandidateHash);

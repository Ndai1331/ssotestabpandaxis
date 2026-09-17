using HCS.DocumentService.Documents;

namespace HCS.DocumentService.Workflows;

internal static class WorkflowSubmissionDeletion
{
    internal static bool CanDelete(DocumentSourceType sourceType, Guid? submitterId, Guid userId,
        IEnumerable<WorkflowInstance> instances, bool hasSigningActivity)
    {
        var workflows = instances.ToList();
        return sourceType == DocumentSourceType.Workflow
            && submitterId == userId
            && !hasSigningActivity
            && workflows.Count == 1
            && workflows[0].Status == WorkflowInstanceStatus.Running
            && workflows[0].Tasks.Count > 0
            && workflows[0].Tasks.All(task => task.Status == ApprovalTaskStatus.Pending && task.DecidedAt is null);
    }
}

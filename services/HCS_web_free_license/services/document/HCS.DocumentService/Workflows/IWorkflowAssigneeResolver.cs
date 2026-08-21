namespace HCS.DocumentService.Workflows;

public interface IWorkflowAssigneeResolver
{
    Task<IReadOnlyList<WorkflowAssigneeCandidateDto>> ResolveByRoleAsync(
        Guid roleId, Guid submitterUserId, CancellationToken cancellationToken = default);
}
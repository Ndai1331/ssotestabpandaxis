namespace HCS.DocumentService.Workflows;

public interface IWorkflowAssigneeResolver
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>> ResolveByRolesAsync(
        IReadOnlyCollection<Guid> roleIds, Guid submitterUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, WorkflowAssigneeCandidateDto?>> ResolveByUsersAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowAssigneeCandidateDto>> ResolveByRoleAsync(
        Guid roleId, Guid submitterUserId, CancellationToken cancellationToken = default);

    Task<WorkflowAssigneeCandidateDto?> ResolveByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);
}

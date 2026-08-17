using hanhchinhso.IdentityService.Internal;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Workflows;

public interface IWorkflowAssigneeResolver
{
    Task<WorkflowAssigneeResolutionResult> ResolveAsync(
        Guid submitterUserId,
        IReadOnlyCollection<WorkflowStepAssignmentConfiguration> configurations,
        CancellationToken cancellationToken = default);
}

public class WorkflowAssigneeResolver :
    IWorkflowAssigneeResolver,
    ITransientDependency
{
    private readonly WorkflowIdentityClient _client;

    public WorkflowAssigneeResolver(WorkflowIdentityClient client)
    {
        _client = client;
    }

    public Task<WorkflowAssigneeResolutionResult> ResolveAsync(
        Guid submitterUserId,
        IReadOnlyCollection<WorkflowStepAssignmentConfiguration> configurations,
        CancellationToken cancellationToken = default)
    {
        return _client.PostAsync<
            WorkflowAssigneeResolutionRequest,
            WorkflowAssigneeResolutionResult>(
            "api/app/identity-reference-validation/resolve-workflow-assignees",
            new WorkflowAssigneeResolutionRequest
            {
                SubmitterUserId = submitterUserId,
                Configurations = configurations.Select(x =>
                    new WorkflowAssigneeConfigurationRequest
                    {
                        ConfigurationId = x.Id,
                        AssigneeType = (int)x.AssigneeType,
                        RoleId = x.RoleId,
                        IsPrimary = x.IsPrimary,
                        CreationTime = x.CreationTime,
                        UserIds = x.Users.Select(y => y.UserId).ToList(),
                        OrganizationUnitIds = x.OrganizationUnits
                            .Select(y => y.OrganizationUnitId).ToList()
                    }).ToList()
            },
            cancellationToken);
    }
}

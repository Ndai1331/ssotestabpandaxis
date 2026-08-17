using hanhchinhso.IdentityService.Internal;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Workflows;

public interface IWorkflowIdentityReferenceValidator
{
    Task ValidateAsync(
        IEnumerable<Guid> userIds,
        IEnumerable<Guid> organizationUnitIds,
        Guid? roleId,
        CancellationToken cancellationToken = default);
}

public sealed class WorkflowIdentityReferenceValidator :
    IWorkflowIdentityReferenceValidator,
    ITransientDependency
{
    private readonly WorkflowIdentityClient _client;

    public WorkflowIdentityReferenceValidator(WorkflowIdentityClient client)
    {
        _client = client;
    }

    public async Task ValidateAsync(
        IEnumerable<Guid> userIds,
        IEnumerable<Guid> organizationUnitIds,
        Guid? roleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.PostAsync<
            IdentityReferenceValidationRequest,
            IdentityReferenceValidationResult>(
            "api/app/identity-reference-validation/validate",
            new IdentityReferenceValidationRequest
        {
            UserIds = Normalize(userIds),
            OrganizationUnitIds = Normalize(organizationUnitIds),
            RoleIds = roleId is null || roleId == Guid.Empty ? [] : [roleId.Value]
        },
            cancellationToken);
        if (result.MissingOrInactiveUserIds.Count > 0)
        {
            throw new UserFriendlyException(
                $"Workflow assignee users do not exist or are disabled: {string.Join(", ", result.MissingOrInactiveUserIds)}.");
        }
        if (result.MissingOrganizationUnitIds.Count > 0)
        {
            throw new UserFriendlyException(
                $"Workflow organization units do not exist: {string.Join(", ", result.MissingOrganizationUnitIds)}.");
        }
        if (result.MissingRoleIds.Count > 0)
        {
            throw new UserFriendlyException(
                $"Workflow roles do not exist: {string.Join(", ", result.MissingRoleIds)}.");
        }
    }

    private static List<Guid> Normalize(IEnumerable<Guid>? ids) =>
        ids?.Where(x => x != Guid.Empty).Distinct().Order().ToList() ?? [];
}

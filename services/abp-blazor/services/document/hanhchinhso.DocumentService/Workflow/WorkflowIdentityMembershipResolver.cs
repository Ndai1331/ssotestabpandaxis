using hanhchinhso.IdentityService.Internal;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Workflows;

public interface IWorkflowIdentityMembershipResolver
{
    Task<IReadOnlySet<Guid>> ResolveAsync(
        Guid userId,
        IEnumerable<Guid> organizationUnitIds,
        CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> ResolveAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class WorkflowIdentityMembershipResolver :
    IWorkflowIdentityMembershipResolver,
    ITransientDependency
{
    private readonly WorkflowIdentityClient _client;

    public WorkflowIdentityMembershipResolver(WorkflowIdentityClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlySet<Guid>> ResolveAsync(
        Guid userId,
        IEnumerable<Guid> organizationUnitIds,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.PostAsync<
            IdentityUserOrganizationUnitMembershipRequest,
            IdentityUserOrganizationUnitMembershipResult>(
            "api/app/identity-reference-validation/resolve-user-organization-unit-memberships",
            new IdentityUserOrganizationUnitMembershipRequest
            {
                UserId = userId,
                OrganizationUnitIds = organizationUnitIds
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .Order()
                    .ToList()
            },
            cancellationToken);
        return result.OrganizationUnitIds.ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> ResolveAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.PostAsync<
            IdentityUserOrganizationUnitMembershipRequest,
            IdentityUserOrganizationUnitMembershipResult>(
            "api/app/identity-reference-validation/resolve-user-organization-unit-memberships",
            new IdentityUserOrganizationUnitMembershipRequest
            {
                UserId = userId,
                IncludeAllUserMemberships = true
            },
            cancellationToken);
        return result.OrganizationUnitIds.ToHashSet();
    }
}

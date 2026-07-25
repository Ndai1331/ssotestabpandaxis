using System.Security.Claims;
using hanhchinhso.IdentityService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Security.Claims;

namespace hanhchinhso.IdentityService.Internal;

[Authorize]
public class IdentityReferenceValidationAppService :
    ApplicationService,
    IIdentityReferenceValidationAppService
{
    internal const string DocumentServiceClientId = "DocumentService.Internal";

    private readonly IdentityServiceDbContext _dbContext;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public IdentityReferenceValidationAppService(
        IdentityServiceDbContext dbContext,
        ICurrentPrincipalAccessor currentPrincipalAccessor)
    {
        _dbContext = dbContext;
        _currentPrincipalAccessor = currentPrincipalAccessor;
    }

    public async Task<IdentityReferenceValidationResult> ValidateAsync(
        IdentityReferenceValidationRequest input)
    {
        EnsureDocumentServiceClient();

        var userIds = Normalize(input.UserIds);
        var organizationUnitIds = Normalize(input.OrganizationUnitIds);
        var roleIds = Normalize(input.RoleIds);

        var activeUserIds = await _dbContext.Users
            .Where(x => userIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();
        var existingOrganizationUnitIds = await _dbContext.OrganizationUnits
            .Where(x => organizationUnitIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();
        var existingRoleIds = await _dbContext.Roles
            .Where(x => roleIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        return new IdentityReferenceValidationResult
        {
            MissingOrInactiveUserIds = userIds.Except(activeUserIds).Order().ToList(),
            MissingOrganizationUnitIds = organizationUnitIds
                .Except(existingOrganizationUnitIds).Order().ToList(),
            MissingRoleIds = roleIds.Except(existingRoleIds).Order().ToList()
        };
    }

    public async Task<WorkflowAssigneeResolutionResult> ResolveWorkflowAssigneesAsync(
        WorkflowAssigneeResolutionRequest input)
    {
        EnsureDocumentServiceClient();
        if (input.SubmitterUserId == Guid.Empty ||
            input.Configurations.Count == 0 ||
            input.Configurations.Count > 100)
        {
            throw new UserFriendlyException(
                "Workflow assignee resolution request is invalid.");
        }

        var configurations = input.Configurations
            .GroupBy(x => x.ConfigurationId)
            .Select(x => x.First())
            .ToList();
        if (configurations.Any(x =>
                x.ConfigurationId == Guid.Empty ||
                x.AssigneeType is < 0 or > 2 ||
                x.UserIds.Count > 500 ||
                x.OrganizationUnitIds.Count > 500))
        {
            throw new UserFriendlyException(
                "Workflow assignee resolution configuration is invalid.");
        }
        if (!await _dbContext.Users.AnyAsync(x =>
                x.Id == input.SubmitterUserId && x.IsActive))
        {
            throw new UserFriendlyException(
                "The workflow submitter does not exist or is disabled.");
        }
        var submitterMemberships = await _dbContext
            .Set<Volo.Abp.Identity.IdentityUserOrganizationUnit>()
            .Where(x => x.UserId == input.SubmitterUserId)
            .OrderBy(x => x.CreationTime)
            .ThenBy(x => x.OrganizationUnitId)
            .ToListAsync();
        var organizationUnits = await _dbContext.OrganizationUnits
            .Select(x => new { x.Id, x.ParentId })
            .ToListAsync();
        var parents = organizationUnits.ToDictionary(x => x.Id, x => x.ParentId);
        submitterMemberships = submitterMemberships
            .Where(x => parents.ContainsKey(x.OrganizationUnitId))
            .ToList();
        var primaryOrganizationUnitId =
            submitterMemberships.FirstOrDefault()?.OrganizationUnitId;
        var primaryChain = BuildAncestorChain(primaryOrganizationUnitId, parents);
        var scopedOrganizationUnitIds = configurations
            .SelectMany(x => x.OrganizationUnitIds)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToHashSet();
        scopedOrganizationUnitIds.IntersectWith(parents.Keys);
        var relevantOrganizationUnitIds = scopedOrganizationUnitIds
            .Union(primaryChain)
            .ToHashSet();
        var relevantMemberships = await _dbContext
            .Set<Volo.Abp.Identity.IdentityUserOrganizationUnit>()
            .Where(x => relevantOrganizationUnitIds.Contains(x.OrganizationUnitId))
            .ToListAsync();
        var explicitUserIds = configurations
            .SelectMany(x => x.UserIds)
            .Where(x => x != Guid.Empty)
            .Distinct();
        var candidateUserIds = explicitUserIds
            .Union(relevantMemberships.Select(x => x.UserId))
            .ToHashSet();
        var users = await _dbContext.Users
            .Where(x => candidateUserIds.Contains(x.Id) && x.IsActive)
            .Select(x => new { x.Id, x.Name, x.UserName })
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Name.IsNullOrWhiteSpace() ? x.UserName : x.Name!);
        var requiredRoleIds = configurations
            .Where(x => x.RoleId.HasValue && x.RoleId != Guid.Empty)
            .Select(x => x.RoleId!.Value)
            .ToHashSet();
        var userRoles = await _dbContext
            .Set<Volo.Abp.Identity.IdentityUserRole>()
            .Where(x => requiredRoleIds.Contains(x.RoleId))
            .Select(x => new { x.UserId, x.RoleId })
            .ToListAsync();
        var rolePairs = userRoles
            .Select(x => (x.UserId, x.RoleId))
            .ToHashSet();

        var result = new WorkflowAssigneeResolutionResult
        {
            PrimarySubmitterOrganizationUnitId = primaryOrganizationUnitId,
            SubmitterOrganizationUnitIds = submitterMemberships
                .Select(x => x.OrganizationUnitId).Distinct().Order().ToList()
        };
        foreach (var configuration in configurations)
        {
            foreach (var candidate in ResolveConfiguration(
                         configuration,
                         users,
                         relevantMemberships,
                         primaryChain,
                         rolePairs))
            {
                result.Candidates.Add(candidate);
            }
        }
        return result;
    }

    public async Task<IdentityUserOrganizationUnitMembershipResult>
        ResolveUserOrganizationUnitMembershipsAsync(
            IdentityUserOrganizationUnitMembershipRequest input)
    {
        EnsureDocumentServiceClient();
        var organizationUnitIds = Normalize(input.OrganizationUnitIds);
        if (input.UserId == Guid.Empty ||
            (!input.IncludeAllUserMemberships &&
             organizationUnitIds.Count > 500))
        {
            throw new UserFriendlyException(
                "Identity membership request is invalid.");
        }
        var query = _dbContext
            .Set<Volo.Abp.Identity.IdentityUserOrganizationUnit>()
            .Where(x => x.UserId == input.UserId);
        if (!input.IncludeAllUserMemberships)
        {
            query = query.Where(x =>
                organizationUnitIds.Contains(x.OrganizationUnitId));
        }
        var memberships = await query
            .Select(x => x.OrganizationUnitId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        return new IdentityUserOrganizationUnitMembershipResult
        {
            OrganizationUnitIds = memberships
        };
    }

    private void EnsureDocumentServiceClient()
    {
        var clientId = _currentPrincipalAccessor.Principal.FindFirstValue(
            OpenIddictConstants.Claims.ClientId);
        if (!string.Equals(
                clientId,
                DocumentServiceClientId,
                StringComparison.Ordinal))
        {
            throw new AbpAuthorizationException(
                "Only the DocumentService internal client can validate identity references.");
        }
    }

    private static List<Guid> Normalize(IEnumerable<Guid>? ids) =>
        ids?.Where(x => x != Guid.Empty).Distinct().Order().ToList() ?? [];

    private static List<Guid> BuildAncestorChain(
        Guid? start,
        IReadOnlyDictionary<Guid, Guid?> parents)
    {
        var chain = new List<Guid>();
        var seen = new HashSet<Guid>();
        var current = start;
        while (current.HasValue && seen.Add(current.Value))
        {
            chain.Add(current.Value);
            current = parents.GetValueOrDefault(current.Value);
        }
        return chain;
    }

    private static IEnumerable<WorkflowResolvedCandidate> ResolveConfiguration(
        WorkflowAssigneeConfigurationRequest configuration,
        IReadOnlyDictionary<Guid, string> users,
        IReadOnlyCollection<Volo.Abp.Identity.IdentityUserOrganizationUnit> memberships,
        IReadOnlyList<Guid> primaryChain,
        IReadOnlySet<(Guid UserId, Guid RoleId)> rolePairs)
    {
        var candidates = new Dictionary<Guid, (Guid? OrganizationUnitId, int Depth)>();
        if (configuration.AssigneeType == 0)
        {
            foreach (var userId in configuration.UserIds.Where(users.ContainsKey))
            {
                candidates.TryAdd(userId, (null, int.MaxValue));
            }
        }
        else
        {
            var organizationUnitIds = configuration.AssigneeType == 1
                ? primaryChain.ToHashSet()
                : configuration.OrganizationUnitIds.ToHashSet();
            foreach (var membership in memberships
                         .Where(x => organizationUnitIds.Contains(x.OrganizationUnitId)))
            {
                var depth = configuration.AssigneeType == 1
                    ? primaryChain.ToList().IndexOf(membership.OrganizationUnitId)
                    : 0;
                var roleMatches = !configuration.RoleId.HasValue ||
                    rolePairs.Contains((membership.UserId, configuration.RoleId.Value));
                if (users.ContainsKey(membership.UserId) && roleMatches &&
                    (!candidates.TryGetValue(membership.UserId, out var existing) ||
                     depth < existing.Depth))
                {
                    candidates[membership.UserId] =
                        (membership.OrganizationUnitId, depth);
                }
            }
            if (configuration.AssigneeType == 2)
            {
                foreach (var userId in configuration.UserIds.Where(users.ContainsKey))
                {
                    candidates.TryAdd(userId, (null, int.MaxValue));
                }
            }
        }

        return candidates.Select(x => new WorkflowResolvedCandidate
        {
            ConfigurationId = configuration.ConfigurationId,
            UserId = x.Key,
            DisplayName = users[x.Key],
            ProvenanceOrganizationUnitId = x.Value.OrganizationUnitId,
            ProvenanceRoleId = configuration.RoleId,
            OrganizationUnitDepth = x.Value.Depth,
            IsPrimaryConfiguration = configuration.IsPrimary,
            ConfigurationCreationTime = configuration.CreationTime
        });
    }
}

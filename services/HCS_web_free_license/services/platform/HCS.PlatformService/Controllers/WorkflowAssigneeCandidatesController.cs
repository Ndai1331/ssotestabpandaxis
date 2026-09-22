using HCS.EntityFrameworkCore;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.PlatformService.Controllers;

[ApiController, Authorize, Route("api/identity/workflow-assignees")]
public sealed class WorkflowAssigneeCandidatesController(
    IIdentityUserRepository identityUsers,
    ICurrentUser currentUser,
    HCSDbContext identityDb) : ControllerBase
{
    [HttpGet("roles")]
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>> GetRolesAsync(
        [FromQuery] Guid[] roleIds,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.Id is not { } submitterUserId)
            return new Dictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>();

        var ids = roleIds.Where(x => x != Guid.Empty).Distinct().Take(100).ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>();

        var submitterOuSet = await LoadUserOuIdsAsync(submitterUserId, cancellationToken);
        var ouPeerUserIds = submitterOuSet.Count == 0
            ? null
            : await LoadOuPeerUserIdsAsync(submitterOuSet, cancellationToken);

        var roleMemberships = await identityDb.Set<IdentityUserRole>().AsNoTracking()
            .Where(x => ids.Contains(x.RoleId))
            .Select(x => new { x.RoleId, x.UserId })
            .ToListAsync(cancellationToken);

        if (ouPeerUserIds is not null)
            roleMemberships = roleMemberships.Where(x => ouPeerUserIds.Contains(x.UserId)).ToList();

        var userIdsByRole = roleMemberships
            .GroupBy(x => x.RoleId)
            .ToDictionary(x => x.Key, x => x.Select(item => item.UserId).Distinct().Take(200).ToArray());

        return await BuildCandidateGroupsAsync(ids, userIdsByRole, submitterOuSet, cancellationToken);
    }

    [HttpGet]
    public async Task<IReadOnlyList<WorkflowAssigneeCandidateDto>> GetAsync(
        [FromQuery] Guid roleId,
        CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty || currentUser.Id is not { } submitterUserId)
            return [];

        var groups = await GetRolesAsync([roleId], cancellationToken);
        return groups.GetValueOrDefault(roleId) ?? [];
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<IReadOnlyList<WorkflowAssigneeCandidateDto>>> LookupUsersAsync(
        [FromQuery] Guid[] userIds,
        CancellationToken cancellationToken = default)
    {
        if (!CanResolveUserLookup()) return Forbid();
        var ids = userIds.Where(x => x != Guid.Empty).Distinct().Take(200).ToArray();
        if (ids.Length == 0) return Ok(Array.Empty<WorkflowAssigneeCandidateDto>());

        // Query the scoped Identity DbContext once. Running one FindAsync per ID
        // made this endpoint an N+1 query from list pages such as document-signing.
        var users = await identityUsers.GetListByIdsAsync(ids, includeDetails: false, cancellationToken: cancellationToken);

        return Ok(users
            .Where(user => user.IsActive)
            .Select(user => new WorkflowAssigneeCandidateDto(
                user.Id, DisplayName(user), null, user.UserName))
            .ToArray());
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<WorkflowAssigneeCandidateDto>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return NotFound();
        if (!User.HasClaim("permission", HCSPermissions.Documents.WorkflowStart))
            return Forbid();

        var user = await identityUsers.FindAsync(userId, includeDetails: false, cancellationToken: cancellationToken);
        if (user is null || !user.IsActive)
            return NotFound();

        var ouId = await identityDb.Set<IdentityUserOrganizationUnit>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.OrganizationUnitId)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new WorkflowAssigneeCandidateDto(
            user.Id,
            DisplayName(user),
            ouId,
            user.UserName));
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>> BuildCandidateGroupsAsync(
        Guid[] roleIds,
        IReadOnlyDictionary<Guid, Guid[]> userIdsByRole,
        HashSet<Guid> submitterOuSet,
        CancellationToken cancellationToken)
    {
        var allUserIds = userIdsByRole.Values.SelectMany(x => x).Distinct().ToArray();
        var users = allUserIds.Length == 0
            ? []
            : await identityUsers.GetListByIdsAsync(allUserIds, includeDetails: false, cancellationToken: cancellationToken);
        var usersById = users.Where(x => x.IsActive).ToDictionary(x => x.Id);
        var primaryOuByUser = await LoadPrimaryOuByUserAsync(allUserIds, submitterOuSet, cancellationToken);

        return roleIds.ToDictionary(roleId => roleId, roleId => (IReadOnlyList<WorkflowAssigneeCandidateDto>)
            userIdsByRole.GetValueOrDefault(roleId, [])
                .Where(usersById.ContainsKey)
                .Select(userId =>
                {
                    var user = usersById[userId];
                    return new WorkflowAssigneeCandidateDto(
                        user.Id,
                        DisplayName(user),
                        primaryOuByUser.GetValueOrDefault(userId),
                        user.UserName);
                })
                .DistinctBy(x => x.UserId)
                .ToArray());
    }

    private async Task<HashSet<Guid>> LoadUserOuIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ids = await identityDb.Set<IdentityUserOrganizationUnit>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.OrganizationUnitId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    private async Task<HashSet<Guid>> LoadOuPeerUserIdsAsync(
        HashSet<Guid> organizationUnitIds,
        CancellationToken cancellationToken)
    {
        var peers = await identityDb.Set<IdentityUserOrganizationUnit>().AsNoTracking()
            .Where(x => organizationUnitIds.Contains(x.OrganizationUnitId))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return peers.ToHashSet();
    }

    private async Task<Dictionary<Guid, Guid?>> LoadPrimaryOuByUserAsync(
        Guid[] userIds,
        HashSet<Guid> preferredOuIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
            return [];

        var rows = await identityDb.Set<IdentityUserOrganizationUnit>().AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.OrganizationUnitId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var shared = group
                        .Select(x => x.OrganizationUnitId)
                        .FirstOrDefault(id => preferredOuIds.Contains(id));
                    if (shared != Guid.Empty)
                        return (Guid?)shared;
                    return group.Select(x => (Guid?)x.OrganizationUnitId).FirstOrDefault();
                });
    }

    private static string DisplayName(IdentityUser user)
    {
        var name = string.Join(' ', new[] { user.Surname, user.Name }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        return string.IsNullOrWhiteSpace(name) ? user.UserName : name;
    }

    private bool CanResolveUserLookup() =>
        User.HasClaim("permission", HCSPermissions.Documents.View)
        || User.HasClaim("permission", HCSPermissions.Documents.Assign)
        || User.HasClaim("permission", HCSPermissions.Documents.SigningExecute)
        || User.HasClaim("permission", HCSPermissions.Documents.WorkflowStart)
        || User.HasClaim("permission", HCSPermissions.Collaboration.Chat)
        || User.HasClaim("permission", HCSPermissions.WorkManagement.Dashboard);
}

public sealed record WorkflowAssigneeCandidateDto(Guid UserId, string DisplayName, Guid? OrganizationUnitId,
    string? UserName = null);

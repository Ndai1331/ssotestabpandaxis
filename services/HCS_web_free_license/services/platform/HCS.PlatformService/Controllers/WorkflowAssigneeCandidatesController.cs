using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.PlatformService.Controllers;

[ApiController, Authorize, Route("api/identity/workflow-assignees")]
public sealed class WorkflowAssigneeCandidatesController(
    IIdentityUserRepository identityUsers,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<WorkflowAssigneeCandidateDto>> GetAsync(
        [FromQuery] Guid roleId,
        CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty || currentUser.Id is not { } submitterUserId)
            return [];

        var candidates = await identityUsers.GetListAsync(
            sorting: "UserName",
            maxResultCount: 200,
            roleId: roleId,
            notActive: false,
            includeDetails: true,
            cancellationToken: cancellationToken);

        var submitter = await identityUsers.FindAsync(submitterUserId, includeDetails: true, cancellationToken: cancellationToken);
        var submitterOuIds = submitter?.OrganizationUnits.Select(x => x.OrganizationUnitId).ToHashSet() ?? [];
        var scoped = submitterOuIds.Count == 0
            ? candidates
            : candidates.Where(user => user.OrganizationUnits.Any(ou => submitterOuIds.Contains(ou.OrganizationUnitId))).ToList();

        return scoped
            .Where(user => user.IsActive)
            .Select(user => new WorkflowAssigneeCandidateDto(
                user.Id,
                DisplayName(user),
                user.OrganizationUnits.Select(x => x.OrganizationUnitId).FirstOrDefault()))
            .DistinctBy(x => x.UserId)
            .ToArray();
    }

    private static string DisplayName(IdentityUser user)
    {
        var name = string.Join(' ', new[] { user.Surname, user.Name }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        return string.IsNullOrWhiteSpace(name) ? user.UserName : name;
    }
}

public sealed record WorkflowAssigneeCandidateDto(Guid UserId, string DisplayName, Guid? OrganizationUnitId);
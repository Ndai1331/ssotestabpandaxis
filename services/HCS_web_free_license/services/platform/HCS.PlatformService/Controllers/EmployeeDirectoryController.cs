using HCS.CollaborationService.Contracts;
using HCS.EntityFrameworkCore;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.PlatformService.Controllers;

public sealed record EmployeeDirectoryUserDto(Guid UserId, string UserName, string DisplayName, string? AvatarUrl);
public sealed record EmployeeDirectoryPageDto(IReadOnlyList<EmployeeDirectoryUserDto> Items, bool HasMore);

[ApiController]
[Authorize]
[Route("api/identity/employee-directory")]
public sealed class EmployeeDirectoryController(
    IIdentityUserRepository identityUsers,
    HCSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EmployeeDirectoryPageDto>> List(
        [FromQuery] string? filter = null,
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 100,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadDirectory()) return Forbid();
        var max = Math.Clamp(maxResultCount, 1, 500);
        var users = await identityUsers.GetListAsync(
            sorting: "UserName",
            skipCount: Math.Max(0, skipCount),
            maxResultCount: max,
            filter: string.IsNullOrWhiteSpace(filter) ? null : filter.Trim(),
            notActive: false,
            cancellationToken: cancellationToken);
        return Ok(new EmployeeDirectoryPageDto(await MapUsersAsync(users.Where(x => x.IsActive), cancellationToken),
            users.Count >= max));
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<EmployeeDirectoryUserDto>> Get(Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadDirectory()) return Forbid();
        var user = await identityUsers.FindAsync(userId, includeDetails: false, cancellationToken: cancellationToken);
        if (user is null || !user.IsActive) return NotFound();
        var users = await MapUsersAsync([user], cancellationToken);
        return users.Count == 0 ? NotFound() : Ok(users[0]);
    }

    private async Task<IReadOnlyList<EmployeeDirectoryUserDto>> MapUsersAsync(
        IEnumerable<IdentityUser> users, CancellationToken cancellationToken)
    {
        var activeUsers = users.ToArray();
        if (activeUsers.Length == 0) return [];
        var userIds = activeUsers.Select(x => x.Id).ToArray();
        var avatarIds = await db.UserAvatars.AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => x.UserId)
            .ToHashSetAsync(cancellationToken);
        return activeUsers.Select(user => new EmployeeDirectoryUserDto(
            user.Id,
            user.UserName,
            UserDisplayNames.FromPerson(user.Surname, user.Name, user.UserName),
            avatarIds.Contains(user.Id) ? $"/api/identity/users/{user.Id:D}/avatar" : null)).ToArray();
    }

    private bool CanReadDirectory() =>
        User.IsInRole("admin")
        || User.HasClaim("permission", HCSPermissions.WorkManagement.EmployeeRatings)
        || User.HasClaim("permission", HCSPermissions.WorkManagement.EmployeeRatingsManagement)
        || User.HasClaim("permission", HCSPermissions.WorkManagement.EmployeeRatingsDashboard);
}

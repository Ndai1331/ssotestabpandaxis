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
public sealed record EmployeeDirectoryPageDto(IReadOnlyList<EmployeeDirectoryUserDto> Items, long TotalCount, bool HasMore);

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
        [FromQuery] int maxResultCount = 20,
        [FromQuery] Guid[]? userIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadDirectory()) return Forbid();
        if (userIds is { Length: > 0 })
        {
            var ids = userIds.Where(id => id != Guid.Empty).Distinct().Take(200).ToArray();
            var found = await identityUsers.GetListByIdsAsync(ids, includeDetails: false, cancellationToken);
            var mapped = await MapUsersAsync(found.Where(x => x.IsActive), cancellationToken);
            return Ok(new EmployeeDirectoryPageDto(mapped, mapped.Count, false));
        }

        var max = Math.Clamp(maxResultCount, 1, 100);
        var skip = Math.Max(0, skipCount);
        var trimmed = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();
        var total = await identityUsers.GetCountAsync(
            filter: trimmed,
            notActive: false,
            cancellationToken: cancellationToken);
        var users = await identityUsers.GetListAsync(
            sorting: "UserName",
            skipCount: skip,
            maxResultCount: max,
            filter: trimmed,
            notActive: false,
            cancellationToken: cancellationToken);
        var items = await MapUsersAsync(users, cancellationToken);
        return Ok(new EmployeeDirectoryPageDto(items, total, skip + items.Count < total));
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

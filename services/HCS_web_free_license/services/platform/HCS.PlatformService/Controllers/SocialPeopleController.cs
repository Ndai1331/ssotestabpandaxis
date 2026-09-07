using HCS.CollaborationService.Contracts;
using HCS.EntityFrameworkCore;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.PlatformService.Controllers;

[ApiController, Authorize(Policy = HCSPermissions.Collaboration.Social), Route("api/identity")]
public sealed class SocialPeopleController(
    IIdentityUserRepository identityUsers,
    HCSDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("social-people")]
    public async Task<IReadOnlyList<SocialPersonDto>> SearchAsync(
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = NormalizeSearch(search);
        if (normalizedSearch is null)
            return [];

        var users = await identityUsers.GetListAsync(
            sorting: "UserName",
            maxResultCount: Math.Clamp(take, 1, 30),
            filter: normalizedSearch,
            notActive: false,
            cancellationToken: cancellationToken);
        return await MapUsersAsync(users.Where(user => user.IsActive), cancellationToken);
    }

    [HttpGet("social-profile")]
    public async Task<ActionResult<SocialPersonDto>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.Id is not { } userId)
            return Unauthorized();

        var user = await identityUsers.FindAsync(userId, includeDetails: false, cancellationToken: cancellationToken);
        if (user is null || !user.IsActive)
            return NotFound();

        var people = await MapUsersAsync([user], cancellationToken);
        return people.Count == 0 ? NotFound() : Ok(people[0]);
    }

    private async Task<IReadOnlyList<SocialPersonDto>> MapUsersAsync(
        IEnumerable<IdentityUser> users,
        CancellationToken cancellationToken)
    {
        var activeUsers = users.ToArray();
        if (activeUsers.Length == 0)
            return [];

        var userIds = activeUsers.Select(user => user.Id).ToArray();
        var avatarIds = await db.UserAvatars.AsNoTracking()
            .Where(avatar => userIds.Contains(avatar.UserId))
            .Select(avatar => avatar.UserId)
            .ToHashSetAsync(cancellationToken);

        return activeUsers.Select(user => new SocialPersonDto(
            user.Id,
            user.UserName,
            UserDisplayNames.FromPerson(user.Surname, user.Name, user.UserName),
            user.Email,
            user.PhoneNumber,
            avatarIds.Contains(user.Id) ? $"/api/identity/users/{user.Id:D}/avatar" : null))
            .ToArray();
    }

    private static string? NormalizeSearch(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

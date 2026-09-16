using HCS.CollaborationService.Contracts;
using HCS.EntityFrameworkCore;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.PlatformService.Controllers;

[ApiController, Authorize(Policy = HCSPermissions.Collaboration.ChatRead), Route("api/chat/contacts")]
public sealed class ChatContactsController(
    ICurrentUser currentUser,
    HCSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ChatContactDto>> GetAsync(
        [FromQuery] string? search,
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var page = await GetPageAsync(search, skip: 0, take, cancellationToken);
        return page.Items;
    }

    [HttpGet("page")]
    public async Task<PagedChatContactsDto> GetPageAsync(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = NormalizeSearch(search);
        var currentUserId = currentUser.Id;
        var query = db.Users
            .AsNoTracking()
            .Where(user => user.IsActive && (!currentUserId.HasValue || user.Id != currentUserId.Value));

        if (searchTerm is not null)
        {
            query = query.Where(user =>
                ((user.Surname ?? string.Empty) + " " + (user.Name ?? string.Empty))
                    .ToLower().Contains(searchTerm) ||
                (user.PhoneNumber != null && user.PhoneNumber.ToLower().Contains(searchTerm)) ||
                (user.Email != null && user.Email.ToLower().Contains(searchTerm)) ||
                user.UserName.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var users = await query
            .OrderBy(user => user.UserName)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(cancellationToken);

        return new PagedChatContactsDto(totalCount, await MapUsersAsync(users, cancellationToken));
    }

    private static string? NormalizeSearch(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private async Task<IReadOnlyList<ChatContactDto>> MapUsersAsync(
        IEnumerable<IdentityUser> users,
        CancellationToken cancellationToken)
    {
        var mappedUsers = users.ToArray();
        if (mappedUsers.Length == 0)
        {
            return [];
        }

        var userIds = mappedUsers.Select(user => user.Id).ToArray();
        var avatarUserIds = await db.UserAvatars
            .AsNoTracking()
            .Where(avatar => userIds.Contains(avatar.UserId))
            .Select(avatar => avatar.UserId)
            .ToHashSetAsync(cancellationToken);

        return mappedUsers
            .Select(user => new ChatContactDto(
                user.Id,
                user.UserName,
                UserDisplayNames.FromPerson(user.Surname, user.Name, user.UserName),
                user.IsActive,
                user.Surname,
                user.Name,
                user.PhoneNumber,
                avatarUserIds.Contains(user.Id)
                    ? $"/api/identity/users/{user.Id:D}/avatar"
                    : null))
            .ToArray();
    }
}

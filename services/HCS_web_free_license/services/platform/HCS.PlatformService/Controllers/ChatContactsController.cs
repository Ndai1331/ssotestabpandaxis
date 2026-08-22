using HCS.CollaborationService.Contracts;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.PlatformService.Controllers;

[ApiController, Authorize(Policy = HCSPermissions.Collaboration.Chat), Route("api/chat/contacts")]
public sealed class ChatContactsController(
    IIdentityUserRepository identityUsers,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ChatContactDto>> GetAsync(
        [FromQuery] string? search,
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = NormalizeSearch(search);
        var users = await identityUsers.GetListAsync(
            sorting: "UserName",
            maxResultCount: Math.Clamp(take, 1, 50),
            filter: normalizedSearch,
            notActive: false,
            cancellationToken: cancellationToken);
        var currentUserId = currentUser.Id;

        return users
            .Where(user => user.IsActive && (!currentUserId.HasValue || user.Id != currentUserId.Value))
            .Select(user => new ChatContactDto(
                user.Id,
                user.UserName,
                UserDisplayNames.FromPerson(user.Surname, user.Name, user.UserName),
                user.IsActive,
                user.Surname,
                user.Name,
                user.PhoneNumber,
                $"/api/identity/users/{user.Id:D}/avatar"))
            .ToArray();
    }

    private static string? NormalizeSearch(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

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
        var users = await identityUsers.GetListAsync(
            sorting: "UserName",
            maxResultCount: Math.Clamp(take, 1, 50),
            filter: string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            notActive: false,
            cancellationToken: cancellationToken);
        var currentUserId = currentUser.Id;

        return users
            .Where(user => user.IsActive && (!currentUserId.HasValue || user.Id != currentUserId.Value))
            .Select(user => new ChatContactDto(
                user.Id,
                user.UserName,
                string.Join(' ', new[] { user.Name, user.Surname }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim() is { Length: > 0 } displayName
                    ? displayName
                    : user.UserName,
                user.IsActive))
            .ToArray();
    }
}

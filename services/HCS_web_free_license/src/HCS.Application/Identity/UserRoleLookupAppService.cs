using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Identity;

namespace HCS.Identity;

[Authorize(IdentityPermissions.Users.Default)]
public sealed class UserRoleLookupAppService(IIdentityUserRepository userRepository)
    : HCSAppService, IUserRoleLookupAppService
{
    public async Task<IReadOnlyList<UserRoleLookupDto>> GetUserRolesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().Take(200).ToArray();
        if (ids.Length == 0) return [];

        var rolesByUser = (await userRepository.GetRoleNamesAsync(ids, cancellationToken))
            .ToDictionary(item => item.Id, item => item.RoleNames);

        return ids.Select(userId => new UserRoleLookupDto(
                userId,
                rolesByUser.TryGetValue(userId, out var roleNames) ? roleNames : []))
            .ToArray();
    }
}

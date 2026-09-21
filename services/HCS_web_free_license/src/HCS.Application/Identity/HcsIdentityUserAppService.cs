using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace HCS.Identity;

/// <summary>
/// ABP Identity only assigns arbitrary roles when <c>CurrentUser.IsInRole("admin")</c>.
/// HCS access tokens carry permission claims, not always <c>role</c>, so that check
/// silently dropped selected roles while still returning HTTP 200.
/// Phone numbers are normalized so administration can create, update, and clear them.
/// </summary>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityUserAppService), typeof(IdentityUserAppService), typeof(HcsIdentityUserAppService))]
public class HcsIdentityUserAppService : IdentityUserAppService
{
    public HcsIdentityUserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IOptions<IdentityOptions> identityOptions,
        IPermissionChecker permissionChecker)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
    }

    protected override async Task<bool> HasAdminRoleAsync()
    {
        if (CurrentUser.IsInRole("admin")
            || CurrentUser.Roles.Any(role => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return await PermissionChecker.IsGrantedAsync(IdentityPermissions.Users.ManageRoles);
    }

    protected override async Task UpdateUserByInput(IdentityUser user, IdentityUserCreateOrUpdateDtoBase input)
    {
        input.PhoneNumber = IdentityPhoneNumbers.Normalize(input.PhoneNumber);
        await base.UpdateUserByInput(user, input);

        if (!string.Equals(user.PhoneNumber, input.PhoneNumber, StringComparison.Ordinal))
        {
            (await UserManager.SetPhoneNumberAsync(user, input.PhoneNumber)).CheckErrors();
        }
    }
}

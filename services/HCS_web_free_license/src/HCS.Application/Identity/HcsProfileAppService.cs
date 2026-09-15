using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.Identity;

/// <summary>
/// Imported hospital accounts keep <c>IsExternal = true</c> even when they have a
/// local password. ABP blocks every external user from changing password here.
/// Allow the change when the user actually has a local password.
/// </summary>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IProfileAppService), typeof(ProfileAppService), typeof(HcsProfileAppService))]
public class HcsProfileAppService : ProfileAppService
{
    public HcsProfileAppService(
        IdentityUserManager userManager,
        IOptions<IdentityOptions> identityOptions)
        : base(userManager, identityOptions)
    {
    }

    public override async Task ChangePasswordAsync(ChangePasswordInput input)
    {
        await IdentityOptions.SetAsync();

        var currentUser = await UserManager.GetByIdAsync(CurrentUser.GetId());
        if (!await UserManager.HasPasswordAsync(currentUser))
        {
            throw new BusinessException(code: IdentityErrorCodes.ExternalUserPasswordChange);
        }

        (await UserManager.ChangePasswordAsync(
            currentUser,
            input.CurrentPassword,
            input.NewPassword)).CheckErrors();
    }
}

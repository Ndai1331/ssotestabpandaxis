using System;
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
/// Also persist phone-number edits from the account profile page.
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

    public override async Task<ProfileDto> UpdateAsync(UpdateProfileDto input)
    {
        input.PhoneNumber = IdentityPhoneNumbers.Normalize(input.PhoneNumber);
        var profile = await base.UpdateAsync(input);

        var user = await UserManager.GetByIdAsync(CurrentUser.GetId());
        if (!string.Equals(user.PhoneNumber, input.PhoneNumber, StringComparison.Ordinal))
        {
            (await UserManager.SetPhoneNumberAsync(user, input.PhoneNumber)).CheckErrors();
            (await UserManager.UpdateAsync(user)).CheckErrors();
            if (CurrentUnitOfWork is not null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            profile = ObjectMapper.Map<IdentityUser, ProfileDto>(user);
        }

        return profile;
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

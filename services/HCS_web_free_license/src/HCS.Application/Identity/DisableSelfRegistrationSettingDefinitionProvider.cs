using Volo.Abp.Account.Settings;
using Volo.Abp.Settings;

namespace HCS.Identity;

/// <summary>
/// Accounts are provisioned by administrators. Linked into the auth-server as well, because the
/// register page lives there while <c>/api/account/register</c> is hosted by Platform.
/// Must run after ABP's account setting provider, so it only lives in modules depending on AbpAccountApplicationModule.
/// </summary>
public class DisableSelfRegistrationSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        var selfRegistration = context.GetOrNull(AccountSettingNames.IsSelfRegistrationEnabled);
        if (selfRegistration != null)
        {
            selfRegistration.DefaultValue = false.ToString();
        }
    }
}

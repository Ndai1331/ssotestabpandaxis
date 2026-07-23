using abptestwithsso.IdentityService.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace abptestwithsso.IdentityService.Permissions;

public class IdentityServicePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        //var myGroup = context.AddGroup(IdentityServicePermissions.GroupName);
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<IdentityServiceResource>(name);
    }
}
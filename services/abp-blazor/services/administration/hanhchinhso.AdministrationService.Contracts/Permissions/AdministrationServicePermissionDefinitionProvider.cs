using hanhchinhso.AdministrationService.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace hanhchinhso.AdministrationService.Permissions;

public class AdministrationServicePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var administrationGroup = context.AddGroup(AdministrationServicePermissions.GroupName, L("AdministrationService"));

        administrationGroup.AddPermission(AdministrationServicePermissions.Dashboard.Host, L("Permission:Dashboard"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AdministrationServiceResource>(name);
    }
}
using hanhchinhso.AIManagementService.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace hanhchinhso.AIManagementService.Permissions;

public class AIManagementServicePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        //var myGroup = context.AddGroup(AIManagementServicePermissions.GroupName);
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AIManagementServiceResource>(name);
    }
}

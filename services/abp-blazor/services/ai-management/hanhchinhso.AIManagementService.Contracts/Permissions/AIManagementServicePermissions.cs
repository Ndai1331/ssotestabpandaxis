using Volo.Abp.Reflection;

namespace hanhchinhso.AIManagementService.Permissions;

public class AIManagementServicePermissions
{
    public const string GroupName = "AIManagementService";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AIManagementServicePermissions));
    }
}

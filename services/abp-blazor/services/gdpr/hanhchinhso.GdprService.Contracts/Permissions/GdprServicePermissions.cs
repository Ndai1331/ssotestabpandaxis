using Volo.Abp.Reflection;

namespace hanhchinhso.GdprService.Permissions;

public class GdprServicePermissions
{
    public const string GroupName = "GdprService";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(GdprServicePermissions));
    }
}
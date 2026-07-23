using Volo.Abp.Reflection;

namespace abptestwithsso.AdministrationService.Permissions;

public class AdministrationServicePermissions
{
    public const string GroupName = "AdministrationService";

    public static class Dashboard
    {
        public const string DashboardGroup = GroupName + ".Dashboard";
        public const string Host = DashboardGroup + ".Host";
    }
    
    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AdministrationServicePermissions));
    }
}
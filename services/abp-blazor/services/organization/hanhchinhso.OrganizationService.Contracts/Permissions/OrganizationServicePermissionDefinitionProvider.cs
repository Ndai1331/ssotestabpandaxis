using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace hanhchinhso.OrganizationService.Permissions;

public class OrganizationServicePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(
            OrganizationServicePermissions.GroupName,
            L("Permission:Organization"));
        var masterData = group.AddPermission(
            OrganizationServicePermissions.MasterData,
            L("Permission:MasterData"));
        masterData.AddChild(OrganizationServicePermissions.Create, L("Permission:Create"));
        masterData.AddChild(OrganizationServicePermissions.Update, L("Permission:Update"));
        masterData.AddChild(OrganizationServicePermissions.Delete, L("Permission:Delete"));
        AddCrud(group, OrganizationServicePermissions.Units.Default, OrganizationServicePermissions.Units.Create,
            OrganizationServicePermissions.Units.Update, OrganizationServicePermissions.Units.Delete, "Permission:Units");
        AddCrud(group, OrganizationServicePermissions.Positions.Default, OrganizationServicePermissions.Positions.Create,
            OrganizationServicePermissions.Positions.Update, OrganizationServicePermissions.Positions.Delete, "Permission:Positions");
        AddCrud(group, OrganizationServicePermissions.Departments.Default, OrganizationServicePermissions.Departments.Create,
            OrganizationServicePermissions.Departments.Update, OrganizationServicePermissions.Departments.Delete, "Permission:Departments");
        AddCrud(group, OrganizationServicePermissions.UserDepartments.Default, OrganizationServicePermissions.UserDepartments.Create,
            OrganizationServicePermissions.UserDepartments.Update, OrganizationServicePermissions.UserDepartments.Delete, "Permission:UserDepartments");
    }

    private static void AddCrud(PermissionGroupDefinition group, string name, string create, string update,
        string delete, string displayName)
    {
        var permission = group.AddPermission(name, L(displayName));
        permission.AddChild(create, L("Permission:Create"));
        permission.AddChild(update, L("Permission:Update"));
        permission.AddChild(delete, L("Permission:Delete"));
    }

    private static LocalizableString L(string name) =>
        LocalizableString.Create<Localization.OrganizationServiceResource>(name);
}

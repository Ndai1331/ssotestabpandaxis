using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.Authorization.Permissions;

namespace HCS.Data;

/// <summary>
/// Keeps the permissions introduced by the application available to the built-in
/// roles without revoking grants made through the Roles screen.
///
/// DbMigrator calls this through the normal data seed pipeline. Auth Server also
/// calls it at startup so running the web services in an IDE is enough to make a
/// newly deployed permission available to existing roles.
/// </summary>
public sealed class HCSRolePermissionSynchronizer(
    IIdentityRoleRepository roleRepository,
    IPermissionDataSeeder permissionDataSeeder,
    IPermissionDefinitionManager permissionDefinitionManager) : ITransientDependency
{
    private static readonly string[] RoleNames = ["admin", "bacsi", "lanhdao", "nhanvien"];

    public async Task SynchronizeExistingRolesAsync()
    {
        var permissions = (await permissionDefinitionManager.GetPermissionsAsync())
            .Where(permission => permission.IsEnabled)
            .Select(permission => permission.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Admin is the local break-glass administrator. Seed every enabled
        // permission, but never revoke grants configured through the Roles UI.
        if (await RoleExistsAsync("admin"))
        {
            await permissionDataSeeder.SeedAsync(
                RolePermissionValueProvider.ProviderName,
                "admin",
                permissions);
        }

        // These roles are the default operational roles shipped with HCS. The
        // grants are additive; custom role assignments remain managed by HR/admin.
        var operationalPermissions = permissions
            .Where(permission =>
                (permission.StartsWith("WorkManagement.", StringComparison.Ordinal) ||
                 permission.StartsWith("Documents.", StringComparison.Ordinal) ||
                 permission.StartsWith("Collaboration.", StringComparison.Ordinal)) &&
                permission is not "WorkManagement.EmployeeRatings.Management" &&
                permission is not "WorkManagement.EmployeeRatings.Dashboard")
            .ToArray();

        foreach (var roleName in RoleNames.Where(role => !string.Equals(role, "admin", StringComparison.Ordinal)))
        {
            if (!await RoleExistsAsync(roleName))
            {
                continue;
            }

            await permissionDataSeeder.SeedAsync(
                RolePermissionValueProvider.ProviderName,
                roleName,
                operationalPermissions);
        }
    }

    private async Task<bool> RoleExistsAsync(string roleName) =>
        await roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant()) is not null;
}

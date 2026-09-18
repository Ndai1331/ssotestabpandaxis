using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace HCS.Data;

/// <summary>
/// Grants every enabled permission to the default <c>admin</c> role, including
/// permissions added after the role's persisted grants were last customized.
/// Other roles are created and granted only by administrators; this synchronizer
/// never inserts product roles such as <c>bacsi</c>, <c>lanhdao</c>, or <c>nhanvien</c>.
/// </summary>
public sealed class HCSRolePermissionSynchronizer(
    IIdentityRoleRepository roleRepository,
    IPermissionDataSeeder permissionDataSeeder,
    IPermissionDefinitionManager permissionDefinitionManager) : ITransientDependency
{
    public const string CustomPermissionsProperty = "HCS.CustomRolePermissions";

    public static readonly string[] EmployeeDefaultPermissions =
    [
        "WorkManagement.Dashboard",
        "WorkManagement.Projects",
        "WorkManagement.ProjectTasks",
        "WorkManagement.Calendar",
        "WorkManagement.EmployeeRatings",
        "Documents.View",
        "Documents.Signing.Execute",
        "Collaboration.Chat",
        "Collaboration.Social",
        "Collaboration.Notifications"
    ];

    public async Task SynchronizeExistingRolesAsync()
    {
        var adminRole = await roleRepository.FindByNormalizedNameAsync("ADMIN");
        if (adminRole is null)
        {
            return;
        }

        await NormalizeAdminFlagsAsync(adminRole);

        var permissions = (await permissionDefinitionManager.GetPermissionsAsync())
            .Where(permission => permission.IsEnabled)
            .Select(permission => permission.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Always grant newly introduced permissions to admin. CustomPermissions
        // only skips a destructive reset; SeedAsync adds missing grants.
        await permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            "admin",
            permissions);
    }

    public static void ApplyAdminFlags(IdentityRole adminRole)
    {
        ArgumentNullException.ThrowIfNull(adminRole);
        adminRole.IsPublic = false;
        adminRole.IsDefault = false;
    }

    private async Task NormalizeAdminFlagsAsync(IdentityRole adminRole)
    {
        if (!adminRole.IsPublic && !adminRole.IsDefault)
        {
            return;
        }

        ApplyAdminFlags(adminRole);
        await roleRepository.UpdateAsync(adminRole, autoSave: true);
    }

    public static string[] PermissionsToGrant(string roleName, IReadOnlyCollection<string> enabledPermissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        ArgumentNullException.ThrowIfNull(enabledPermissions);

        var enabled = enabledPermissions as HashSet<string>
            ?? enabledPermissions.ToHashSet(StringComparer.Ordinal);

        if (string.Equals(roleName, "admin", StringComparison.Ordinal))
        {
            return enabled.ToArray();
        }

        if (string.Equals(roleName, "nhanvien", StringComparison.Ordinal))
        {
            return EmployeeDefaultPermissions.Where(enabled.Contains).ToArray();
        }

        return enabled
            .Where(IsOperationalPermission)
            .ToArray();
    }

    public static string[] PermissionsToRevoke(string roleName, IReadOnlyCollection<string> enabledPermissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        ArgumentNullException.ThrowIfNull(enabledPermissions);

        if (!string.Equals(roleName, "nhanvien", StringComparison.Ordinal))
        {
            return [];
        }

        var keep = EmployeeDefaultPermissions.ToHashSet(StringComparer.Ordinal);
        return enabledPermissions
            .Where(IsProductAppPermission)
            .Where(permission => !keep.Contains(permission))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsOperationalPermission(string permission) =>
        IsProductAppPermission(permission) &&
        permission is not "WorkManagement.EmployeeRatings.Management" &&
        permission is not "WorkManagement.EmployeeRatings.Dashboard";

    private static bool IsProductAppPermission(string permission) =>
        permission.StartsWith("WorkManagement.", StringComparison.Ordinal) ||
        permission.StartsWith("Documents.", StringComparison.Ordinal) ||
        permission.StartsWith("Collaboration.", StringComparison.Ordinal);

}

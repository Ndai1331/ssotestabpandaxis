using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace HCS.Data;

/// <summary>
/// Keeps the permissions introduced by the application available to the built-in
/// roles. Admin remains fully granted. <c>bacsi</c> and <c>lanhdao</c> keep the
/// additive operational set. <c>nhanvien</c> is the default employee role and is
/// locked to the workspace / inbox / signing / calendar / social allowlist;
/// extras in those catalogs are revoked on each sync so a local Auth Server
/// restart reapplies the product default.
/// </summary>
public sealed class HCSRolePermissionSynchronizer(
    IIdentityRoleRepository roleRepository,
    IPermissionDataSeeder permissionDataSeeder,
    IPermissionDefinitionManager permissionDefinitionManager,
    IPermissionManager permissionManager) : ITransientDependency
{
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
        var permissions = (await permissionDefinitionManager.GetPermissionsAsync())
            .Where(permission => permission.IsEnabled)
            .Select(permission => permission.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (await RoleExistsAsync("admin"))
        {
            await permissionDataSeeder.SeedAsync(
                RolePermissionValueProvider.ProviderName,
                "admin",
                permissions);
        }

        var operationalPermissions = PermissionsToGrant("lanhdao", permissions);
        foreach (var roleName in new[] { "bacsi", "lanhdao" })
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

        if (!await RoleExistsAsync("nhanvien"))
        {
            return;
        }

        await permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            "nhanvien",
            PermissionsToGrant("nhanvien", permissions));

        foreach (var permission in PermissionsToRevoke("nhanvien", permissions))
        {
            await permissionManager.SetAsync(
                permission,
                RolePermissionValueProvider.ProviderName,
                "nhanvien",
                false);
        }
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

    private async Task<bool> RoleExistsAsync(string roleName) =>
        await roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant()) is not null;
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
    IPermissionDefinitionManager permissionDefinitionManager,
    IPermissionGrantRepository? permissionGrantRepository = null,
    IPermissionManager? permissionManager = null) : ITransientDependency
{
    public const string CustomPermissionsProperty = "HCS.CustomRolePermissions";

    public static readonly string[] EmployeeDefaultPermissions =
    [
        "WorkManagement.Dashboard",
        "WorkManagement.Projects",
        "WorkManagement.ProjectTasks",
        "WorkManagement.Calendar",
        "WorkManagement.EmployeeRatings",
        "Documents.Signing.Execute",
        "Collaboration.Chat",
        "Collaboration.Social",
        "Collaboration.Notifications"
    ];

    public async Task SynchronizeExistingRolesAsync()
    {
        var adminRole = await roleRepository.FindByNormalizedNameAsync("ADMIN");
        if (adminRole is not null)
        {
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

        await NormalizeEmployeeRoleAsync();
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

    private async Task NormalizeEmployeeRoleAsync()
    {
        if (permissionGrantRepository is null && permissionManager is null)
        {
            return;
        }

        var keys = await ResolveEmployeeProviderKeysAsync();
        if (keys.Count == 0)
        {
            return;
        }

        var enabled = (await permissionDefinitionManager.GetPermissionsAsync())
            .Where(permission => permission.IsEnabled)
            .Select(permission => permission.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var revoke = PermissionsToRevoke("nhanvien", enabled).ToHashSet(StringComparer.Ordinal);
        if (revoke.Count == 0)
        {
            return;
        }

        foreach (var key in keys)
        {
            await RevokeEmployeePermissionsAsync(key, revoke);
        }
    }

    private async Task<HashSet<string>> ResolveEmployeeProviderKeysAsync()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal) { "nhanvien" };
        var roles = await roleRepository.GetListAsync();
        foreach (var role in roles)
        {
            if (IsEmployeeRole(role.Name) || IsEmployeeRole(role.NormalizedName))
            {
                keys.Add(role.Name);
            }
        }

        return keys;
    }

    private async Task RevokeEmployeePermissionsAsync(string providerKey, HashSet<string> revoke)
    {
        if (permissionManager is not null)
        {
            foreach (var permission in revoke)
            {
                await permissionManager.SetAsync(
                    permission,
                    RolePermissionValueProvider.ProviderName,
                    providerKey,
                    false);
            }

            return;
        }

        if (permissionGrantRepository is null)
        {
            return;
        }

        var grants = await permissionGrantRepository.GetListAsync(
            RolePermissionValueProvider.ProviderName, providerKey);
        foreach (var grant in grants.Where(item => revoke.Contains(item.Name)))
        {
            await permissionGrantRepository.DeleteAsync(grant);
        }
    }

    public static bool IsEmployeeRole(string? roleName)
    {
        var folded = FoldRoleKey(roleName);
        return folded is "nhanvien";
    }

    public static string FoldRoleKey(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return string.Empty;
        }

        var decomposed = roleName.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
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

        if (IsEmployeeRole(roleName))
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

        if (!IsEmployeeRole(roleName))
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

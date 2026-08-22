using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.Authorization.Permissions;

namespace HCS.Data;

public sealed class HCSRoleDataSeedContributor(
    IIdentityRoleRepository roleRepository,
    IGuidGenerator guidGenerator,
    IPermissionDataSeeder permissionDataSeeder,
    IPermissionDefinitionManager permissionDefinitionManager) : IDataSeedContributor, ITransientDependency
{
    private static readonly string[] RoleNames = ["admin", "bacsi", "lanhdao", "nhanvien"];

    public async Task SeedAsync(DataSeedContext context)
    {
        foreach (var roleName in RoleNames)
        {
            var existingRole = await roleRepository.FindByNormalizedNameAsync(
                roleName.ToUpperInvariant());
            if (existingRole is not null)
            {
                continue;
            }

            await roleRepository.InsertAsync(new IdentityRole(guidGenerator.Create(), roleName));
        }

        var permissions = (await permissionDefinitionManager.GetPermissionsAsync())
            .Where(permission => permission.IsEnabled)
            .Select(permission => permission.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Admin is the local break-glass administrator. Seed every enabled permission
        // registered by the current application, but never revoke existing grants.
        // This keeps policies and grants configured through the Roles UI intact.
        await permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            "admin",
            permissions);

        // Operational roles need the Work/Document/Chat pages after Community port.
        // Grants are additive so later Role UI changes are not revoked.
        var operationalPermissions = permissions
            .Where(permission =>
                permission.StartsWith("WorkManagement.", StringComparison.Ordinal) ||
                permission.StartsWith("Documents.", StringComparison.Ordinal) ||
                permission.StartsWith("Collaboration.", StringComparison.Ordinal))
            .ToArray();

        foreach (var roleName in RoleNames.Where(role => !string.Equals(role, "admin", StringComparison.Ordinal)))
        {
            await permissionDataSeeder.SeedAsync(
                RolePermissionValueProvider.ProviderName,
                roleName,
                operationalPermissions);
        }
    }
}

using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace HCS.Data;

public sealed class HCSRoleDataSeedContributor(
    IIdentityRoleRepository roleRepository,
    IGuidGenerator guidGenerator,
    HCSRolePermissionSynchronizer permissionSynchronizer) : IDataSeedContributor, ITransientDependency
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

        await permissionSynchronizer.SynchronizeExistingRolesAsync();
    }
}

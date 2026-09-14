using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace HCS.Data;

public sealed class HCSRoleDataSeedContributor(HCSRolePermissionSynchronizer permissionSynchronizer)
    : IDataSeedContributor, ITransientDependency
{
    public Task SeedAsync(DataSeedContext context) =>
        permissionSynchronizer.SynchronizeExistingRolesAsync();
}

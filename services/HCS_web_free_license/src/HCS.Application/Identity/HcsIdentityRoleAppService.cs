using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace HCS.Identity;

/// <summary>
/// ABP deletes a role by unassigning its users. HCS keeps the assignment and
/// rejects the delete so an in-use role cannot disappear from user accounts.
/// </summary>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityRoleAppService), typeof(IdentityRoleAppService), typeof(HcsIdentityRoleAppService))]
public class HcsIdentityRoleAppService : IdentityRoleAppService
{
    protected IIdentityUserRepository UserRepository { get; }

    public HcsIdentityRoleAppService(
        IdentityRoleManager roleManager,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository)
        : base(roleManager, roleRepository)
    {
        UserRepository = userRepository;
    }

    public override async Task DeleteAsync(Guid id)
    {
        var assignedCount = await UserRepository.GetCountAsync(roleId: id);
        if (assignedCount > 0)
        {
            throw new BusinessException(HCSDomainErrorCodes.RoleAssignedToUsers)
                .WithData("Count", assignedCount);
        }

        await base.DeleteAsync(id);
    }
}

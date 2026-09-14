using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace HCS;

public abstract class IdentityUserRoleAssignmentTests<TStartupModule> : HCSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IIdentityUserAppService _userAppService;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IGuidGenerator _guidGenerator;

    protected IdentityUserRoleAssignmentTests()
    {
        _userAppService = GetRequiredService<IIdentityUserAppService>();
        _roleRepository = GetRequiredService<IIdentityRoleRepository>();
        _guidGenerator = GetRequiredService<IGuidGenerator>();
    }

    [Fact]
    public async Task UpdateRoles_Persists_Operational_Role_Without_Admin_Claim()
    {
        var roleName = "op" + Guid.NewGuid().ToString("N")[..8];
        await _roleRepository.InsertAsync(new IdentityRole(_guidGenerator.Create(), roleName), autoSave: true);

        var created = await _userAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = "u" + Guid.NewGuid().ToString("N")[..16],
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "Test-password-42!",
            RoleNames = []
        });

        await _userAppService.UpdateRolesAsync(created.Id, new IdentityUserUpdateRolesDto
        {
            RoleNames = [roleName]
        });

        var roles = await _userAppService.GetRolesAsync(created.Id);
        roles.Items.ShouldContain(role => role.Name == roleName);
    }
}

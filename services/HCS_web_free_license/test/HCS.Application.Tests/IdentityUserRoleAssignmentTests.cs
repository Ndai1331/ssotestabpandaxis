using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace HCS;

public abstract class IdentityUserRoleAssignmentTests<TStartupModule> : HCSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IIdentityUserAppService _userAppService;
    private readonly IIdentityRoleAppService _roleAppService;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IGuidGenerator _guidGenerator;

    protected IdentityUserRoleAssignmentTests()
    {
        _userAppService = GetRequiredService<IIdentityUserAppService>();
        _roleAppService = GetRequiredService<IIdentityRoleAppService>();
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

    [Fact]
    public async Task Delete_Role_Assigned_To_User_Is_Rejected()
    {
        var roleName = "inuse" + Guid.NewGuid().ToString("N")[..8];
        var role = await _roleRepository.InsertAsync(new IdentityRole(_guidGenerator.Create(), roleName), autoSave: true);

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

        var exception = await Should.ThrowAsync<BusinessException>(() => _roleAppService.DeleteAsync(role.Id));
        exception.Code.ShouldBe(HCSDomainErrorCodes.RoleAssignedToUsers);
        (await _roleRepository.FindAsync(role.Id)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Delete_Unused_Role_Succeeds()
    {
        var roleName = "empty" + Guid.NewGuid().ToString("N")[..8];
        var role = await _roleRepository.InsertAsync(new IdentityRole(_guidGenerator.Create(), roleName), autoSave: true);

        await _roleAppService.DeleteAsync(role.Id);

        (await _roleRepository.FindAsync(role.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Create_And_Update_Persist_Phone_Number()
    {
        var created = await _userAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = "u" + Guid.NewGuid().ToString("N")[..16],
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "Test-password-42!",
            PhoneNumber = " 0901234567 ",
            RoleNames = []
        });

        created.PhoneNumber.ShouldBe("0901234567");

        var updated = await _userAppService.UpdateAsync(created.Id, new IdentityUserUpdateDto
        {
            UserName = created.UserName,
            Email = created.Email,
            Name = created.Name,
            Surname = created.Surname,
            PhoneNumber = "0912345678",
            IsActive = created.IsActive,
            LockoutEnabled = created.LockoutEnabled,
            ConcurrencyStamp = created.ConcurrencyStamp
        });

        updated.PhoneNumber.ShouldBe("0912345678");
        (await _userAppService.GetAsync(created.Id)).PhoneNumber.ShouldBe("0912345678");

        var cleared = await _userAppService.UpdateAsync(created.Id, new IdentityUserUpdateDto
        {
            UserName = created.UserName,
            Email = created.Email,
            PhoneNumber = " ",
            IsActive = created.IsActive,
            LockoutEnabled = created.LockoutEnabled,
            ConcurrencyStamp = updated.ConcurrencyStamp
        });

        cleared.PhoneNumber.ShouldBeNullOrWhiteSpace();
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using HCS.OrganizationUnits;
using Shouldly;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;
using Ou = Volo.Abp.Identity.OrganizationUnit;

namespace HCS;

public abstract class OrganizationUnitLookupAppServiceTests<TStartupModule> : HCSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IOrganizationUnitLookupAppService lookup;
    private readonly OrganizationUnitManager organizationUnitManager;
    private readonly IIdentityUserAppService users;
    private readonly IGuidGenerator guidGenerator;

    protected OrganizationUnitLookupAppServiceTests()
    {
        lookup = GetRequiredService<IOrganizationUnitLookupAppService>();
        organizationUnitManager = GetRequiredService<OrganizationUnitManager>();
        users = GetRequiredService<IIdentityUserAppService>();
        guidGenerator = GetRequiredService<IGuidGenerator>();
    }

    [Fact]
    public async Task GetList_Returns_Created_Organization_Unit()
    {
        var displayName = "OU " + Guid.NewGuid().ToString("N")[..8];
        var unit = new Ou(guidGenerator.Create(), displayName);
        await organizationUnitManager.CreateAsync(unit);

        var list = await lookup.GetListAsync();

        list.ShouldContain(item => item.Id == unit.Id && item.DisplayName == displayName);
    }

    [Fact]
    public async Task SetUser_And_GetUsers_Return_Primary_Membership()
    {
        var unit = new Ou(guidGenerator.Create(), "Dept " + Guid.NewGuid().ToString("N")[..8]);
        await organizationUnitManager.CreateAsync(unit);
        var created = await users.CreateAsync(new IdentityUserCreateDto
        {
            UserName = "u" + Guid.NewGuid().ToString("N")[..16],
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "Test-password-42!",
            IsActive = true,
            RoleNames = []
        });

        await lookup.SetUserAsync(created.Id, new SetUserOrganizationUnitsInput
        {
            OrganizationUnitIds = [unit.Id]
        });

        var memberships = await lookup.GetUsersAsync([created.Id]);
        var membership = memberships.ShouldHaveSingleItem();
        membership.UserId.ShouldBe(created.Id);
        membership.OrganizationUnitId.ShouldBe(unit.Id);
        membership.DisplayName.ShouldBe(unit.DisplayName);

        var members = await lookup.GetMembersAsync(unit.Id);
        members.ShouldContain(item => item.UserId == created.Id);
    }
}

using System;
using System.Threading.Tasks;
using HCS.OrganizationUnits;
using Shouldly;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace HCS;

public abstract class OrganizationUnitMemberSearchTests<TStartupModule> : HCSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IOrganizationUnitManagementAppService _organizationUnits;
    private readonly IIdentityUserAppService _userAppService;

    protected OrganizationUnitMemberSearchTests()
    {
        _organizationUnits = GetRequiredService<IOrganizationUnitManagementAppService>();
        _userAppService = GetRequiredService<IIdentityUserAppService>();
    }

    [Theory]
    [InlineData("Huỳnh Thị Quỳnh Như")]
    [InlineData("huỳnh")]
    [InlineData("Như")]
    [InlineData("quỳnh")]
    public async Task Members_filter_matches_vietnamese_full_name_or_parts(string filter)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var unit = await _organizationUnits.CreateAsync(new CreateOrganizationUnitInput
        {
            DisplayName = "Dept " + suffix
        });
        var created = await _userAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = "nv" + suffix,
            Email = $"{suffix}@example.com",
            Password = "Test-password-42!",
            Surname = "Huỳnh",
            Name = "Thị Quỳnh Như",
            RoleNames = []
        });
        await _organizationUnits.AddMemberAsync(unit.Id, new AddOrganizationUnitMemberInput
        {
            UserId = created.Id
        });

        var result = await _organizationUnits.GetMembersAsync(unit.Id, new GetOrganizationUnitMembersInput
        {
            Filter = filter,
            MaxResultCount = 50
        });

        result.Items.ShouldContain(member => member.Id == created.Id);
    }
}

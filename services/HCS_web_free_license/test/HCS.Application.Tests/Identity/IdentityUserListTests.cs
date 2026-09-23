using System;
using System.Linq;
using System.Threading.Tasks;
using HCS.Identity;
using Shouldly;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace HCS;

public abstract class IdentityUserListTests<TStartupModule> : HCSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IIdentityUserAppService _userAppService;

    protected IdentityUserListTests()
    {
        _userAppService = GetRequiredService<IIdentityUserAppService>();
    }

    [Fact]
    public async Task GetList_Skip_Returns_The_Next_Page()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await CreateUserAsync($"za-{marker}", $"za-{marker}@example.com", "0911000001");
        await CreateUserAsync($"zb-{marker}", $"zb-{marker}@example.com", "0911000002");
        await CreateUserAsync($"zc-{marker}", $"zc-{marker}@example.com", "0911000003");

        var page1 = await _userAppService.GetListAsync(new GetIdentityUsersInput
        {
            MaxResultCount = 1,
            SkipCount = 0
        });
        var page2 = await _userAppService.GetListAsync(new GetIdentityUsersInput
        {
            MaxResultCount = 1,
            SkipCount = 1
        });

        page1.TotalCount.ShouldBeGreaterThanOrEqualTo(3);
        page1.Items.ShouldHaveSingleItem();
        page2.Items.ShouldHaveSingleItem();
        page1.Items[0].Id.ShouldNotBe(page2.Items[0].Id);
    }

    [Fact]
    public async Task GetList_Filter_Matches_UserName_Email_And_Phone()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var created = await CreateUserAsync($"Case{marker}", $"Mail{marker}@Example.COM", "0987654321", "Nguyen", "Van A");

        var byUserName = await _userAppService.GetListAsync(new GetIdentityUsersInput
        {
            Filter = marker.ToUpperInvariant(),
            MaxResultCount = 50
        });
        byUserName.Items.ShouldContain(user => user.Id == created.Id);

        var byEmail = await _userAppService.GetListAsync(new GetIdentityUsersInput
        {
            Filter = $"mail{marker}@example.com",
            MaxResultCount = 50
        });
        byEmail.Items.ShouldContain(user => user.Id == created.Id);

        var byPhone = await _userAppService.GetListAsync(new GetIdentityUsersInput
        {
            Filter = "0987 654",
            MaxResultCount = 50
        });
        byPhone.Items.ShouldContain(user => user.Id == created.Id);

        var byName = await _userAppService.GetListAsync(new GetIdentityUsersInput
        {
            Filter = "nguyen",
            MaxResultCount = 50
        });
        byName.Items.ShouldContain(user => user.Id == created.Id);
    }

    [Fact]
    public async Task GetList_NotActive_Returns_Only_Inactive_Users()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var inactive = await CreateUserAsync($"off-{marker}", $"off-{marker}@example.com", "0911000099", isActive: false);
        var active = await CreateUserAsync($"on-{marker}", $"on-{marker}@example.com", "0911000098");

        HcsIdentityUserListQuery.NotActive = true;
        try
        {
            var result = await _userAppService.GetListAsync(new GetIdentityUsersInput
            {
                Filter = marker,
                MaxResultCount = 50
            });
            result.Items.ShouldContain(user => user.Id == inactive.Id);
            result.Items.ShouldNotContain(user => user.Id == active.Id);
            result.Items.ShouldAllBe(user => !user.IsActive);
        }
        finally
        {
            HcsIdentityUserListQuery.NotActive = null;
        }
    }

    private async Task<IdentityUserDto> CreateUserAsync(
        string userName,
        string email,
        string phone,
        string? surname = null,
        string? name = null,
        bool isActive = true)
    {
        return await _userAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = userName,
            Email = email,
            Password = "Test-password-42!",
            PhoneNumber = phone,
            Surname = surname,
            Name = name,
            IsActive = isActive,
            RoleNames = []
        });
    }
}

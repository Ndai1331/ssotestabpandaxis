using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace HCS;

public abstract class IdentityUserSearchTests<TStartupModule> : HCSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IIdentityUserAppService _userAppService;

    protected IdentityUserSearchTests()
    {
        _userAppService = GetRequiredService<IIdentityUserAppService>();
    }

    [Theory]
    [InlineData("Huỳnh Thị Quỳnh Như")]
    [InlineData("huỳnh")]
    [InlineData("Như")]
    [InlineData("quỳnh")]
    public async Task List_filter_matches_vietnamese_full_name_or_parts(string filter)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var created = await _userAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = "nv" + suffix,
            Email = $"{suffix}@example.com",
            Password = "Test-password-42!",
            Surname = "Huỳnh",
            Name = "Thị Quỳnh Như",
            RoleNames = []
        });

        var result = await _userAppService.GetListAsync(new GetIdentityUsersInput
        {
            Filter = filter,
            MaxResultCount = 100
        });

        result.Items.ShouldContain(user => user.Id == created.Id);
    }
}

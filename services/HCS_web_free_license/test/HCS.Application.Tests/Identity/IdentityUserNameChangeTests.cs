using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace HCS;

public abstract class IdentityUserNameChangeTests<TStartupModule> : HCSApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IIdentityUserAppService _userAppService;
    private readonly IProfileAppService _profileAppService;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    protected IdentityUserNameChangeTests()
    {
        _userAppService = GetRequiredService<IIdentityUserAppService>();
        _profileAppService = GetRequiredService<IProfileAppService>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Admin_Update_Changes_UserName()
    {
        var created = await CreateUserAsync();
        var nextUserName = "renamed" + Guid.NewGuid().ToString("N")[..10];

        var updated = await _userAppService.UpdateAsync(created.Id, UpdateFrom(created, nextUserName));

        updated.UserName.ShouldBe(nextUserName);
        (await _userAppService.GetAsync(created.Id)).UserName.ShouldBe(nextUserName);
    }

    [Fact]
    public async Task Admin_Update_Rejects_Duplicate_UserName()
    {
        var first = await CreateUserAsync();
        var second = await CreateUserAsync();

        var exception = await Should.ThrowAsync<AbpIdentityResultException>(() =>
            _userAppService.UpdateAsync(second.Id, UpdateFrom(second, first.UserName)));

        exception.IdentityResult.Errors.ShouldContain(error =>
            string.Equals(error.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Profile_Update_Changes_Current_User_UserName()
    {
        var created = await CreateUserAsync();
        var nextUserName = "self" + Guid.NewGuid().ToString("N")[..12];

        using (_principalAccessor.Change(PrincipalFor(created)))
        {
            var profile = await _profileAppService.GetAsync();
            var updated = await _profileAppService.UpdateAsync(new UpdateProfileDto
            {
                UserName = "  " + nextUserName + "  ",
                Email = profile.Email,
                Name = profile.Name,
                Surname = profile.Surname,
                PhoneNumber = profile.PhoneNumber,
                ConcurrencyStamp = profile.ConcurrencyStamp
            });

            updated.UserName.ShouldBe(nextUserName);
        }

        (await _userAppService.GetAsync(created.Id)).UserName.ShouldBe(nextUserName);
    }

    [Fact]
    public async Task Profile_Update_Rejects_Duplicate_UserName()
    {
        var first = await CreateUserAsync();
        var second = await CreateUserAsync();

        using (_principalAccessor.Change(PrincipalFor(second)))
        {
            var profile = await _profileAppService.GetAsync();
            var exception = await Should.ThrowAsync<BusinessException>(() =>
                _profileAppService.UpdateAsync(new UpdateProfileDto
                {
                    UserName = first.UserName,
                    Email = profile.Email,
                    ConcurrencyStamp = profile.ConcurrencyStamp
                }));

            exception.Code.ShouldBe(HCSDomainErrorCodes.AccountUserNameTaken);
        }
    }

    [Fact]
    public async Task Profile_Update_Rejects_UserName_With_Whitespace()
    {
        var created = await CreateUserAsync();

        using (_principalAccessor.Change(PrincipalFor(created)))
        {
            var profile = await _profileAppService.GetAsync();
            var exception = await Should.ThrowAsync<BusinessException>(() =>
                _profileAppService.UpdateAsync(new UpdateProfileDto
                {
                    UserName = "bad name",
                    Email = profile.Email,
                    ConcurrencyStamp = profile.ConcurrencyStamp
                }));

            exception.Code.ShouldBe(HCSDomainErrorCodes.AccountUserNameInvalid);
        }
    }

    private async Task<IdentityUserDto> CreateUserAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return await _userAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = "u" + suffix,
            Email = $"{suffix}@example.com",
            Password = "Test-password-42!",
            RoleNames = []
        });
    }

    private static IdentityUserUpdateDto UpdateFrom(IdentityUserDto user, string userName) => new()
    {
        UserName = userName,
        Email = user.Email,
        Name = user.Name,
        Surname = user.Surname,
        PhoneNumber = user.PhoneNumber,
        IsActive = user.IsActive,
        LockoutEnabled = user.LockoutEnabled,
        ConcurrencyStamp = user.ConcurrencyStamp
    };

    private static ClaimsPrincipal PrincipalFor(IdentityUserDto user) =>
        new(new ClaimsIdentity(
        [
            new Claim(AbpClaimTypes.UserId, user.Id.ToString()),
            new Claim(AbpClaimTypes.UserName, user.UserName),
            new Claim(AbpClaimTypes.Email, user.Email)
        ], "test"));
}

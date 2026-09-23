using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace HCS.Identity;

/// <summary>
/// ABP Identity only assigns arbitrary roles when <c>CurrentUser.IsInRole("admin")</c>.
/// HCS access tokens carry permission claims, not always <c>role</c>, so that check
/// silently dropped selected roles while still returning HTTP 200.
/// Phone numbers are normalized so administration can create, update, and clear them.
/// User list search/paging is overridden because OSS GetList uses case-sensitive
/// Contains and Dynamic LINQ OrderBy, which breaks PostgreSQL skip and phone/name search.
/// </summary>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityUserAppService), typeof(IdentityUserAppService), typeof(HcsIdentityUserAppService))]
public class HcsIdentityUserAppService : IdentityUserAppService
{
    private const int MaxPageSize = 100;

    public HcsIdentityUserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IRepository<IdentityUser, Guid> userQuery,
        IIdentityRoleRepository roleRepository,
        IOptions<IdentityOptions> identityOptions,
        IPermissionChecker permissionChecker)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
        UserQuery = userQuery;
    }

    protected IRepository<IdentityUser, Guid> UserQuery { get; }

    [Authorize(IdentityPermissions.Users.Default)]
    public override async Task<PagedResultDto<IdentityUserDto>> GetListAsync(GetIdentityUsersInput input)
    {
        var query = ApplyListFilter(await UserQuery.GetQueryableAsync(), input.Filter, HcsIdentityUserListQuery.NotActive);
        var totalCount = await AsyncExecuter.CountAsync(query);
        var pageSize = input.MaxResultCount <= 0 ? 10 : Math.Min(input.MaxResultCount, MaxPageSize);
        var skip = Math.Max(0, input.SkipCount);
        var users = await AsyncExecuter.ToListAsync(
            query
                .OrderByDescending(user => user.CreationTime)
                .ThenBy(user => user.UserName)
                .Skip(skip)
                .Take(pageSize));

        return new PagedResultDto<IdentityUserDto>(
            totalCount,
            ObjectMapper.Map<List<IdentityUser>, List<IdentityUserDto>>(users));
    }

    internal static IQueryable<IdentityUser> ApplyListFilter(
        IQueryable<IdentityUser> query,
        string? filter,
        bool? notActive)
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var term = filter.Trim().ToLower();
            var phoneTerm = new string(term.Where(char.IsDigit).ToArray());
            var matchPhoneDigits = phoneTerm.Length >= 3;
            query = query.Where(user =>
                user.UserName.ToLower().Contains(term) ||
                user.Email.ToLower().Contains(term) ||
                (user.Name != null && user.Name.ToLower().Contains(term)) ||
                (user.Surname != null && user.Surname.ToLower().Contains(term)) ||
                ((user.Surname ?? string.Empty) + " " + (user.Name ?? string.Empty)).ToLower().Contains(term) ||
                (user.PhoneNumber != null && (
                    user.PhoneNumber.ToLower().Contains(term) ||
                    (matchPhoneDigits && user.PhoneNumber.Contains(phoneTerm)))));
        }

        if (notActive == true)
        {
            query = query.Where(user => !user.IsActive);
        }
        else if (notActive == false)
        {
            query = query.Where(user => user.IsActive);
        }

        return query;
    }

    protected override async Task<bool> HasAdminRoleAsync()
    {
        if (CurrentUser.IsInRole("admin")
            || CurrentUser.Roles.Any(role => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return await PermissionChecker.IsGrantedAsync(IdentityPermissions.Users.ManageRoles);
    }

    protected override async Task UpdateUserByInput(IdentityUser user, IdentityUserCreateOrUpdateDtoBase input)
    {
        input.PhoneNumber = IdentityPhoneNumbers.Normalize(input.PhoneNumber);
        await base.UpdateUserByInput(user, input);

        if (!string.Equals(user.PhoneNumber, input.PhoneNumber, StringComparison.Ordinal))
        {
            (await UserManager.SetPhoneNumberAsync(user, input.PhoneNumber)).CheckErrors();
        }
    }
}

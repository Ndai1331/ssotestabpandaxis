using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCS.OrganizationUnits;
using Volo.Abp.Identity;
using Ou = Volo.Abp.Identity.OrganizationUnit;

namespace HCS.OrganizationUnits;

public partial class OrganizationUnitManagementAppService
{
    private static OrganizationUnitDto Map(Ou unit) => new()
    {
        Id = unit.Id,
        ParentId = unit.ParentId,
        Code = unit.Code,
        DisplayName = unit.DisplayName,
        ConcurrencyStamp = unit.ConcurrencyStamp ?? string.Empty
    };

    private static OrganizationUnitMemberDto Map(IdentityUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        FullName = BuildFullName(user),
        Email = user.Email ?? string.Empty,
        PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber.Trim(),
        IsActive = user.IsActive
    };

    private async Task<List<OrganizationUnitMemberDto>> MapMembersAsync(IReadOnlyList<IdentityUser> users)
    {
        var mapped = users.Select(Map).ToList();
        if (mapped.Count == 0)
            return mapped;

        var userIds = mapped.Select(x => x.Id).ToArray();
        var rolesByUser = (await identityUserRepository.GetRoleNamesAsync(userIds))
            .ToDictionary(item => item.Id, item => item.RoleNames);
        var departments = await userOrganizationNames.GetDisplayNamesAsync(userIds);
        foreach (var member in mapped)
        {
            member.RoleNames = rolesByUser.TryGetValue(member.Id, out var roleNames)
                ? roleNames?.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    ?? []
                : [];
            if (departments.TryGetValue(member.Id, out var department) && !string.IsNullOrWhiteSpace(department))
                member.DepartmentName = department;
        }

        return mapped;
    }

    private static string BuildFullName(IdentityUser user)
    {
        var fullName = string.Join(
            ' ',
            new[] { user.Surname, user.Name }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
    }
}

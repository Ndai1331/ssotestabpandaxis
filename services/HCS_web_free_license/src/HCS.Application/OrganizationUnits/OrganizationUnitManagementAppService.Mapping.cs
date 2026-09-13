using System.Linq;
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
        IsActive = user.IsActive
    };

    private static string BuildFullName(IdentityUser user)
    {
        var fullName = string.Join(
            ' ',
            new[] { user.Name, user.Surname }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
    }
}

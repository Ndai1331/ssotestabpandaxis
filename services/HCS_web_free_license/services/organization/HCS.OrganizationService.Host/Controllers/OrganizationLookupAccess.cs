using System.Security.Claims;
using HCS.OrganizationService.Contracts;

namespace HCS.OrganizationService.Host.Controllers;

public static class OrganizationLookupAccess
{
    private static readonly string[] LookupRoles = ["admin", "lanhdao", "bacsi", "nhanvien"];

    private static readonly string[] LookupPermissions =
    [
        OrganizationPermissions.Departments,
        OrganizationPermissions.Units,
        OrganizationPermissions.Positions,
        OrganizationPermissions.UserMappings,
        "Documents.View",
        "Documents.Create",
        "Documents.Update",
        "Documents.Assign",
        "Documents.Signing.Execute",
        "Documents.Workflow.Start",
        "Documents.Workflow.View",
        "Collaboration.Social",
        "WorkManagement.EmployeeRatings",
        "WorkManagement.EmployeeRatings.Management",
        "WorkManagement.EmployeeRatings.Dashboard",
        ..OrganizationPermissions.MasterDataAccess
    ];

    public static bool CanRead(ClaimsPrincipal user) =>
        LookupRoles.Any(user.IsInRole)
        || LookupPermissions.Any(permission => user.HasClaim("permission", permission));
}

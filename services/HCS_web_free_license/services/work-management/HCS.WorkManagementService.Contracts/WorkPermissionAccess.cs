using System.Security.Claims;

namespace HCS.WorkManagementService.Contracts;

public static class WorkPermissionAccess
{
    public static bool IsAdministrator(ClaimsPrincipal? user) =>
        user is not null &&
        (user.IsInRole("admin") ||
         user.FindAll("role").Any(claim => string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase)) ||
         user.FindAll(ClaimTypes.Role).Any(claim => string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase)));

    public static bool HasPermission(ClaimsPrincipal? user, string permission) =>
        user is not null &&
        (IsAdministrator(user) || user.HasClaim("permission", permission));

    public static bool CanReadWorkspaceResource(ClaimsPrincipal? user, string permission) =>
        HasPermission(user, permission) || HasPermission(user, WorkPermissions.Dashboard);
}

using System;
using System.Linq;
using System.Security.Claims;

namespace HCS.Permissions;

public static class HcsAdministrator
{
    public const string RoleName = "admin";

    public static bool Is(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return false;
        }

        if (user.IsInRole(RoleName))
        {
            return true;
        }

        return user.Claims.Any(claim =>
            IsRoleClaim(claim.Type) &&
            string.Equals(claim.Value, RoleName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRoleClaim(string type) =>
        string.Equals(type, "role", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.Ordinal);
}

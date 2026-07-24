namespace hanhchinhso.AuthServer;

/// <summary>
/// Maps Keycloak group claims to ABP Identity role names (BD SSO lab).
/// App gate: <c>bd-app-hcs</c>. Role priority: admin &gt; lanhdao &gt; bacsi &gt; nhanvien.
/// </summary>
public static class KeycloakGroupRoleMapper
{
    public const string GroupsClaim = "groups";
    public const string AppAccessGroup = "bd-app-hcs";

    private static readonly (string Group, string Role)[] Map =
    [
        ("bd-admin", "admin"),
        ("bd-lanhdao", "lanhdao"),
        ("bd-bacsi", "bacsi"),
        ("bd-nhanvien", "nhanvien"),
    ];

    public static bool HasAppAccess(IEnumerable<string> groups)
    {
        var set = new HashSet<string>(groups, StringComparer.OrdinalIgnoreCase);
        return set.Contains(AppAccessGroup);
    }

    public static IReadOnlyList<string> ResolveRoles(IEnumerable<string> groups)
    {
        var set = new HashSet<string>(groups, StringComparer.OrdinalIgnoreCase);
        if (!set.Contains(AppAccessGroup))
        {
            return [];
        }

        var roles = new List<string>();
        foreach (var (group, role) in Map)
        {
            if (set.Contains(group))
            {
                roles.Add(role);
            }
        }

        // Default role only after app entitlement is confirmed
        if (roles.Count == 0)
        {
            roles.Add("nhanvien");
        }

        return roles;
    }

    /// <summary>
    /// Highest-priority single role for apps that need one primary role.
    /// </summary>
    public static string ResolvePrimaryRole(IEnumerable<string> groups)
    {
        var roles = ResolveRoles(groups);
        if (roles.Count == 0)
        {
            return string.Empty;
        }

        foreach (var (_, role) in Map)
        {
            if (roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                return role;
            }
        }

        return "nhanvien";
    }
}

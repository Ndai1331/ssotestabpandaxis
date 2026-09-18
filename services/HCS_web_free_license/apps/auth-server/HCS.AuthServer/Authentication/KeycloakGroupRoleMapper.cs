using HCS.Settings;

namespace HCS.AuthServer;

public static class KeycloakGroupRoleMapper
{
    public const string GroupsClaim = "groups";
    public const string AppAccessGroup = KeycloakSettingDefaults.AppAccessGroup;

    public static bool HasAppAccess(IEnumerable<string> groups) =>
        HasAppAccess(groups, AppAccessGroup);

    public static bool HasAppAccess(IEnumerable<string> groups, string? appAccessGroup)
    {
        var required = NormalizeGroup(appAccessGroup) ?? AppAccessGroup;
        return Normalize(groups).Contains(required);
    }

    public static IReadOnlyList<string> ResolveRoles(IEnumerable<string> groups) =>
        ResolveRoles(groups, AppAccessGroup, KeycloakSettingDefaults.RoleMappings);

    public static IReadOnlyList<string> ResolveRoles(
        IEnumerable<string> groups,
        string? appAccessGroup,
        IEnumerable<KeycloakRoleMapping>? mappings)
    {
        var normalizedGroups = Normalize(groups);
        var required = NormalizeGroup(appAccessGroup) ?? AppAccessGroup;
        if (!normalizedGroups.Contains(required))
        {
            return [];
        }

        var roles = KeycloakRoleMappingSerializer.Normalize(mappings)
            .Where(mapping => normalizedGroups.Contains(mapping.Group))
            .Select(mapping => mapping.Role)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return roles.Count == 0 ? [KeycloakSettingDefaults.DefaultRole] : roles;
    }

    private static string? NormalizeGroup(string? group) =>
        string.IsNullOrWhiteSpace(group) ? null : group.Trim().TrimStart('/');

    private static HashSet<string> Normalize(IEnumerable<string> groups) =>
        groups
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => group.Trim().TrimStart('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

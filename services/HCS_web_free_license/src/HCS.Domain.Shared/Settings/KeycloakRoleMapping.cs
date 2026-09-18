using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HCS.Settings;

public sealed class KeycloakRoleMapping
{
    public string Group { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public static class KeycloakSettingDefaults
{
    public const string Realm = "bd";
    public const string ClientId = "hcs-free-auth";
    public const string AppAccessGroup = "bd-app-hcs";
    public const string DefaultRole = "nhanvien";

    public static readonly KeycloakRoleMapping[] RoleMappings =
    [
        new() { Group = "bd-admin", Role = "admin" },
        new() { Group = "bd-lanhdao", Role = "lanhdao" },
        new() { Group = "bd-bacsi", Role = "bacsi" },
        new() { Group = "bd-nhanvien", Role = "nhanvien" }
    ];
}

public static class KeycloakAuthority
{
    public static string Combine(string? baseUrl, string? realm)
    {
        var root = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        var realmName = string.IsNullOrWhiteSpace(realm) ? KeycloakSettingDefaults.Realm : realm.Trim();
        return string.IsNullOrWhiteSpace(root) ? string.Empty : $"{root}/realms/{realmName}";
    }

    public static bool TrySplit(string? authority, out string baseUrl, out string realm)
    {
        baseUrl = string.Empty;
        realm = KeycloakSettingDefaults.Realm;
        if (string.IsNullOrWhiteSpace(authority))
        {
            return false;
        }

        var value = authority.Trim().TrimEnd('/');
        const string marker = "/realms/";
        var index = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index <= 0)
        {
            baseUrl = value;
            return true;
        }

        baseUrl = value[..index];
        realm = value[(index + marker.Length)..];
        if (string.IsNullOrWhiteSpace(realm))
        {
            realm = KeycloakSettingDefaults.Realm;
        }

        return !string.IsNullOrWhiteSpace(baseUrl);
    }

    public static string DiscoveryUrl(string authority) =>
        $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
}

public static class KeycloakRoleMappingSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(IEnumerable<KeycloakRoleMapping>? mappings)
    {
        var normalized = Normalize(mappings);
        return JsonSerializer.Serialize(normalized, JsonOptions);
    }

    public static IReadOnlyList<KeycloakRoleMapping> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return KeycloakSettingDefaults.RoleMappings;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<KeycloakRoleMapping>>(json, JsonOptions);
            var normalized = Normalize(parsed);
            return normalized.Count == 0 ? KeycloakSettingDefaults.RoleMappings : normalized;
        }
        catch (JsonException)
        {
            return KeycloakSettingDefaults.RoleMappings;
        }
    }

    public static IReadOnlyList<KeycloakRoleMapping> Normalize(IEnumerable<KeycloakRoleMapping>? mappings)
    {
        return (mappings ?? [])
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Group) && !string.IsNullOrWhiteSpace(mapping.Role))
            .Select(mapping => new KeycloakRoleMapping
            {
                Group = mapping.Group.Trim().TrimStart('/'),
                Role = mapping.Role.Trim()
            })
            .GroupBy(mapping => mapping.Group, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }
}

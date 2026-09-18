using System;
using System.Collections.Generic;

namespace HCS.Settings;

public sealed class KeycloakSettingValues
{
    public string? Enabled { get; init; }
    public string? ShowSsoLoginButton { get; init; }
    public string? BaseUrl { get; init; }
    public string? Realm { get; init; }
    public string? Authority { get; init; }
    public string? MetadataAddress { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? RequireHttpsMetadata { get; init; }
    public string? AdminUser { get; init; }
    public string? AdminSecret { get; init; }
    public string? AppAccessGroup { get; init; }
    public string? RoleMappings { get; init; }
    public string? Revision { get; init; }
}

public sealed class KeycloakResolvedSettings
{
    public bool Enabled { get; init; }
    public bool ShowSsoLoginButton { get; init; } = true;
    public string BaseUrl { get; init; } = string.Empty;
    public string Realm { get; init; } = KeycloakSettingDefaults.Realm;
    public string Authority { get; init; } = string.Empty;
    public string? MetadataAddress { get; init; }
    public string ClientId { get; init; } = KeycloakSettingDefaults.ClientId;
    public string ClientSecret { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;
    public string AdminUser { get; init; } = string.Empty;
    public string AdminSecret { get; init; } = string.Empty;
    public string AppAccessGroup { get; init; } = KeycloakSettingDefaults.AppAccessGroup;
    public IReadOnlyList<KeycloakRoleMapping> RoleMappings { get; init; } = KeycloakSettingDefaults.RoleMappings;
    public bool HasClientSecret { get; init; }
    public bool HasAdminSecret { get; init; }
    public string Revision { get; init; } = "0";
}

public static class KeycloakSettingsResolution
{
    public static bool ShouldReplaceSecret(string? incoming) => !string.IsNullOrWhiteSpace(incoming);

    public static string NextRevision(string? current) =>
        long.TryParse(current, out var value) && value >= 0
            ? (value + 1).ToString()
            : "1";

    public static KeycloakResolvedSettings Resolve(
        KeycloakSettingValues stored,
        KeycloakSettingValues fallback)
    {
        stored ??= new KeycloakSettingValues();
        fallback ??= new KeycloakSettingValues();

        var fallbackBaseUrl = fallback.BaseUrl;
        var fallbackRealm = fallback.Realm;
        if (string.IsNullOrWhiteSpace(fallbackBaseUrl)
            && KeycloakAuthority.TrySplit(fallback.Authority, out var splitBaseUrl, out var splitRealm))
        {
            fallbackBaseUrl = splitBaseUrl;
            fallbackRealm = splitRealm;
        }

        var baseUrl = First(stored.BaseUrl, fallbackBaseUrl);
        var realm = First(stored.Realm, fallbackRealm, KeycloakSettingDefaults.Realm);
        var clientSecret = First(stored.ClientSecret, fallback.ClientSecret);
        var adminSecret = First(stored.AdminSecret, fallback.AdminSecret);
        var enabledFallback = ParseBool(fallback.Enabled, defaultValue: true);

        return new KeycloakResolvedSettings
        {
            Enabled = ParseBool(stored.Enabled, enabledFallback),
            ShowSsoLoginButton = ParseBool(stored.ShowSsoLoginButton, defaultValue: true),
            BaseUrl = baseUrl,
            Realm = realm,
            Authority = KeycloakAuthority.Combine(baseUrl, realm),
            MetadataAddress = First(fallback.MetadataAddress),
            ClientId = First(stored.ClientId, fallback.ClientId, KeycloakSettingDefaults.ClientId),
            ClientSecret = clientSecret,
            RequireHttpsMetadata = ParseBool(
                stored.RequireHttpsMetadata,
                ParseBool(fallback.RequireHttpsMetadata, defaultValue: true)),
            AdminUser = First(stored.AdminUser, fallback.AdminUser),
            AdminSecret = adminSecret,
            AppAccessGroup = First(stored.AppAccessGroup, fallback.AppAccessGroup, KeycloakSettingDefaults.AppAccessGroup)
                .TrimStart('/'),
            RoleMappings = KeycloakRoleMappingSerializer.Deserialize(
                First(stored.RoleMappings, fallback.RoleMappings, KeycloakRoleMappingSerializer.Serialize(null))),
            HasClientSecret = !string.IsNullOrWhiteSpace(clientSecret),
            HasAdminSecret = !string.IsNullOrWhiteSpace(adminSecret),
            Revision = First(stored.Revision, "0")
        };
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static string First(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}

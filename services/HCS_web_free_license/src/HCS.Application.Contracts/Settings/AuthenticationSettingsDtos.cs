using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCS.Settings;

public sealed class AuthenticationSettingsDto
{
    public bool ShowSsoLoginButton { get; set; } = true;
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = KeycloakSettingDefaults.Realm;
    public string ClientId { get; set; } = KeycloakSettingDefaults.ClientId;
    public bool HasClientSecret { get; set; }
    public bool RequireHttpsMetadata { get; set; } = true;
    public string AdminUser { get; set; } = string.Empty;
    public bool HasAdminSecret { get; set; }
    public string AppAccessGroup { get; set; } = KeycloakSettingDefaults.AppAccessGroup;
    public List<KeycloakRoleMapping> RoleMappings { get; set; } = [.. KeycloakSettingDefaults.RoleMappings];
}

public sealed class UpdateAuthenticationSettingsDto
{
    public bool ShowSsoLoginButton { get; set; } = true;
    public bool Enabled { get; set; }

    [StringLength(256)]
    public string? BaseUrl { get; set; }

    [StringLength(64)]
    public string? Realm { get; set; }

    [StringLength(128)]
    public string? ClientId { get; set; }

    [StringLength(256)]
    public string? ClientSecret { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;

    [StringLength(128)]
    public string? AdminUser { get; set; }

    [StringLength(256)]
    public string? AdminSecret { get; set; }

    [StringLength(128)]
    public string? AppAccessGroup { get; set; }

    public List<KeycloakRoleMapping>? RoleMappings { get; set; }
}

public sealed class AuthenticationConnectionTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

using System.Text.Json;
using HCS.Settings;
using Xunit;

namespace HCS;

public sealed class KeycloakSettingsResolutionTests
{
    [Fact]
    public void Combine_BuildsAuthorityFromBaseUrlAndRealm()
    {
        Assert.Equal("https://sso.example/realms/bd", KeycloakAuthority.Combine("https://sso.example/", "bd"));
        Assert.Equal("http://localhost:5110/realms/bd", KeycloakAuthority.Combine("http://localhost:5110", null));
        Assert.Equal(string.Empty, KeycloakAuthority.Combine("  ", "bd"));
    }

    [Fact]
    public void TrySplit_ReadsBaseUrlAndRealmFromAuthority()
    {
        Assert.True(KeycloakAuthority.TrySplit("http://localhost:5110/realms/bd", out var baseUrl, out var realm));
        Assert.Equal("http://localhost:5110", baseUrl);
        Assert.Equal("bd", realm);
        Assert.Equal(
            "http://localhost:5110/realms/bd/.well-known/openid-configuration",
            KeycloakAuthority.DiscoveryUrl("http://localhost:5110/realms/bd"));
    }

    [Fact]
    public void Deserialize_FallsBackToDefaultMappingsForEmptyOrInvalidJson()
    {
        Assert.Equal(KeycloakSettingDefaults.RoleMappings.Length, KeycloakRoleMappingSerializer.Deserialize(null).Count);
        Assert.Equal(KeycloakSettingDefaults.RoleMappings.Length, KeycloakRoleMappingSerializer.Deserialize("[]").Count);
        Assert.Equal(KeycloakSettingDefaults.RoleMappings.Length, KeycloakRoleMappingSerializer.Deserialize("{").Count);
    }

    [Fact]
    public void Deserialize_KeepsCustomMappingsAndDropsDuplicates()
    {
        var json = KeycloakRoleMappingSerializer.Serialize(
        [
            new KeycloakRoleMapping { Group = "/bd-lead", Role = "lanhdao" },
            new KeycloakRoleMapping { Group = "bd-lead", Role = "admin" },
            new KeycloakRoleMapping { Group = " ", Role = "ignored" }
        ]);

        var mappings = KeycloakRoleMappingSerializer.Deserialize(json);

        Assert.Single(mappings);
        Assert.Equal("bd-lead", mappings[0].Group);
        Assert.Equal("lanhdao", mappings[0].Role);
        Assert.Contains("\"group\"", json, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(JsonDocument.Parse(json).RootElement.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public void Resolve_PrefersStoredValuesOverEnvironmentFallback()
    {
        var resolved = KeycloakSettingsResolution.Resolve(
            new KeycloakSettingValues
            {
                Enabled = "true",
                BaseUrl = "https://sso.saved",
                Realm = "hospital",
                ClientId = "saved-client",
                ClientSecret = "db-secret",
                RequireHttpsMetadata = "false",
                AppAccessGroup = "/bd-app-hcs",
                RoleMappings = KeycloakRoleMappingSerializer.Serialize(
                [
                    new KeycloakRoleMapping { Group = "bd-lead", Role = "lanhdao" }
                ]),
                Revision = "4"
            },
            new KeycloakSettingValues
            {
                Enabled = "false",
                Authority = "http://localhost:5110/realms/bd",
                ClientId = "hcs-free-auth",
                ClientSecret = "env-secret",
                RequireHttpsMetadata = "true"
            });

        Assert.True(resolved.Enabled);
        Assert.Equal("https://sso.saved", resolved.BaseUrl);
        Assert.Equal("hospital", resolved.Realm);
        Assert.Equal("https://sso.saved/realms/hospital", resolved.Authority);
        Assert.Equal("saved-client", resolved.ClientId);
        Assert.Equal("db-secret", resolved.ClientSecret);
        Assert.False(resolved.RequireHttpsMetadata);
        Assert.Equal("bd-app-hcs", resolved.AppAccessGroup);
        Assert.Equal("bd-lead", resolved.RoleMappings[0].Group);
        Assert.Equal("4", resolved.Revision);
        Assert.True(resolved.HasClientSecret);
    }

    [Fact]
    public void Resolve_DefaultsEnabledWhenNothingIsConfigured()
    {
        var resolved = KeycloakSettingsResolution.Resolve(new KeycloakSettingValues(), new KeycloakSettingValues());

        Assert.True(resolved.Enabled);
        Assert.Equal(KeycloakSettingDefaults.Realm, resolved.Realm);
        Assert.Equal(KeycloakSettingDefaults.ClientId, resolved.ClientId);
        Assert.Equal(KeycloakSettingDefaults.AppAccessGroup, resolved.AppAccessGroup);
    }

    [Fact]
    public void Resolve_UsesEnvironmentAuthorityWhenDatabaseIsEmpty()
    {
        var resolved = KeycloakSettingsResolution.Resolve(
            new KeycloakSettingValues(),
            new KeycloakSettingValues
            {
                Enabled = "true",
                Authority = "http://localhost:5110/realms/bd",
                ClientId = "hcs-free-auth",
                ClientSecret = "env-secret",
                RequireHttpsMetadata = "false",
                MetadataAddress = "http://host.docker.internal:5110/realms/bd/.well-known/openid-configuration"
            });

        Assert.True(resolved.Enabled);
        Assert.Equal("http://localhost:5110", resolved.BaseUrl);
        Assert.Equal("bd", resolved.Realm);
        Assert.Equal("hcs-free-auth", resolved.ClientId);
        Assert.Equal("env-secret", resolved.ClientSecret);
        Assert.False(resolved.RequireHttpsMetadata);
        Assert.Equal(
            "http://host.docker.internal:5110/realms/bd/.well-known/openid-configuration",
            resolved.MetadataAddress);
    }

    [Fact]
    public void EmptySecretDoesNotReplaceStoredSecret()
    {
        Assert.False(KeycloakSettingsResolution.ShouldReplaceSecret(null));
        Assert.False(KeycloakSettingsResolution.ShouldReplaceSecret("  "));
        Assert.True(KeycloakSettingsResolution.ShouldReplaceSecret("new-secret"));
        Assert.Equal("2", KeycloakSettingsResolution.NextRevision("1"));

        var merged = AuthenticationSettingsAppService.MergeInput(
            new UpdateAuthenticationSettingsDto
            {
                ClientSecret = " ",
                AdminSecret = null,
                RoleMappings = [new KeycloakRoleMapping { Group = "bd-admin", Role = "admin" }]
            },
            new KeycloakSettingValues
            {
                ClientSecret = "kept-client",
                AdminSecret = "kept-admin"
            });

        Assert.Equal("kept-client", merged.ClientSecret);
        Assert.Equal("kept-admin", merged.AdminSecret);
    }
}

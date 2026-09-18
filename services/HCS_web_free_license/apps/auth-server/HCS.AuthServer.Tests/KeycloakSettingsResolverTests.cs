using HCS.Settings;
using Xunit;

namespace HCS.AuthServer.Tests;

public sealed class KeycloakSettingsResolverTests
{
    [Fact]
    public void StoredSettingsWinOverEnvironmentFallback()
    {
        var resolved = KeycloakSettingsResolution.Resolve(
            new KeycloakSettingValues
            {
                Enabled = "true",
                BaseUrl = "https://sso.saved",
                Realm = "hospital",
                ClientSecret = "db-secret"
            },
            new KeycloakSettingValues
            {
                Enabled = "false",
                Authority = "http://localhost:5110/realms/bd",
                ClientId = "hcs-free-auth",
                ClientSecret = "env-secret",
                RequireHttpsMetadata = "true",
                MetadataAddress =
                    "http://host.docker.internal:5110/realms/bd/.well-known/openid-configuration"
            });

        Assert.True(resolved.Enabled);
        Assert.Equal("https://sso.saved/realms/hospital", resolved.Authority);
        Assert.Equal("db-secret", resolved.ClientSecret);
        Assert.Equal("hcs-free-auth", resolved.ClientId);
        Assert.Equal(
            "http://host.docker.internal:5110/realms/bd/.well-known/openid-configuration",
            resolved.MetadataAddress);
    }
}

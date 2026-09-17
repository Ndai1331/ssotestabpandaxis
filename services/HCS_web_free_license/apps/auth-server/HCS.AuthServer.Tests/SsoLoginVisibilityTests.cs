using Xunit;

namespace HCS.AuthServer.Tests;

public sealed class SsoLoginVisibilityTests
{
    [Fact]
    public void Admin_Toggle_Off_Hides_Sso_Even_When_Keycloak_Is_Enabled()
    {
        Assert.False(SsoLoginVisibility.IsVisible("false", hasExternalProviders: true, keycloakEnabled: true));
    }

    [Fact]
    public void Default_Setting_Hides_Sso_When_No_Provider_Is_Available()
    {
        Assert.False(SsoLoginVisibility.IsVisible(null, hasExternalProviders: false, keycloakEnabled: false));
    }

    [Fact]
    public void Enabled_Setting_Shows_Sso_When_Keycloak_Is_Configured()
    {
        Assert.True(SsoLoginVisibility.IsVisible("true", hasExternalProviders: false, keycloakEnabled: true));
    }
}

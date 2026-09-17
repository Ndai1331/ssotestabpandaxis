using System;

namespace HCS.AuthServer;

public static class SsoLoginVisibility
{
    public static bool IsVisible(string? configuredValue, bool hasExternalProviders, bool keycloakEnabled)
    {
        if (string.Equals(configuredValue, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return hasExternalProviders || keycloakEnabled;
    }
}

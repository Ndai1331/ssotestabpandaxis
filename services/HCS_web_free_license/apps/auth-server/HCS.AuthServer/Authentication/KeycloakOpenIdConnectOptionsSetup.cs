using HCS.Settings;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace HCS.AuthServer;

public sealed class KeycloakOpenIdConnectOptionsSetup(
    KeycloakSettingsResolver resolver) : IConfigureNamedOptions<OpenIdConnectOptions>
{
    public void Configure(OpenIdConnectOptions options) => Configure(Options.DefaultName, options);

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (!string.Equals(name, KeycloakOptions.Scheme, StringComparison.Ordinal))
        {
            return;
        }

        Apply(options, resolver.Current, forceRefresh: false);
    }

    public static void Apply(
        OpenIdConnectOptions options,
        KeycloakResolvedSettings settings,
        bool forceRefresh)
    {
        var previousMetadata = options.MetadataAddress;
        if (!string.IsNullOrWhiteSpace(settings.Authority))
        {
            options.Authority = settings.Authority;
        }

        options.ClientId = settings.ClientId;
        options.ClientSecret = settings.ClientSecret;
        options.RequireHttpsMetadata = settings.RequireHttpsMetadata;

        var metadataAddress = FirstNonEmpty(
            settings.MetadataAddress,
            string.IsNullOrWhiteSpace(settings.Authority)
                ? previousMetadata
                : KeycloakAuthority.DiscoveryUrl(settings.Authority));
        if (string.IsNullOrWhiteSpace(metadataAddress))
        {
            return;
        }

        var metadataChanged = !string.Equals(previousMetadata, metadataAddress, StringComparison.OrdinalIgnoreCase);
        options.MetadataAddress = metadataAddress;
        if (metadataChanged || options.ConfigurationManager is null)
        {
            options.Configuration = null;
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = settings.RequireHttpsMetadata });
            return;
        }

        if (forceRefresh)
        {
            options.Configuration = null;
            options.ConfigurationManager.RequestRefresh();
        }
    }

    private static string FirstNonEmpty(params string?[] values)
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

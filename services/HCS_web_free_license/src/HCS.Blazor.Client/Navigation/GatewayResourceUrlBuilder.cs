using System;
using Microsoft.Extensions.Configuration;

namespace HCS.Blazor.Client.Navigation;

internal static class GatewayResourceUrlBuilder
{
    public static string Build(IConfiguration configuration, string resourceUrl)
    {
        if (string.IsNullOrWhiteSpace(resourceUrl))
            throw new ArgumentException("A resource URL is required.", nameof(resourceUrl));

        if (Uri.TryCreate(resourceUrl, UriKind.Absolute, out _))
            return resourceUrl;

        var configuredOrigin = configuration["Bff:PublicOrigin"] ?? configuration["RemoteServices:Default:BaseUrl"];
        if (!Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var origin) ||
            origin.Scheme != Uri.UriSchemeHttps || origin.AbsolutePath != "/")
        {
            throw new InvalidOperationException(
                "Bff:PublicOrigin or RemoteServices:Default:BaseUrl must be an absolute HTTPS gateway origin.");
        }

        return new Uri(origin, resourceUrl.TrimStart('/')).AbsoluteUri;
    }
}

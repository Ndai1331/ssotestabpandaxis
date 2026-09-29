using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HCS.Blazor;

/// <summary>
/// Browser config for the Blazor WebAssembly client. Container environment variables
/// override the static appsettings files that would otherwise be baked into the image.
/// </summary>
internal static class BlazorClientRuntimeSettings
{
    public const string BaseUrlKey = "BlazorClient:RemoteServices:Default:BaseUrl";
    public const string PublicOriginKey = "BlazorClient:Bff:PublicOrigin";
    public const string AccountUrlKey = "BlazorClient:Bff:AccountUrl";

    public static bool IsAppSettingsRequest(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return value.EndsWith("/appsettings.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "/appsettings.json", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("/appsettings.Production.json", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("/appsettings.Development.json", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasAnySetting(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration[BaseUrlKey])
        || !string.IsNullOrWhiteSpace(configuration[PublicOriginKey])
        || !string.IsNullOrWhiteSpace(configuration[AccountUrlKey]);

    public static string CreateJson(IConfiguration configuration)
    {
        var baseUrl = configuration[BaseUrlKey]?.Trim();
        var publicOrigin = configuration[PublicOriginKey]?.Trim();
        var accountUrl = configuration[AccountUrlKey]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl)
            || string.IsNullOrWhiteSpace(publicOrigin)
            || string.IsNullOrWhiteSpace(accountUrl))
        {
            throw new InvalidOperationException(
                "Set BlazorClient__RemoteServices__Default__BaseUrl, BlazorClient__Bff__PublicOrigin, and BlazorClient__Bff__AccountUrl together.");
        }

        var payload = new
        {
            RemoteServices = new
            {
                Default = new
                {
                    BaseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"
                }
            },
            Bff = new
            {
                PublicOrigin = publicOrigin.TrimEnd('/'),
                AccountUrl = accountUrl
            }
        };

        return JsonSerializer.Serialize(payload);
    }
}

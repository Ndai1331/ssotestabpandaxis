using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace HCS.Blazor.Client.Authentication;

/// <summary>
/// Sends the active UI culture on every BFF/API call so remote ABP localization
/// and platform culture filters resolve the same language as the Blazor client.
/// </summary>
public sealed class CultureHttpMessageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var culture = CultureInfo.DefaultThreadCurrentUICulture?.Name
            ?? CultureInfo.CurrentUICulture.Name;
        if (!string.IsNullOrWhiteSpace(culture))
        {
            if (!request.Headers.Contains("Accept-Language"))
            {
                request.Headers.TryAddWithoutValidation("Accept-Language", culture);
            }

            if (request.RequestUri is not null &&
                request.RequestUri.AbsolutePath.Contains(
                    "/api/abp/application-localization",
                    StringComparison.OrdinalIgnoreCase))
            {
                var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
                if (!query.ContainsKey("CultureName"))
                {
                    request.RequestUri = new Uri(QueryHelpers.AddQueryString(
                        request.RequestUri.GetLeftPart(UriPartial.Path),
                        "CultureName",
                        culture));
                }
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}

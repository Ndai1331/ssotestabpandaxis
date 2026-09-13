using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;

namespace HCS.Blazor.Client.Authentication;

public sealed class BusyHttpMessageHandler(HcsBusyTracker tracker) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var track = ShouldTrack(request);
        if (track)
        {
            tracker.Begin();
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            if (track)
            {
                tracker.End();
            }
        }
    }

    internal static bool ShouldTrack(HttpRequestMessage request)
    {
        if (request.Method == HttpMethod.Options || request.Method == HttpMethod.Head)
        {
            return false;
        }

        var path = request.RequestUri?.AbsolutePath ?? "";
        if (path.Length == 0)
        {
            return true;
        }

        if (path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/bff/user", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/bff/antiforgery", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/unread-count", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (request.Method == HttpMethod.Get &&
            (path.Equals("/api/notifications", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith("/avatar", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}

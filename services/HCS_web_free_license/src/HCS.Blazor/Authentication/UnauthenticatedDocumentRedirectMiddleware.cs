using System.Threading.Tasks;
using HCS.Blazor.Client.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HCS.Blazor;

/// <summary>
/// Sends unauthenticated HTML navigations to BFF login before the WASM payload downloads.
/// </summary>
internal sealed class UnauthenticatedDocumentRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (BffUnauthenticatedDocumentRedirect.TryGetLoginUrl(
                context.User.Identity?.IsAuthenticated == true,
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Request.QueryString.Value,
                context.Request.Headers.Accept.ToString(),
                configuration,
                out var loginUrl))
        {
            context.Response.Redirect(loginUrl);
            return;
        }

        await next(context);
    }
}

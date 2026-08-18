using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace HCS.WebGateway;

internal static class BffEndpoints
{
    private const string DefaultReturnUrl = "https://localhost:44403/";

    internal static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/bff/login", (HttpContext context, IConfiguration configuration, string? returnUrl) =>
        {
            var safeReturnUrl = GetSafeReturnUrl(configuration, returnUrl);
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = safeReturnUrl },
                [HCSWebGatewayModule.OidcScheme]);
        }).AllowAnonymous();

        endpoints.MapPost("/bff/logout", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.Headers["X-HCS-Antiforgery"] = "invalid";
                return Results.BadRequest(new { error = "invalid_antiforgery_token" });
            }

            await context.SignOutAsync(HCSWebGatewayModule.CookieScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapGet("/bff/user", (HttpContext context, ClaimsPrincipal user) =>
        {
            // #region agent log
            var cookieHeader = context.Request.Headers.Cookie.ToString();
            var cookieNames = cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2)[0])
                .Where(name => name.StartsWith(".HCS.Bff", StringComparison.Ordinal))
                .ToArray();
            _ = AgentDebugLog.WriteAsync(
                "A,B,C",
                "BffEndpoints.cs:/bff/user",
                "bff/user auth probe",
                new
                {
                    isAuthenticated = user.Identity?.IsAuthenticated == true,
                    cookieHeaderLength = cookieHeader.Length,
                    hcsBffCookieNames = cookieNames,
                    hasChunkMarker = cookieNames.Contains(".HCS.Bff"),
                    chunkCount = cookieNames.Count(name => name.StartsWith(".HCS.BffC", StringComparison.Ordinal))
                });
            // #endregion
            return Results.Ok(new
            {
                isAuthenticated = user.Identity?.IsAuthenticated == true,
                name = user.Identity?.Name,
                claims = user.Claims
                    .Where(claim => IsPublicClaim(claim.Type))
                    .Select(claim => new { claim.Type, claim.Value })
            });
        }).RequireAuthorization();

        endpoints.MapGet("/bff/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken, headerName = tokens.HeaderName });
        }).RequireAuthorization();

        return endpoints;
    }

    internal static string GetSafeReturnUrl(IConfiguration configuration, string? returnUrl)
    {
        var allowedOrigins = HCSWebGatewayModule.GetCorsOrigins(configuration);
        var fallback = allowedOrigins.FirstOrDefault() ?? DefaultReturnUrl;
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var candidate))
        {
            return fallback;
        }

        var candidateOrigin = candidate.GetLeftPart(UriPartial.Authority);
        return allowedOrigins.Contains(candidateOrigin, StringComparer.OrdinalIgnoreCase)
            ? candidate.ToString()
            : fallback;
    }

    private static bool IsPublicClaim(string type) => type is
        "sub" or "name" or "preferred_username" or "email" or "role" or "permission" or ClaimTypes.Name or ClaimTypes.Role;
}

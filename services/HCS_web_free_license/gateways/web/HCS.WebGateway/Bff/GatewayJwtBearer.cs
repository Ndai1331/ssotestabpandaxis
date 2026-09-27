using HCS.Bff;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace HCS.WebGateway;

/// <summary>
/// Native mobile sends OpenIddict access tokens (<c>typ: at+jwt</c>) to the Gateway.
/// Microsoft JwtBearer defaults to type <c>JWT</c> and a single issuer string, which
/// rejects those tokens even when /connect/userinfo accepts them.
/// </summary>
internal static class GatewayJwtBearer
{
    internal const string AccessTokenType = "at+jwt";
    internal const string JwtTokenType = "JWT";

    internal static IReadOnlyList<string> ResolveIssuers(IConfiguration configuration)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        Add(values, configuration["Authentication:Authority"]);
        foreach (var extra in configuration.GetSection("Authentication:ValidIssuers").Get<string[]>() ?? [])
        {
            Add(values, extra);
        }

        return [.. values];
    }

    internal static void Configure(JwtBearerOptions options, IConfiguration configuration, string authority)
    {
        var bearerAudience = configuration["Authentication:BearerAudience"]?.Trim();
        if (string.IsNullOrWhiteSpace(bearerAudience))
        {
            bearerAudience = "HCS";
        }

        options.Authority = authority;
        options.Audience = bearerAudience;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidTypes = [AccessTokenType, JwtTokenType];
        options.TokenValidationParameters.NameClaimType = "sub";
        options.TokenValidationParameters.RoleClaimType = BffRoleClaims.JwtRole;

        var metadataAddress = configuration["Authentication:BearerMetadataAddress"]?.Trim();
        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        var metadataIsHttp = Uri.TryCreate(options.MetadataAddress, UriKind.Absolute, out var metadataUri) &&
            metadataUri.Scheme == Uri.UriSchemeHttp;
        options.RequireHttpsMetadata = !metadataIsHttp &&
            configuration.GetValue("Authentication:RequireHttpsMetadata", true);

        if (configuration.GetValue("Authentication:AllowUntrustedBackchannelCertificate", false))
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        var issuers = ResolveIssuers(configuration);
        if (issuers.Count > 0)
        {
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidIssuers = issuers;
            options.TokenValidationParameters.IssuerValidator = (issuer, _, _) =>
                issuers.Contains(issuer, StringComparer.Ordinal)
                    ? issuer
                    : throw new SecurityTokenInvalidIssuerException(
                        $"IDX10205: Issuer '{issuer}' is not in the configured Authentication issuers.");
        }

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(context.Token))
                {
                    context.Token = context.Request.Query["access_token"];
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                context.HttpContext.RequestServices
                    .GetService<ILoggerFactory>()
                    ?.CreateLogger("HCS.WebGateway.MobileBearer")
                    .LogWarning(context.Exception, "Mobile bearer token rejected by Gateway JWT validation.");
                return Task.CompletedTask;
            }
        };
    }

    private static void Add(ISet<string> values, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim().TrimEnd('/');
        values.Add(trimmed);
        values.Add(trimmed + "/");
    }
}

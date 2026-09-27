using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.Security.Claims;

namespace HCS;

/// <summary>
/// Resource-service JWT validation for OpenIddict access tokens
/// (<c>typ: at+jwt</c>). Authority comes from <c>AuthServer:Authority</c>
/// (per-hospital env), not a hardcoded host.
/// </summary>
public static class HcsServiceJwtBearer
{
    public const string AccessTokenType = "at+jwt";
    public const string JwtTokenType = "JWT";
    public const string JwtSubjectClaim = "sub";
    public const string JwtRoleClaim = "role";

    public static IReadOnlyList<string> ResolveIssuers(IConfiguration configuration)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        Add(values, configuration["AuthServer:Authority"]);
        foreach (var extra in configuration.GetSection("AuthServer:ValidIssuers").Get<string[]>() ?? [])
        {
            Add(values, extra);
        }

        return [.. values];
    }

    public static void AlignAbpClaimTypes()
    {
        AbpClaimTypes.UserId = JwtSubjectClaim;
        AbpClaimTypes.Role = JwtRoleClaim;
        AbpClaimTypes.UserName = "preferred_username";
        AbpClaimTypes.Name = "given_name";
        AbpClaimTypes.SurName = "family_name";
        AbpClaimTypes.Email = "email";
    }

    public static void Configure(JwtBearerOptions options, IConfiguration configuration)
    {
        AlignAbpClaimTypes();
        var authority = configuration["AuthServer:Authority"]
            ?? throw new InvalidOperationException("AuthServer:Authority is required.");

        options.Authority = authority;
        options.Audience = configuration["AuthServer:Audience"] ?? "HCS";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidTypes = [AccessTokenType, JwtTokenType];
        options.TokenValidationParameters.NameClaimType = JwtSubjectClaim;
        options.TokenValidationParameters.RoleClaimType = JwtRoleClaim;

        var metadataAddress = configuration["AuthServer:MetadataAddress"]?.Trim()
            ?? configuration["AuthServer:BearerMetadataAddress"]?.Trim();
        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        var metadataIsHttp = Uri.TryCreate(options.MetadataAddress, UriKind.Absolute, out var metadataUri) &&
            metadataUri.Scheme == Uri.UriSchemeHttp;
        options.RequireHttpsMetadata = !metadataIsHttp &&
            configuration.GetValue("AuthServer:RequireHttpsMetadata", true);

        if (configuration.GetValue("AuthServer:AllowUntrustedBackchannelCertificate", false))
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
                        $"IDX10205: Issuer '{issuer}' is not in the configured AuthServer issuers.");
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

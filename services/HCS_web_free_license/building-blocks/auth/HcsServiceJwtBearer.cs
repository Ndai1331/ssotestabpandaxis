using Microsoft.AspNetCore.Authentication;
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
        Add(values, configuration["Authentication:Authority"]);
        foreach (var extra in configuration.GetSection("AuthServer:ValidIssuers").Get<string[]>() ?? [])
        {
            Add(values, extra);
        }

        foreach (var extra in configuration.GetSection("Authentication:ValidIssuers").Get<string[]>() ?? [])
        {
            Add(values, extra);
        }

        return [.. values];
    }

    /// <summary>
    /// ABP <c>AbpOpenIddictAspNetCoreModule</c> registers OpenIddict.Validation and
    /// may reset the default scheme after <c>AddJwtBearer</c>. Resource APIs must
    /// authenticate with Microsoft JwtBearer against Auth Server JWKS.
    /// </summary>
    public static void ForceDefaultSchemes(IServiceCollection services)
    {
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        });
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
            ?? configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("AuthServer:Authority is required.");

        options.Authority = authority;
        options.Audience = configuration["AuthServer:Audience"]
            ?? configuration["Authentication:BearerAudience"]
            ?? "HCS";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidTypes = [AccessTokenType, JwtTokenType];
        options.TokenValidationParameters.NameClaimType = JwtSubjectClaim;
        options.TokenValidationParameters.RoleClaimType = JwtRoleClaim;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidAudiences = [options.Audience];

        var metadataAddress = configuration["AuthServer:MetadataAddress"]?.Trim()
            ?? configuration["AuthServer:BearerMetadataAddress"]?.Trim()
            ?? configuration["Authentication:BearerMetadataAddress"]?.Trim();
        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        var metadataIsHttp = Uri.TryCreate(options.MetadataAddress, UriKind.Absolute, out var metadataUri) &&
            metadataUri.Scheme == Uri.UriSchemeHttp;
        options.RequireHttpsMetadata = !metadataIsHttp &&
            configuration.GetValue("AuthServer:RequireHttpsMetadata",
                configuration.GetValue("Authentication:RequireHttpsMetadata", true));

        if (configuration.GetValue("AuthServer:AllowUntrustedBackchannelCertificate", false)
            || configuration.GetValue("Authentication:AllowUntrustedBackchannelCertificate", false))
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
                var bearer = ReadBearerToken(
                    context.Request.Headers.Authorization.Count > 0
                        ? context.Request.Headers.Authorization[0]
                        : null);
                if (!string.IsNullOrWhiteSpace(bearer))
                {
                    context.Token = bearer;
                }
                else if (context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) &&
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
                    ?.CreateLogger("HCS.JwtBearer")
                    .LogWarning(context.Exception, "Bearer token rejected by resource-service JWT validation.");
                return Task.CompletedTask;
            }
        };
    }

    /// <summary>
    /// YARP may forward two <c>Authorization</c> values. ASP.NET joins them with
    /// <c>, </c>, turning a 3-part JWS into a 5-part string (IDX14309 / JWE IV).
    /// </summary>
    public static string? ReadBearerToken(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        var first = authorization;
        var cut = first.IndexOf(", Bearer ", StringComparison.OrdinalIgnoreCase);
        if (cut >= 0)
        {
            first = first[..cut];
        }

        const string prefix = "Bearer ";
        if (!first.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = first[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
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

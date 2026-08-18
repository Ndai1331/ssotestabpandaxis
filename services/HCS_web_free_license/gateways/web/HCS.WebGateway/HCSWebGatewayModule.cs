using System.Text.Json;
using System.Security.Claims;
using HCS.Bff;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Cryptography.X509Certificates;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace HCS.WebGateway;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule))]
public sealed class HCSWebGatewayModule : AbpModule
{
    internal const string CorsPolicyName = "HCS.WebGateway";
    internal const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    internal const string OidcScheme = "HCS.Bff.Oidc";
    internal const string CookieName = ".HCS.Bff";
    internal const string DataProtectionApplicationName = "HCS.Bff";

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var environment = context.Services.GetHostingEnvironment();

        BffDeploymentPolicy.ValidateBrowserOrigins(configuration, environment.IsDevelopment());
        context.Services.AddSingleton(TimeProvider.System);
        ConfigureDataProtection(context.Services, configuration, environment);
        ConfigureAuthentication(context.Services, configuration);
        context.Services.AddSingleton<BffTokenRefreshService>();
        context.Services.AddHttpClient("HCS.Bff.TokenRefresh", client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(
                configuration.GetValue("Authentication:RefreshTimeoutSeconds", 10), 2, 30)));
        context.Services.AddAuthorization(options =>
        {
            options.AddPolicy("HCS.Proxy", policy => policy.RequireAssertion(authorizationContext =>
            {
                if (authorizationContext.Resource is HttpContext httpContext &&
                    BffRequestPolicy.IsAnonymousBootstrapPath(httpContext.Request.Path))
                {
                    return true;
                }

                return authorizationContext.User.Identity?.IsAuthenticated == true;
            }));
        });
        context.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = ".HCS.Bff.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.HeaderName = "X-XSRF-TOKEN";
        });

        context.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy => policy
                .WithOrigins(GetCorsOrigins(configuration))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });

        context.Services
            .AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(BffAccessTokenTransform.Add);

        context.Services.AddHealthChecks();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var environment = context.GetEnvironment();

        if (environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseCors(CorsPolicyName);
        app.UseMiddleware<BffWebSocketOriginMiddleware>();
        app.UseMiddleware<BffCookieChunkCleanupMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<BffAccessTokenMiddleware>();
        app.UseMiddleware<BffAntiforgeryMiddleware>();
        app.UseAbpSerilogEnrichers();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthResponseAsync
            });
            endpoints.MapBffEndpoints();
            endpoints.MapReverseProxy().RequireAuthorization("HCS.Proxy");
        });
    }

    internal static void ConfigureDataProtection(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var certificate = LoadDataProtectionCertificate(configuration, environment.IsDevelopment());
        var redisConnectionString = GetRequiredValue(configuration, "DataProtection:Redis");
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        var connectTimeoutSeconds = Math.Clamp(configuration.GetValue("DataProtection:ConnectTimeoutSeconds", 5), 1, 15);
        redisOptions.ConnectTimeout = checked(connectTimeoutSeconds * 1000);
        redisOptions.SyncTimeout = checked(connectTimeoutSeconds * 1000);
        redisOptions.ConnectRetry = 1;
        redisOptions.AbortOnConnectFail = true;
        var redis = ConnectionMultiplexer.Connect(redisOptions);
        services.AddSingleton<IConnectionMultiplexer>(redis);
        var dataProtection = services.AddDataProtection()
            .SetApplicationName(DataProtectionApplicationName)
            .PersistKeysToStackExchangeRedis(redis, "HCS-DataProtection-Keys");

        if (certificate is not null)
        {
            dataProtection.ProtectKeysWithCertificate(certificate);
        }
    }

    internal static X509Certificate2? LoadDataProtectionCertificate(IConfiguration configuration, bool isDevelopment)
    {
        var certificatePath = configuration["DataProtection:Certificate:Path"];
        var certificatePassword = configuration["DataProtection:Certificate:Password"];
        if (string.IsNullOrWhiteSpace(certificatePath) || string.IsNullOrWhiteSpace(certificatePassword))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "Production requires DataProtection:Certificate:Path and Password from environment/User Secrets to protect Redis keys at rest.");
            }

            if (!string.IsNullOrWhiteSpace(certificatePath) || !string.IsNullOrWhiteSpace(certificatePassword))
            {
                throw new InvalidOperationException("Data Protection certificate path and password must be supplied together.");
            }

            return null;
        }

        if (!Path.IsPathFullyQualified(certificatePath) || !File.Exists(certificatePath))
        {
            throw new InvalidOperationException("Data Protection certificate path must be an existing absolute path.");
        }

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, certificatePassword);
        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("Data Protection certificate must contain a private key.");
        }

        return certificate;
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var authority = GetRequiredAbsoluteHttpsUrl(configuration, "Authentication:Authority");
        var clientId = GetRequiredValue(configuration, "Authentication:ClientId");
        var clientSecret = GetRequiredValue(configuration, "Authentication:ClientSecret");
        var cookieDomain = BffDeploymentPolicy.ValidateAndGetCookieDomain(configuration);

        services.AddSingleton<ITicketStore, BffAuthTicketStore>();
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieScheme;
                options.DefaultSignInScheme = CookieScheme;
                // Proxy endpoints must return 401. Only /bff/login explicitly challenges OIDC.
                options.DefaultChallengeScheme = CookieScheme;
            })
            .AddCookie(CookieScheme, options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                // form_post callback is cross-site (auth.hcs.localhost → hcs.localhost).
                // SameSite=Lax is not stored on that POST in Chrome, so the BFF session never sticks.
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.Path = "/";
                options.Cookie.Domain = cookieDomain;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnSigningIn = signingInContext =>
                {
                    // #region agent log
                    _ = AgentDebugLog.WriteAsync(
                        "A,B",
                        "HCSWebGatewayModule.cs:OnSigningIn",
                        "BFF session cookie issuing",
                        new
                        {
                            cookieName = options.Cookie.Name,
                            sameSite = options.Cookie.SameSite.ToString(),
                            securePolicy = options.Cookie.SecurePolicy.ToString(),
                            cookieDomain = options.Cookie.Domain,
                            path = signingInContext.Request.Path.Value,
                            method = signingInContext.Request.Method
                        });
                    // #endregion
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToLogin = cookieContext =>
                {
                    if (BffRequestPolicy.IsProtectedResourcePath(cookieContext.Request.Path))
                    {
                        // #region agent log
                        if (cookieContext.Request.Path.StartsWithSegments("/bff/user"))
                        {
                            var cookieHeader = cookieContext.Request.Headers.Cookie.ToString();
                            var cookieNames = cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(part => part.Split('=', 2)[0])
                                .Where(name => name.StartsWith(".HCS.Bff", StringComparison.Ordinal))
                                .ToArray();
                            _ = AgentDebugLog.WriteAsync(
                                "A,B,C",
                                "HCSWebGatewayModule.cs:OnRedirectToLogin",
                                "bff/user rejected as anonymous",
                                new
                                {
                                    cookieHeaderLength = cookieHeader.Length,
                                    hcsBffCookieNames = cookieNames,
                                    hasChunkMarker = cookieNames.Contains(".HCS.Bff"),
                                    chunkCount = cookieNames.Count(name => name.StartsWith(".HCS.BffC", StringComparison.Ordinal))
                                });
                        }
                        // #endregion
                        cookieContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    cookieContext.Response.Redirect(cookieContext.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = cookieContext =>
                {
                    cookieContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(OidcScheme, options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = configuration.GetValue("Authentication:RequireHttpsMetadata", true);
                if (configuration.GetValue("Authentication:AllowUntrustedBackchannelCertificate", false))
                {
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                }
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false;
                options.CallbackPath = "/signin-oidc";
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.SameSite = SameSiteMode.None;
                options.NonceCookie.HttpOnly = true;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.HttpOnly = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("roles");
                options.Scope.Add("HCS");
                options.Scope.Add("offline_access");
                options.Events.OnRedirectToIdentityProvider = oidcContext =>
                {
                    oidcContext.ProtocolMessage.Prompt = "login";
                    // #region agent log
                    _ = AgentDebugLog.WriteAsync(
                        "A",
                        "HCSWebGatewayModule.cs:OnRedirectToIdentityProvider",
                        "OIDC challenge redirect",
                        new
                        {
                            responseMode = oidcContext.ProtocolMessage.ResponseMode,
                            redirectUri = oidcContext.ProtocolMessage.RedirectUri,
                            responseType = oidcContext.ProtocolMessage.ResponseType
                        });
                    // #endregion
                    return Task.CompletedTask;
                };
                options.Events.OnTokenValidated = tokenContext =>
                {
                    var accessToken = tokenContext.TokenEndpointResponse?.AccessToken;
                    // #region agent log
                    _ = AgentDebugLog.WriteAsync(
                        "A,C",
                        "HCSWebGatewayModule.cs:OnTokenValidated",
                        "OIDC token validated",
                        new
                        {
                            hasAccessToken = !string.IsNullOrWhiteSpace(accessToken),
                            hasRefreshToken = !string.IsNullOrWhiteSpace(tokenContext.TokenEndpointResponse?.RefreshToken),
                            subject = tokenContext.Principal?.FindFirst("sub")?.Value,
                            path = tokenContext.HttpContext.Request.Path.Value,
                            method = tokenContext.HttpContext.Request.Method
                        });
                    // #endregion
                    if (tokenContext.Principal?.Identity is not ClaimsIdentity identity || string.IsNullOrWhiteSpace(accessToken))
                    {
                        return Task.CompletedTask;
                    }

                    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                    foreach (var permission in token.Claims.Where(claim => claim.Type == "permission"))
                    {
                        if (!identity.HasClaim("permission", permission.Value))
                        {
                            identity.AddClaim(new System.Security.Claims.Claim("permission", permission.Value));
                        }
                    }

                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = failureContext =>
                {
                    // #region agent log
                    _ = AgentDebugLog.WriteAsync(
                        "A,E",
                        "HCSWebGatewayModule.cs:OnRemoteFailure",
                        "OIDC remote failure",
                        new
                        {
                            failure = failureContext.Failure?.GetType().Name,
                            message = failureContext.Failure?.Message
                        });
                    // #endregion
                    // GET /signin-oidc without state (refresh/bookmark) must not strand the user.
                    failureContext.Response.Redirect(BffDeploymentPolicy.GetGatewayOrigin(configuration).AbsoluteUri);
                    failureContext.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        services.AddOptions<CookieAuthenticationOptions>(CookieScheme)
            .Configure<ITicketStore>((options, store) => options.SessionStore = store);
    }

    internal static string GetRequiredValue(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required configuration '{key}' is missing. Supply it through environment variables or user secrets.");
        }

        return value;
    }

    internal static string GetRequiredAbsoluteHttpsUrl(IConfiguration configuration, string key)
    {
        var value = GetRequiredValue(configuration, key);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException($"Configuration '{key}' must be an absolute HTTPS origin without a path.");
        }

        return value.TrimEnd('/');
    }

    internal static string[] GetCorsOrigins(IConfiguration configuration)
    {
        var origins = configuration.GetSection("App:CorsOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0)
        {
            throw new InvalidOperationException("App:CorsOrigins must contain at least one origin.");
        }

        var normalized = origins
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var origin in normalized)
        {
            if (string.IsNullOrWhiteSpace(origin) || origin == "*" ||
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidOperationException(
                    $"Invalid CORS origin '{origin}'. Configure an absolute HTTP(S) origin without wildcard, path, query, or fragment.");
            }
        }

        return normalized;
    }

    private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration
        }));
    }
}

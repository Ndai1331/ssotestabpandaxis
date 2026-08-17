using System;
using System.IO;
using global::MudBlazor;
using Volo.Abp.MudBlazorUI;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore.Components.Web.Theming.MudBlazor.Routing;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Mapperly;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi;
using hanhchinhso.Blazor.Client;
using hanhchinhso.Blazor.Client.Navigation;
using hanhchinhso.Blazor.Components;
using StackExchange.Redis;
using Volo.Abp.AspNetCore.Authentication.OpenIdConnect;
using Volo.Abp.AspNetCore.Components.Web;
using Volo.Abp.AspNetCore.Mvc.Client;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.AspNetCore.Components.WebAssembly.Theming.MudBlazor.Bundling;
using Volo.Abp.AspNetCore.Components.Server.MudBlazorLeptonXTheme;
using Volo.Abp.AspNetCore.Components.Server.MudBlazorLeptonXTheme.Bundling;
using Volo.Abp.AspNetCore.Components.WebAssembly.MudBlazorLeptonXTheme.Bundling;
using Volo.Abp.AspNetCore.Components.Web.MudBlazorLeptonXTheme;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonX;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonX.Bundling;
using Volo.Abp.AspNetCore.Components.Web.MudBlazorLeptonXTheme;
using Volo.Abp.LeptonX.Shared;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Http.Client.Web;
using Volo.Abp.Http.Client;
using Volo.Abp.Http.Client.IdentityModel.Web;
using Volo.Abp.Security.Claims;
using Volo.Abp.Account;
using Volo.Abp.Account.LinkUsers;
using Volo.Abp.Account.Pro.Admin.Blazor.MudBlazor.Server;
using Volo.Abp.Account.Pro.Public.Blazor.MudBlazor.Server;
using Volo.Abp.Account.Public.Web.Impersonation;
using Volo.Abp.Account.Pro.Public.Blazor.MudBlazor.WebAssembly.Bundling;
using Volo.Abp.Identity.Pro.Blazor.MudBlazor;
using Volo.Abp.Identity.Pro.Blazor.MudBlazor.Server;
using Volo.Abp.Identity;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.Blazor.MudBlazor.Server;
using Volo.Abp.AuditLogging.Blazor.MudBlazor.WebAssembly.Bundling;
using hanhchinhso.AuditLoggingService;
using Volo.Abp.Gdpr;
using Volo.Abp.Gdpr.Blazor.MudBlazor.Extensions;
using Volo.Abp.Gdpr.Blazor.MudBlazor.Server;
using hanhchinhso.GdprService;
using Volo.Abp.LanguageManagement;
using Volo.Abp.LanguageManagement.Blazor.MudBlazor.Server;
using hanhchinhso.LanguageService;
using hanhchinhso.LanguageService.Localization;
using hanhchinhso.OrganizationService;
using hanhchinhso.DocumentService;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.Pro;
using Volo.Abp.OpenIddict.Pro.Blazor.MudBlazor.Server;
using Volo.Abp.TextTemplateManagement;
using Volo.Abp.TextTemplateManagement.Blazor.MudBlazor.Server;
using Volo.AIManagement;
using Volo.AIManagement.Client;
using Volo.AIManagement.Blazor.MudBlazor.Server;
using Volo.AIManagement.Client.Blazor.MudBlazor.Server;
using hanhchinhso.AIManagementService;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.FeatureManagement;
using hanhchinhso.IdentityService;
using hanhchinhso.AdministrationService;

namespace hanhchinhso.Blazor;

[DependsOn(
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpDistributedLockingModule),
    typeof(AbpAutofacModule),
    typeof(AbpGdprBlazorMudBlazorServerModule),
    typeof(AbpGdprHttpApiClientModule),
    typeof(hanhchinhsoGdprServiceContractsModule),
    typeof(AbpAccountPublicBlazorMudBlazorWebAssemblyBundlingModule),
    typeof(AbpAccountPublicWebImpersonationModule),
    typeof(AbpAccountAdminBlazorMudBlazorServerModule),
    typeof(AbpAccountAdminHttpApiClientModule),
    typeof(AbpAccountPublicBlazorMudBlazorServerModule),
    typeof(AbpAccountPublicHttpApiModule),
    typeof(AbpAccountPublicHttpApiClientModule),
    typeof(AbpIdentityProBlazorMudBlazorServerModule),
    typeof(AbpIdentityHttpApiClientModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(TextTemplateManagementBlazorMudBlazorServerModule),
    typeof(TextTemplateManagementHttpApiClientModule),
    typeof(hanhchinhsoAuditLoggingServiceContractsModule),
    typeof(AbpAuditLoggingBlazorMudBlazorServerModule),
    typeof(AbpAuditLoggingBlazorMudBlazorWebAssemblyBundlingModule),
    typeof(AbpAuditLoggingHttpApiClientModule),
    typeof(AbpOpenIddictProBlazorMudBlazorServerModule),
    typeof(AbpOpenIddictProHttpApiClientModule),
    typeof(LanguageManagementBlazorMudBlazorServerModule),
    typeof(LanguageManagementHttpApiClientModule),
    typeof(hanhchinhsoLanguageServiceContractsModule),
    typeof(hanhchinhsoOrganizationServiceContractsModule),
    typeof(hanhchinhsoDocumentServiceContractsModule),
    typeof(hanhchinhsoAIManagementServiceContractsModule),
    typeof(AIManagementBlazorMudBlazorServerModule),
    typeof(AIManagementClientBlazorMudBlazorServerModule),
    typeof(AIManagementHttpApiClientModule),
    typeof(AIManagementClientHttpApiClientModule),
    typeof(AIManagementHttpApiModule),
    typeof(AbpAspNetCoreComponentsServerMudBlazorLeptonXThemeModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyMudBlazorLeptonXThemeBundlingModule),
    typeof(AbpAspNetCoreMvcUiLeptonXThemeModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAspNetCoreMvcClientModule),
    typeof(AbpAspNetCoreAuthenticationOpenIdConnectModule),
    typeof(AbpHttpClientWebModule),
    typeof(AbpHttpClientIdentityModelWebModule),
    typeof(AbpSettingManagementHttpApiClientModule),
    typeof(AbpPermissionManagementHttpApiClientModule),
    typeof(AbpFeatureManagementHttpApiClientModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(hanhchinhsoIdentityServiceContractsModule),
    typeof(hanhchinhsoAdministrationServiceContractsModule)
    )]
public class hanhchinhsoBlazorModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(
                typeof(LanguageServiceResource),
                typeof(hanhchinhsoBlazorModule).Assembly
            );
        });

        PreConfigure<AbpAspNetCoreComponentsWebOptions>(options =>
        {
            options.IsBlazorWebApp = true;
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHttpClientProxies(
            typeof(hanhchinhsoOrganizationServiceContractsModule).Assembly,
            "OrganizationService");
        context.Services.AddHttpClientProxies(
            typeof(hanhchinhsoDocumentServiceContractsModule).Assembly,
            "DocumentService");

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        // Add services to the container.
        context.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        ConfigureLocalization(hostingEnvironment);
        ConfigureUrls(configuration);
        ConfigureBundles();
        ConfigureAuthentication(context, configuration);
        ConfigureImpersonation(context, configuration);
        ConfigureSwaggerServices(context.Services);
        ConfigureCache(configuration);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        ConfigureDistributedLocking(context, configuration);
        ConfigureBlazorise(context);
        ConfigureRouter();
        ConfigureMenu(configuration);
        ConfigureCookieConsent(context);
        ConfigureTheme();
    }

    private void ConfigureLocalization(IWebHostEnvironment hostingEnvironment)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<hanhchinhsoBlazorModule>();
        });
        
        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<hanhchinhsoBlazorModule>(hostingEnvironment.ContentRootPath);
            });
        }

    }



    private void ConfigureCookieConsent(ServiceConfigurationContext context)
    {
        context.Services.AddAbpCookieConsent(options =>
        {
            options.IsEnabled = true;
            options.CookiePolicyUrl = "/CookiePolicy";
            options.PrivacyPolicyUrl = "/PrivacyPolicy";
        });
    }
    
    private void ConfigureTheme()
    {
        Configure<LeptonXThemeOptions>(options =>
        {
            options.DefaultStyle = LeptonXStyleNames.Light;
        });
        
        Configure<LeptonXThemeMvcOptions>(options =>
        {
            options.ApplicationLayout = LeptonXMvcLayouts.SideMenu;
            options.ApplicationLayout = LeptonXMvcLayouts.SideMenu;
        });

        Configure<MudBlazorLeptonXThemeBlazorOptions>(options =>
        {
            options.Layout = MudBlazorLeptonXBlazorLayouts.SideMenu;
        });
    }


    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
        });

        Configure<AbpAccountLinkUserOptions>(options =>
        {
            options.LoginUrl = configuration["AuthServer:Authority"];
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            // Blazor Web App
            options.Parameters.InteractiveAuto = true;

            // MVC UI
            options.StyleBundles.Configure(
                LeptonXThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );

            options.ScriptBundles.Configure(
                LeptonXThemeBundles.Scripts.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-scripts.js");
                }
            );

            // Blazor UI
            options.StyleBundles.Configure(
                BlazorMudBlazorLeptonXThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });

        Configure<AbpBundlingOptions>(options =>
        {
            var globalStyles = options.StyleBundles.Get(BlazorWebAssemblyMudBlazorStandardBundles.Styles.Global);
            globalStyles.AddContributors(typeof(hanhchinhsoStyleBundleContributor));

            var globalScripts = options.ScriptBundles.Get(BlazorWebAssemblyMudBlazorStandardBundles.Scripts.Global);
            globalScripts.AddContributors(typeof(hanhchinhsoScriptBundleContributor));

            options.Parameters["LeptonXTheme.Layout"] = "side-menu"; // side-menu or top-menu
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies", options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(365);
                options.IntrospectAccessToken();
            })
            .AddAbpOpenIdConnect("oidc", options =>
            {
                options.Authority = configuration["AuthServer:Authority"];
                options.RequireHttpsMetadata = configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata");;
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;

                options.ClientId = configuration["AuthServer:ClientId"];
                options.ClientSecret = configuration["AuthServer:ClientSecret"];

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.Scope.Add("roles");
                options.Scope.Add("email");
                options.Scope.Add("phone");
                options.Scope.Add("AuthServer");
                options.Scope.Add("IdentityService");
                options.Scope.Add("AdministrationService");
                options.Scope.Add("AuditLoggingService");
                options.Scope.Add("GdprService");
                options.Scope.Add("LanguageService");
                options.Scope.Add("OrganizationService");
                options.Scope.Add("DocumentService");
                options.Scope.Add("AIManagementService");
            });

            if (configuration.GetValue<bool>("AuthServer:IsOnK8s"))
            {
                context.Services.Configure<OpenIdConnectOptions>("oidc", options =>
                {
                    options.TokenValidationParameters.ValidIssuers = new[]
                    {
                        configuration["AuthServer:MetaAddress"]!.EnsureEndsWith('/'),
                        configuration["AuthServer:Authority"]!.EnsureEndsWith('/')
                    };

                    options.MetadataAddress = configuration["AuthServer:MetaAddress"]!.EnsureEndsWith('/') +
                                            ".well-known/openid-configuration";

                    var previousOnRedirectToIdentityProvider = options.Events.OnRedirectToIdentityProvider;
                    options.Events.OnRedirectToIdentityProvider = async ctx =>
                    {
                        // Intercept the redirection so the browser navigates to the right URL in your host
                        ctx.ProtocolMessage.IssuerAddress = configuration["AuthServer:Authority"]!.EnsureEndsWith('/') + "connect/authorize";

                        if (previousOnRedirectToIdentityProvider != null)
                        {
                            await previousOnRedirectToIdentityProvider(ctx);
                        }
                    };
                    var previousOnRedirectToIdentityProviderForSignOut = options.Events.OnRedirectToIdentityProviderForSignOut;
                    options.Events.OnRedirectToIdentityProviderForSignOut = async ctx =>
                    {
                        // Intercept the redirection for signout so the browser navigates to the right URL in your host
                        ctx.ProtocolMessage.IssuerAddress = configuration["AuthServer:Authority"]!.EnsureEndsWith('/') + "connect/endsession";

                        if (previousOnRedirectToIdentityProviderForSignOut != null)
                        {
                            await previousOnRedirectToIdentityProviderForSignOut(ctx);
                        }
                    };
                });

            }

        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });

        context.Services.Configure<AbpAntiForgeryOptions>(options =>
        {
            options.TokenCookie.SameSite = SameSiteMode.Unspecified;
        });
    }

    private void ConfigureImpersonation(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.Configure<AbpIdentityProBlazorOptions>(options =>
        {
            options.EnableUserImpersonation = true;
        });
    }

    private void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "hanhchinhso API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }

    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options =>
        {
            options.KeyPrefix = "hanhchinhso:";
        });
    }

    private void ConfigureDataProtection(
        ServiceConfigurationContext context,
        IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment)
    {
        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("hanhchinhso");
        if (!hostingEnvironment.IsDevelopment())
        {
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "hanhchinhso-Protection-Keys");
        }
    }

    private void ConfigureDistributedLocking(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }

    private void ConfigureBlazorise(ServiceConfigurationContext context)
    {
        // MudBlazor services are registered by AbpMudBlazorUIModule.
    }

    private void ConfigureMenu(IConfiguration configuration)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new hanhchinhsoMenuContributor(configuration));
        });
    }

    private void ConfigureRouter()
    {
        Configure<AbpRouterOptions>(options =>
        {
            options.AppAssembly = typeof(hanhchinhsoBlazorModule).Assembly;
            options.AdditionalAssemblies.Add(typeof(hanhchinhsoBlazorClientModule).Assembly);
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (!env.IsDevelopment())
        {
            app.Use((ctx, next) =>
            {
                /* This application should act like it is always called as HTTPS.
                 * Because it will work in a HTTPS url in production,
                 * but the HTTPS is stripped out in Ingress controller.
                 */
                ctx.Request.Scheme = "https";
                return next();
            });
        }

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.UseRouting();
        var configuration = context.GetConfiguration();
        if (Convert.ToBoolean(configuration["AuthServer:IsOnK8s"]))
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Value != null &&
                    context.Request.Path.Value.StartsWith("/appsettings", StringComparison.OrdinalIgnoreCase) &&
                    context.Request.Path.Value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    // Set endpoint to null so the static files middleware will handle the request.
                    context.SetEndpoint(null);
                }
                await next(context);
            });

            app.UseStaticFilesForPatterns("appsettings*.json");
        }
        app.MapAbpStaticAssets();
        app.UseAbpSecurityHeaders();
        app.UseAuthentication();
        app.UseMultiTenancy();
        app.UseDynamicClaims();
        app.UseAntiforgery();
        app.UseAuthorization();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints(builder =>
        {
            builder.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(builder.ServiceProvider.GetRequiredService<IOptions<AbpRouterOptions>>().Value.AdditionalAssemblies.ToArray());
        });
    }
}

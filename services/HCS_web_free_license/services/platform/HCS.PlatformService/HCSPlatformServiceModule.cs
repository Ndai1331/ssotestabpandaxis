using HCS.EntityFrameworkCore;
using HCS.PlatformService.Filters;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;

namespace HCS.PlatformService;

[DependsOn(
    typeof(HCSApplicationModule),
    typeof(HCSEntityFrameworkCoreModule),
    typeof(HCSHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule))]
public sealed class HCSPlatformServiceModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var authority = configuration["AuthServer:Authority"]
            ?? throw new InvalidOperationException("AuthServer:Authority is required.");

        PreConfigure<OpenIddictBuilder>(builder => builder.AddValidation(options =>
        {
            options.SetIssuer(new Uri(authority));
            options.AddAudiences("HCS");
            options.UseSystemNetHttp(http =>
            {
                if (configuration.GetValue("AuthServer:AllowUntrustedBackchannelCertificate", false))
                {
                    // Local Docker reaches the public issuer through Caddy's development
                    // certificate. Production retains the default certificate validation.
                    http.ConfigureHttpClientHandler(handler =>
                        handler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
                }
            });
            options.UseAspNetCore();
        }));
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // OpenIddict preserves JWT claim names. Align ABP's role/user providers with
        // the access-token contract so existing role-based permission grants apply.
        AbpClaimTypes.UserId = "sub";
        AbpClaimTypes.Role = "role";

        context.Services.Configure<MvcOptions>(options =>
            options.Filters.Add<DefaultApplicationLocalizationCultureFilter>());
        context.Services.AddAuthentication(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
            options.IsDynamicClaimsEnabled = true);
        Configure<AbpAntiForgeryOptions>(options =>
        {
            // Browser-originated API writes are validated at the BFF boundary before
            // the gateway attaches the bearer token. Platform only hosts bearer APIs;
            // it does not share the BFF antiforgery cookie/token pair.
            options.AutoValidateFilter = type => !typeof(ControllerBase).IsAssignableFrom(type);
        });

        context.Services.AddHealthChecks();
        context.Services.AddAbpSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "HCS Platform API", Version = "v1" });
            options.CustomSchemaIds(type => type.FullName);
        });

        Configure<AbpAspNetCoreMvcOptions>(options =>
            options.ConventionalControllers.Create(typeof(HCSApplicationModule).Assembly));
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var environment = context.GetEnvironment();

        if (environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseAbpSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "HCS Platform API"));
        }

        app.UseCorrelationId();
        app.UseRouting();
        app.UseAuthentication();
        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions());
            endpoints.MapControllers();
        });
    }
}

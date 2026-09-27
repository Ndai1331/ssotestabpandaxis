using HCS.BlobStorage;
using HCS.EntityFrameworkCore;
using HCS.Permissions;
using HCS.PlatformService.Filters;
using HCS.PlatformService.Storage;
using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Volo.Abp;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Minio;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.EventBus.RabbitMq;

namespace HCS.PlatformService;

[DependsOn(
    typeof(HCSApplicationModule),
    typeof(HCSEntityFrameworkCoreModule),
    typeof(HcsBlobStorageModule),
    typeof(HCSHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpBlobStoringMinioModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AbpOpenIddictAspNetCoreModule),
    typeof(AbpSwashbuckleModule))]
public sealed class HCSPlatformServiceModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // OpenIddict preserves JWT claim names. Align ABP's role/user providers with
        // the access-token contract so existing role-based permission grants apply.
        HCS.HcsServiceJwtBearer.AlignAbpClaimTypes();

        context.Services.Configure<MvcOptions>(options =>
            options.Filters.Add<DefaultApplicationLocalizationCultureFilter>());
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => HCS.HcsServiceJwtBearer.Configure(options, context.Services.GetConfiguration()));
        context.Services.AddAuthorization(options =>
        {
            options.AddPolicy(HCSPermissions.Collaboration.Chat,
                policy => policy.RequireClaim("permission", HCSPermissions.Collaboration.Chat));
            options.AddPolicy(HCSPermissions.Collaboration.ChatRead, policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", HCSPermissions.Collaboration.Chat)
                || context.User.HasClaim("permission", HCSPermissions.WorkManagement.Dashboard)
                || HcsAdministrator.Is(context.User)));
            options.AddPolicy(HCSPermissions.Collaboration.Social,
                policy => policy.RequireClaim("permission", HCSPermissions.Collaboration.Social));
            options.AddPolicy(HCSPermissions.ServiceLogs.Default, policy => policy.RequireAssertion(context =>
                HcsAdministrator.Is(context.User)
                || context.User.HasClaim("permission", HCSPermissions.ServiceLogs.Default)));
        });
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
        context.Services.AddHostedService<RolePermissionSyncHostedService>();
        context.Services.AddAbpSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "HCS Platform API", Version = "v1" });
            options.CustomSchemaIds(type => type.FullName);
        });

        Configure<AbpAspNetCoreMvcOptions>(options =>
            options.ConventionalControllers.Create(typeof(HCSApplicationModule).Assembly));

        Configure<AbpExceptionHttpStatusCodeOptions>(options =>
            options.Map(HCSDomainErrorCodes.RoleAssignedToUsers, HttpStatusCode.Conflict));

        context.Services.AddHttpContextAccessor();

        var configuration = context.Services.GetConfiguration();
        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.Configure<AvatarBlobContainer>(container => container.UseHcsStorage(configuration));
            options.Containers.Configure<BrandingBlobContainer>(container => container.UseHcsStorage(configuration));
        });
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

using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Integration;
using HCS.WorkManagementService.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Minio;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Swashbuckle;
using System.Security.Claims;

namespace HCS.WorkManagementService;

[DependsOn(typeof(AbpAutofacModule), typeof(AbpAspNetCoreMvcModule), typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule), typeof(AbpBlobStoringMinioModule),
    typeof(AbpEventBusRabbitMqModule), typeof(AbpSwashbuckleModule),
    typeof(AbpOpenIddictAspNetCoreModule))]
public sealed class HcsWorkManagementServiceModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var authority = configuration["AuthServer:Authority"]
            ?? throw new InvalidOperationException("AuthServer:Authority is required.");

        PreConfigure<OpenIddictBuilder>(builder => builder.AddValidation(options =>
        {
            options.SetIssuer(new Uri(authority));
            options.AddAudiences(configuration["AuthServer:Audience"] ?? "HCS");
            options.UseSystemNetHttp(http =>
            {
                if (configuration.GetValue("AuthServer:AllowUntrustedBackchannelCertificate", false))
                    http.ConfigureHttpClientHandler(handler => handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
            });
            options.UseAspNetCore();
        }));
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
        context.Services.AddAuthorization(options =>
        {
            foreach (var permission in new[] { WorkPermissions.Projects, WorkPermissions.Tasks, WorkPermissions.Calendar, WorkPermissions.Events,
                         WorkPermissions.Surveys, WorkPermissions.SurveyManagement, WorkPermissions.Reports, WorkPermissions.Dashboard,
                         WorkPermissions.EmployeeRatings, WorkPermissions.EmployeeRatingsManagement, WorkPermissions.EmployeeRatingsDashboard })
            {
                if (permission == WorkPermissions.Events)
                {
                    options.AddPolicy(permission, policy => policy.RequireAssertion(context =>
                        IsAdministrator(context.User) || context.User.HasClaim("permission", permission)));
                }
                else
                {
                    options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
                }
            }
            options.AddPolicy(WorkPermissions.EmployeeRatingsRead, policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", WorkPermissions.EmployeeRatings)
                || context.User.HasClaim("permission", WorkPermissions.EmployeeRatingsManagement)
                || context.User.HasClaim("permission", WorkPermissions.EmployeeRatingsDashboard)
                || context.User.IsInRole("admin")));
        });
        context.Services.AddHttpContextAccessor();
        context.Services.AddHttpClient("HCS.Platform", client =>
        {
            var baseUrl = configuration["Services:Platform:BaseUrl"] ?? "https://localhost:44411/";
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (configuration.GetValue("AuthServer:AllowUntrustedBackchannelCertificate", false))
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });
        Configure<AbpAntiForgeryOptions>(BearerApiAntiforgery.DisableCookieValidation);
        context.Services.AddDbContext<WorkManagementDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString(WorkManagementDbContext.ConnectionStringName)));
        Configure<AbpBlobStoringOptions>(options => options.Containers.Configure<WorkAssetBlobContainer>(container =>
            container.UseMinio(minio =>
            {
                minio.EndPoint = configuration["Minio:EndPoint"] ?? "localhost:9000";
                minio.AccessKey = configuration["Minio:AccessKey"] ?? string.Empty;
                minio.SecretKey = configuration["Minio:SecretKey"] ?? string.Empty;
                minio.WithSSL = configuration.GetValue("Minio:WithSSL", false);
                minio.CreateBucketIfNotExists = configuration.GetValue("Minio:CreateBucketIfNotExists", true);
            })));
        context.Services.AddScoped<IInboxExecutor, EfInboxExecutor>();
        context.Services.AddScoped<OutboxDispatcher>();
        context.Services.AddHostedService<OutboxWorker>();
        context.Services.AddHealthChecks().AddDbContextCheck<WorkManagementDbContext>("hcs_work");
        context.Services.AddAbpSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "HCS Work Management API", Version = "v1" });
            options.CustomSchemaIds(type => type.FullName);
        });
    }

    private static bool IsAdministrator(ClaimsPrincipal user) =>
        user.IsInRole("admin") ||
        user.FindAll("role").Any(claim => string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase)) ||
        user.FindAll(ClaimTypes.Role).Any(claim => string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase));

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        if (context.GetEnvironment().IsDevelopment()) app.UseDeveloperExceptionPage();
        app.UseCorrelationId(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
        app.UseSwagger();
        app.UseAbpSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "HCS Work Management API"));
        app.UseAuditing(); app.UseUnitOfWork(); app.UseMiddleware<HttpAuditOutboxMiddleware>();
        app.UseConfiguredEndpoints(endpoints => endpoints.MapHealthChecks("/health"));
    }
}

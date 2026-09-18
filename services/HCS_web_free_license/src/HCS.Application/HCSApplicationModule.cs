using HCS.Logging;
using HCS.Settings;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Account;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;

namespace HCS;

[DependsOn(
    typeof(HCSDomainModule),
    typeof(HCSApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class HCSApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<SeqOptions>(configuration.GetSection("Seq"));
        context.Services.AddHttpClient(SeqServiceLogQuery.HttpClientName, (services, client) =>
        {
            var options = services.GetRequiredService<IOptions<SeqOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.ServerUrl) &&
                Uri.TryCreate(options.ServerUrl.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var uri))
            {
                client.BaseAddress = uri;
            }

            var timeoutSeconds = options.TimeoutSeconds <= 0 ? 15 : Math.Min(options.TimeoutSeconds, 60);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Seq-ApiKey", options.ApiKey.Trim());
            }

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        });
        context.Services.AddHttpClient(AuthenticationSettingsAppService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}


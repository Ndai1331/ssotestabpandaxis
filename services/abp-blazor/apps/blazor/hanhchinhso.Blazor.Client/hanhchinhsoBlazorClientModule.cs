using System;
using System.Net.Http;
using global::MudBlazor;
using Volo.Abp.MudBlazorUI;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using hanhchinhso.Blazor.Client.Navigation;
using Volo.Abp.AspNetCore.Components.Web;
using Volo.Abp.AspNetCore.Components.Web.Theming.MudBlazor.Routing;
using Volo.Abp.Autofac.WebAssembly;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Localization;
using Volo.Abp.UI.Navigation;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Components.WebAssembly.Theming.MudBlazor.Bundling;
using Volo.Abp.AspNetCore.Components.WebAssembly.MudBlazorLeptonXTheme;
using Volo.Abp.AspNetCore.Components.Web.MudBlazorLeptonXTheme;
using Volo.Abp.LeptonX.Shared;
using Volo.Abp.SettingManagement.Blazor.MudBlazor.WebAssembly;
using Volo.Abp.Account.Pro.Admin.Blazor.MudBlazor.WebAssembly;
using Volo.Abp.Account.Pro.Public.Blazor.MudBlazor.WebAssembly;
using Volo.Abp.Identity.Pro.Blazor.MudBlazor;
using Volo.Abp.Identity.Pro.Blazor.MudBlazor.WebAssembly;
using Volo.Abp.AuditLogging.Blazor.MudBlazor.WebAssembly;
using hanhchinhso.AuditLoggingService;
using Volo.Abp.Gdpr.Blazor.MudBlazor.Extensions;
using Volo.Abp.Gdpr.Blazor.MudBlazor.WebAssembly;
using hanhchinhso.GdprService;
using Volo.Abp.LanguageManagement.Blazor.MudBlazor.WebAssembly;
using hanhchinhso.LanguageService;
using hanhchinhso.LanguageService.Localization;
using Volo.Abp.OpenIddict.Pro.Blazor.MudBlazor.WebAssembly;
using OpenIddict.Abstractions;
using Volo.Abp.TextTemplateManagement.Blazor.MudBlazor.WebAssembly;
using Volo.AIManagement.Blazor.MudBlazor.WebAssembly;
using Volo.AIManagement.Client.Blazor.MudBlazor.WebAssembly;
using hanhchinhso.AIManagementService;
using hanhchinhso.IdentityService;
using hanhchinhso.AdministrationService;
using hanhchinhso.OrganizationService;
using hanhchinhso.DocumentService;
using Volo.Abp.Http.Client;

namespace hanhchinhso.Blazor.Client;

[DependsOn(
    typeof(AbpSettingManagementBlazorMudBlazorWebAssemblyModule),
    typeof(AbpGdprBlazorMudBlazorWebAssemblyModule),
    typeof(hanhchinhsoGdprServiceContractsModule),
    typeof(AbpAccountAdminBlazorMudBlazorWebAssemblyModule),
    typeof(AbpAccountPublicBlazorMudBlazorWebAssemblyModule),
    typeof(AbpIdentityProBlazorMudBlazorWebAssemblyModule),
    typeof(AIManagementBlazorMudBlazorWebAssemblyModule),
    typeof(AIManagementClientBlazorMudBlazorWebAssemblyModule),
    typeof(hanhchinhsoAIManagementServiceContractsModule),
    typeof(AbpOpenIddictProBlazorMudBlazorWebAssemblyModule),
    typeof(AbpAuditLoggingBlazorMudBlazorWebAssemblyModule),
    typeof(hanhchinhsoAuditLoggingServiceContractsModule),
    typeof(TextTemplateManagementBlazorMudBlazorWebAssemblyModule),
    typeof(LanguageManagementBlazorMudBlazorWebAssemblyModule),
    typeof(hanhchinhsoLanguageServiceContractsModule),
    typeof(hanhchinhsoOrganizationServiceContractsModule),
    typeof(hanhchinhsoDocumentServiceContractsModule),
    typeof(AbpHttpClientModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyMudBlazorLeptonXThemeModule),
    typeof(AbpAutofacWebAssemblyModule),
    typeof(hanhchinhsoIdentityServiceContractsModule),
    typeof(hanhchinhsoAdministrationServiceContractsModule)
)]
public class hanhchinhsoBlazorClientModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<AbpAspNetCoreComponentsWebOptions>(options =>
        {
            options.IsBlazorWebApp = true;
        });
    }
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var environment = context.Services.GetSingletonInstance<IWebAssemblyHostEnvironment>();
        var builder = context.Services.GetSingletonInstance<WebAssemblyHostBuilder>();
        var configuration = context.Services.GetConfiguration();

        ConfigureAuthentication(builder);
        ConfigureImpersonation(context);
        ConfigureHttpClient(context, environment);
        context.Services.AddHttpClientProxies(
            typeof(hanhchinhsoOrganizationServiceContractsModule).Assembly,
            "OrganizationService");
        context.Services.AddHttpClientProxies(
            typeof(hanhchinhsoDocumentServiceContractsModule).Assembly,
            "DocumentService");
        ConfigureBlazorise(context);
        ConfigureRouter(context);
        ConfigureMenu(context);
        ConfigureVirtualFileSystem();
        ConfigureCookieConsent(context);
        ConfigureTheme();
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

        Configure<MudBlazorLeptonXThemeBlazorOptions>(options =>
        {
            // When Layout is changed, the `options.Parameters["LeptonXTheme.Layout"]` in hanhchinhsoBlazorModule.cs should be updated accordingly.
            options.Layout = MudBlazorLeptonXBlazorLayouts.SideMenu;
        });
    }


    private void ConfigureRouter(ServiceConfigurationContext context)
    {
        Configure<AbpRouterOptions>(options =>
        {
            options.AppAssembly = typeof(hanhchinhsoBlazorClientModule).Assembly;
        });
    }

    private void ConfigureMenu(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new hanhchinhsoMenuContributor(context.Services.GetConfiguration()));
        });
    }

    private void ConfigureBlazorise(ServiceConfigurationContext context)
    {
        // MudBlazor services are registered by AbpMudBlazorUIModule.
    }

    private static void ConfigureAuthentication(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddBlazorWebAppServices();
    }

    private void ConfigureImpersonation(ServiceConfigurationContext context)
    {
        context.Services.Configure<AbpIdentityProBlazorOptions>(options =>
        {
            options.EnableUserImpersonation = true;
        });
    }
    
    private static void ConfigureHttpClient(ServiceConfigurationContext context, IWebAssemblyHostEnvironment environment)
    {
        context.Services.AddTransient(sp => new HttpClient
        {
            BaseAddress = new Uri(environment.BaseAddress)
        });
    }

    private void ConfigureVirtualFileSystem()
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<hanhchinhsoBlazorClientModule>();
        });
    }
}

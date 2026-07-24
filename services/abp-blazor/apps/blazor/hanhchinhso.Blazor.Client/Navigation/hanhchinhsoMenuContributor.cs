using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using hanhchinhso.AdministrationService.Permissions;
using Volo.Abp.Account.Localization;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.SettingManagement.Blazor.MudBlazor.Menus;
using Volo.Abp.Users;
using Volo.Abp.Localization;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Identity.Pro.Blazor.MudBlazor.Navigation;
using Volo.Abp.AuditLogging.Blazor.MudBlazor.Menus;
using hanhchinhso.LanguageService.Localization;
using Volo.Abp.LanguageManagement.Blazor.MudBlazor.Menus;
using Volo.Abp.TextTemplateManagement.Blazor.MudBlazor.Menus;
using Volo.Abp.OpenIddict.Pro.Blazor.MudBlazor.Menus;
using Volo.AIManagement.Blazor.MudBlazor.Navigation;
using hanhchinhso.OrganizationService.Permissions;

namespace hanhchinhso.Blazor.Client.Navigation;

public class hanhchinhsoMenuContributor : IMenuContributor
{
    private readonly IConfiguration _configuration;

    public hanhchinhsoMenuContributor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
        else if (context.Menu.Name == StandardMenus.User)
        {
            await ConfigureUserMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<LanguageServiceResource>();

        //Home
        context.Menu.AddItem(
            new ApplicationMenuItem( 
                hanhchinhsoMenus.Home,
                l["Menu:Home"],
                "/",
                icon: "fa fa-home",
                order: 0
            )
        );

        // Workflow (Elsa Studio) — opens standalone Studio in a new tab (Option A)
        var elsaStudioUrl = _configuration["ElsaStudio:Url"] ?? "http://localhost:44396";
        context.Menu.AddItem(
            new ApplicationMenuItem(
                "WorkflowService.Studio",
                l["Menu:Workflow"],
                elsaStudioUrl,
                icon: "fa fa-project-diagram",
                order: 1,
                target: "_blank"
            ).RequireAuthenticated()
        );

        context.Menu.AddItem(
            new ApplicationMenuItem(
                "HanhChinhSo.Organization.MasterData",
                "Danh mục tổ chức",
                "/organization/master-data",
                icon: "fa fa-sitemap",
                order: 2
            ).RequirePermissions(OrganizationServicePermissions.MasterData)
        );

        //HostDashboard
        context.Menu.AddItem(
            new ApplicationMenuItem(
                hanhchinhsoMenus.HostDashboard,
                l["Menu:Dashboard"],
                "/HostDashboard",
                icon: "fa fa-chart-line",
                order: 3
            ).RequirePermissions(AdministrationServicePermissions.Dashboard.Host)
        );
        
        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 5;


        //Administration->Identity
        administration.SetSubItemOrder(IdentityProMenus.GroupName, 2);

        //Administration->OpenId
        administration.SetSubItemOrder(OpenIddictProMenus.GroupName, 3);

        //Administration->Language Management
        administration.SetSubItemOrder(LanguageManagementMenus.GroupName, 4);
        //Administration->AI Management
        administration.SetSubItemOrder(AIManagementMenus.GroupName, 5);
        //Administration->Text Template Management
        administration.SetSubItemOrder(TextTemplateManagementMenus.GroupName, 6);

        //Administration->Audit Logs
        administration.SetSubItemOrder(AbpAuditLoggingMenus.GroupName, 7);

        //Administration->Settings
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 8);

        return Task.CompletedTask;
    }

    private async Task ConfigureUserMenuAsync(MenuConfigurationContext context)
    {
        var authServerUrl = _configuration["AuthServer:Authority"] ?? "";
        var accountResource = context.GetLocalizer<AccountResource>();

        context.Menu.AddItem(new ApplicationMenuItem("Account.Manage", accountResource["MyAccount"], $"{authServerUrl.EnsureEndsWith('/')}Account/Manage", icon: "fa fa-cog", order: 1000,  target: "_blank").RequireAuthenticated());
        context.Menu.AddItem(new ApplicationMenuItem("Account.SecurityLogs", accountResource["MySecurityLogs"], $"{authServerUrl.EnsureEndsWith('/')}Account/SecurityLogs", icon: "fa fa-user-shield", target: "_blank").RequireAuthenticated());
        context.Menu.AddItem(new ApplicationMenuItem("Account.Sessions", accountResource["Sessions"], url: $"{authServerUrl.EnsureEndsWith('/')}Account/Sessions", icon: "fa fa-clock", target: "_blank").RequireAuthenticated());
        
        if (!OperatingSystem.IsBrowser())
        {
            context.Menu.AddItem(new ApplicationMenuItem("Account.Logout", accountResource["Logout"], url: "/Account/Logout", icon: "fa fa-power-off", order: int.MaxValue - 1000).RequireAuthenticated());
        }

        await Task.CompletedTask;
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using abptestwithsso.AdministrationService.Permissions;
using Volo.Abp.Account.Localization;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.SettingManagement.Blazor.MudBlazor.Menus;
using Volo.Abp.Users;
using Volo.Abp.Localization;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Identity.Pro.Blazor.MudBlazor.Navigation;
using Volo.Abp.AuditLogging.Blazor.MudBlazor.Menus;
using abptestwithsso.LanguageService.Localization;
using Volo.Abp.LanguageManagement.Blazor.MudBlazor.Menus;
using Volo.Abp.TextTemplateManagement.Blazor.MudBlazor.Menus;
using Volo.Abp.OpenIddict.Pro.Blazor.MudBlazor.Menus;
using Volo.AIManagement.Blazor.MudBlazor.Navigation;

namespace abptestwithsso.Blazor.Client.Navigation;

public class abptestwithssoMenuContributor : IMenuContributor
{
    private readonly IConfiguration _configuration;

    public abptestwithssoMenuContributor(IConfiguration configuration)
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

    private static async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<LanguageServiceResource>();

        //Home
        context.Menu.AddItem(
            new ApplicationMenuItem( 
                abptestwithssoMenus.Home,
                l["Menu:Home"],
                "/",
                icon: "fa fa-home",
                order: 0
            )
        );

        //HostDashboard
        context.Menu.AddItem(
            new ApplicationMenuItem(
                abptestwithssoMenus.HostDashboard,
                l["Menu:Dashboard"],
                "/HostDashboard",
                icon: "fa fa-chart-line",
                order: 2
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

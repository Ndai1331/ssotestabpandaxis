using System.Threading.Tasks;
using HCS.Blazor.Client.Pages.Settings;
using HCS.Localization;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Volo.Abp.SettingManagement.Blazor;

namespace HCS.Blazor.Client.Settings;

public sealed class SystemBrandingSettingsPageContributor : ISettingComponentContributor
{
    public async Task ConfigureAsync(SettingComponentCreationContext context)
    {
        if (!await CheckPermissionsAsync(context))
        {
            return;
        }

        var localizer = context.ServiceProvider.GetRequiredService<IStringLocalizer<HCSResource>>();
        context.Groups.Add(new SettingComponentGroup(
            "HCS.Branding",
            localizer["Settings:BrandingGroup"],
            typeof(SystemBrandingSettingsGroup),
            order: 90));
    }

    public async Task<bool> CheckPermissionsAsync(SettingComponentCreationContext context)
    {
        var authenticationState = await context.ServiceProvider
            .GetRequiredService<AuthenticationStateProvider>()
            .GetAuthenticationStateAsync();

        return (await context.ServiceProvider
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(authenticationState.User, null, HCSPermissions.SystemBranding.Update))
            .Succeeded;
    }
}

using System.Threading.Tasks;
using HCS.Blazor.Client.Pages.Settings;
using HCS.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Volo.Abp.SettingManagement.Blazor;

namespace HCS.Blazor.Client.Settings;

public sealed class AuthenticationSettingsPageContributor : ISettingComponentContributor
{
    public async Task ConfigureAsync(SettingComponentCreationContext context)
    {
        if (!await CheckPermissionsAsync(context))
        {
            return;
        }

        var localizer = context.ServiceProvider.GetRequiredService<IStringLocalizer<HCSResource>>();
        context.Groups.Add(new SettingComponentGroup(
            "HCS.Authentication",
            localizer["Settings:SsoLoginGroup"],
            typeof(AuthenticationSettingsGroup),
            order: 100));
    }

    public async Task<bool> CheckPermissionsAsync(SettingComponentCreationContext context)
    {
        var authenticationState = await context.ServiceProvider
            .GetRequiredService<AuthenticationStateProvider>()
            .GetAuthenticationStateAsync();

        return authenticationState.User.IsInRole("admin");
    }
}

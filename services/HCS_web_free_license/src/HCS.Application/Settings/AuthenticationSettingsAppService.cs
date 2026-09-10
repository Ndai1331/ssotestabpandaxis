using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.SettingManagement;

namespace HCS.Settings;

[Authorize(Roles = "admin")]
public sealed class AuthenticationSettingsAppService(
    ISettingManager settingManager) : HCSAppService, IAuthenticationSettingsAppService
{
    public async Task<AuthenticationSettingsDto> GetAsync()
    {
        var configuredValue = await settingManager.GetOrNullGlobalAsync(HCSSettings.ShowSsoLoginButton);
        return new AuthenticationSettingsDto
        {
            // Keep the button enabled when no global override has been saved yet.
            ShowSsoLoginButton = !string.Equals(configuredValue, "false", StringComparison.OrdinalIgnoreCase)
        };
    }

    public Task UpdateAsync(UpdateAuthenticationSettingsDto input) =>
        settingManager.SetGlobalAsync(
            HCSSettings.ShowSsoLoginButton,
            input.ShowSsoLoginButton ? "true" : "false");
}

using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.SettingManagement;

namespace HCS.Settings;

[RemoteService(IsEnabled = false)]
[Authorize]
public class SystemFeatureSettingsAppService(
    ISettingManager settingManager) : HCSAppService, ISystemFeatureSettingsAppService
{
    public async Task<SystemFeatureSettingsDto> GetAsync()
    {
        var allowSigning = await settingManager.GetOrNullGlobalAsync(HCSSettings.AllowSigningFromDocuments);
        var enableReport = await settingManager.GetOrNullGlobalAsync(HCSSettings.LegacySigningReportEnabled);
        return new SystemFeatureSettingsDto
        {
            AllowSigningFromDocuments = HCSSettings.IsEnabledOrDefault(allowSigning),
            EnableProposalStatistics = HCSSettings.IsEnabledOrDefault(enableReport)
        };
    }

    [Authorize(HCSPermissions.SystemBranding.Update)]
    public Task UpdateGeneralAsync(UpdateGeneralSettingsDto input) =>
        settingManager.SetGlobalAsync(
            HCSSettings.AllowSigningFromDocuments,
            input.AllowSigningFromDocuments ? "true" : "false");
}

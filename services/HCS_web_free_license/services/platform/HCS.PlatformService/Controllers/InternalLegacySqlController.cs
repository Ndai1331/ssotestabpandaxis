using HCS.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.SettingManagement;

namespace HCS.PlatformService.Controllers;

[ApiController]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/internal/legacy-sql")]
public sealed class InternalLegacySqlController(ISettingManager settingManager) : ControllerBase
{
    [HttpGet("connection-string")]
    public async Task<IActionResult> GetConnectionString(CancellationToken cancellationToken)
    {
        var enabled = await settingManager.GetOrNullGlobalAsync(HCSSettings.LegacySigningReportEnabled);
        var value = await settingManager.GetOrNullGlobalAsync(HCSSettings.LegacySigningReportSqlServerConnectionString);
        return Ok(new
        {
            enabled = HCSSettings.IsEnabledOrDefault(enabled),
            connectionString = value
        });
    }
}

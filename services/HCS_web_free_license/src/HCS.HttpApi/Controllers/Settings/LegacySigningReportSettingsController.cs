using System.Threading.Tasks;
using HCS.Permissions;
using HCS.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.Controllers.Settings;

[Authorize(HCSPermissions.SystemBranding.Update)]
[Route("api/hcs/legacy-signing-report")]
public sealed class LegacySigningReportSettingsController : HCSController, ILegacySigningReportSettingsAppService
{
    private readonly ILegacySigningReportSettingsAppService _service;

    public LegacySigningReportSettingsController(ILegacySigningReportSettingsAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<LegacySigningReportSettingsDto> GetAsync() => _service.GetAsync();

    [HttpPut]
    public Task UpdateAsync(UpdateLegacySigningReportSettingsDto input) => _service.UpdateAsync(input);

    [HttpPost("test")]
    public Task<LegacySigningReportConnectionTestResultDto> TestAsync(UpdateLegacySigningReportSettingsDto input) =>
        _service.TestAsync(input);
}

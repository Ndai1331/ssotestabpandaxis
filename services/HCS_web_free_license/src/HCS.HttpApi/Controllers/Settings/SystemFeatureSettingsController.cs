using System.Threading.Tasks;
using HCS.Permissions;
using HCS.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.Controllers.Settings;

[Authorize]
[Route("api/hcs/system-features")]
public sealed class SystemFeatureSettingsController : HCSController, ISystemFeatureSettingsAppService
{
    private readonly ISystemFeatureSettingsAppService _service;

    public SystemFeatureSettingsController(ISystemFeatureSettingsAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<SystemFeatureSettingsDto> GetAsync() => _service.GetAsync();

    [HttpPut("general")]
    [Authorize(HCSPermissions.SystemBranding.Update)]
    public Task UpdateGeneralAsync(UpdateGeneralSettingsDto input) => _service.UpdateGeneralAsync(input);
}

using System.Threading.Tasks;
using HCS.Permissions;
using HCS.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.Controllers.Settings;

[Authorize(HCSPermissions.SystemBranding.Update)]
[Route("api/hcs/storage-settings")]
public sealed class StorageSettingsController : HCSController, IStorageSettingsAppService
{
    private readonly IStorageSettingsAppService _service;

    public StorageSettingsController(IStorageSettingsAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<StorageSettingsDto> GetAsync() => _service.GetAsync();

    [HttpPut]
    public Task UpdateAsync(UpdateStorageSettingsDto input) => _service.UpdateAsync(input);

    [HttpPost("test")]
    public Task<StorageConnectionTestResultDto> TestAsync(UpdateStorageSettingsDto input) =>
        _service.TestAsync(input);
}

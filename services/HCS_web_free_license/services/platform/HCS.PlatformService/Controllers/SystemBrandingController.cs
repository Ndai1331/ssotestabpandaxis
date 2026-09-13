using HCS.Branding;
using HCS.PlatformService.Branding;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.PlatformService.Controllers;

[ApiController, Route("api/hcs/system-branding")]
public sealed class SystemBrandingController(SystemBrandingAppService branding) : ControllerBase
{
    [HttpGet]
    [Authorize(HCSPermissions.SystemBranding.Update)]
    public Task<SystemBrandingDto> Get(CancellationToken cancellationToken) => branding.GetAsync();

    [HttpGet("public")]
    [AllowAnonymous]
    public Task<SystemBrandingDto> GetPublic(CancellationToken cancellationToken) => branding.GetPublicAsync();

    [HttpPut]
    [Authorize(HCSPermissions.SystemBranding.Update)]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public Task<SystemBrandingDto> Update(
        [FromForm] SystemBrandingUpdateRequest input,
        CancellationToken cancellationToken) => branding.UpdateAsync(input, cancellationToken);

    [HttpGet("assets/{slot}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsset(string slot, CancellationToken cancellationToken)
    {
        var asset = await branding.GetAssetAsync(slot, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        Response.Headers.ETag = $"\"{asset.Sha256}\"";
        return File(asset.Content, asset.ContentType, enableRangeProcessing: true);
    }
}

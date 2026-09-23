using HCS.Identity;
using HCS.PlatformService.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace HCS.PlatformService.Controllers;

[ApiController, Authorize, Route("api/identity")]
public sealed class ProfileAvatarController(UserAvatarAppService avatars) : ControllerBase
{
    [HttpGet("profile/avatar")]
    public Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        GetAvatarFileAsync(userId: null, cancellationToken);

    [HttpGet("users/{userId:guid}/avatar")]
    public Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken) =>
        GetAvatarFileAsync(userId, cancellationToken);

    [HttpPost("profile/avatar")]
    [RequestSizeLimit(UserAvatar.MaxSizeBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest("A non-empty image file is required.");
        }

        await using var stream = file.OpenReadStream();
        await avatars.UploadAsync(file.FileName, file.ContentType, stream, file.Length, cancellationToken);
        return NoContent();
    }

    [HttpDelete("profile/avatar")]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await avatars.DeleteAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> GetAvatarFileAsync(Guid? userId, CancellationToken cancellationToken)
    {
        try
        {
            var meta = await avatars.GetMetaAsync(userId, cancellationToken);
            var etag = new EntityTagHeaderValue($"\"{meta.LastModificationTime.Ticks:x}-{meta.Size:x}\"");
            var responseHeaders = Response.GetTypedHeaders();
            responseHeaders.CacheControl = new CacheControlHeaderValue
            {
                Private = true,
                MaxAge = TimeSpan.Zero,
                MustRevalidate = true
            };
            responseHeaders.ETag = etag;

            var ifNoneMatch = Request.GetTypedHeaders().IfNoneMatch;
            if (ifNoneMatch is { Count: > 0 }
                && ifNoneMatch.Any(candidate =>
                    candidate.Equals(EntityTagHeaderValue.Any)
                    || candidate.Compare(etag, useStrongComparison: true)))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            var content = await avatars.OpenBlobAsync(meta.BlobName, cancellationToken);
            return File(
                content,
                meta.ContentType,
                meta.FileName,
                lastModified: new DateTimeOffset(DateTime.SpecifyKind(meta.LastModificationTime, DateTimeKind.Utc)),
                entityTag: etag,
                enableRangeProcessing: true);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

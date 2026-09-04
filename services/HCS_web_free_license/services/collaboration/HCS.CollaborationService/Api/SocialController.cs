using HCS.CollaborationService.Application;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace HCS.CollaborationService.Api;

[ApiController, Authorize(Policy = CollaborationPermissions.Social), Route("api/social")]
public sealed class SocialController(
    SocialPostAppService posts,
    SocialCommentAppService comments,
    SocialMediaStore media) : AbpControllerBase
{
    [HttpGet("feed")]
    public Task<PagedSocialPostsDto> Feed([FromQuery] int skip = 0, [FromQuery] int take = 20,
        CancellationToken ct = default) => posts.GetFeedAsync(skip, take, ct);

    [HttpGet("profile/posts")]
    public Task<PagedSocialPostsDto> ProfilePosts([FromQuery] int skip = 0, [FromQuery] int take = 20,
        [FromQuery] SocialPostVisibility? visibility = null, CancellationToken ct = default) =>
        posts.GetProfilePostsAsync(skip, take, visibility, ct);

    [HttpPost("posts")]
    public Task<SocialPostDto> CreatePost(CreateSocialPostInput input, CancellationToken ct) => posts.CreateAsync(input, ct);

    [HttpGet("posts/{postId:guid}/comments")]
    public Task<IReadOnlyList<SocialCommentDto>> Comments(Guid postId, CancellationToken ct) => comments.GetAsync(postId, ct);

    [HttpPost("posts/{postId:guid}/comments")]
    public Task<SocialCommentDto> AddComment(Guid postId, CreateSocialCommentInput input, CancellationToken ct) => comments.CreateAsync(postId, input, ct);

    [HttpPost("uploads")]
    [RequestSizeLimit(26_214_400)]
    public async Task<UploadSocialMediaResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null) throw new BusinessException("Collaboration:InvalidSocialMedia");
        await using var stream = file.OpenReadStream();
        return await media.UploadAsync(file.FileName, file.ContentType, stream, file.Length, ct);
    }

    [HttpGet("media/{mediaId:guid}")]
    public async Task<IActionResult> Download(Guid mediaId, CancellationToken ct)
    {
        var file = await media.DownloadAsync(mediaId, ct);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("media/{mediaId:guid}")]
    public Task DeleteMedia(Guid mediaId, CancellationToken ct) => media.DeleteUnattachedAsync(mediaId, ct);
}

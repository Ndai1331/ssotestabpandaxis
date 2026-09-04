using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace HCS.CollaborationService.Application;

public class SocialPostAppService(
    CollaborationDbContext db,
    ICurrentUser currentUser,
    IGuidGenerator guidGenerator,
    IClock clock) : ApplicationService
{
    private Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException("Authenticated user required.");

    public async Task<PagedSocialPostsDto> GetFeedAsync(int skip = 0, int take = 20, CancellationToken ct = default) =>
        await GetPostsAsync(db.SocialPosts.AsNoTracking().Where(x => x.Visibility == SocialPostVisibility.Public), skip, take, ct);

    public async Task<PagedSocialPostsDto> GetProfilePostsAsync(int skip = 0, int take = 20, CancellationToken ct = default) =>
        await GetPostsAsync(db.SocialPosts.AsNoTracking().Where(x => x.AuthorUserId == UserId), skip, take, ct);

    public async Task<SocialPostDto> CreateAsync(CreateSocialPostInput input, CancellationToken ct = default)
    {
        var me = UserId;
        SocialPostRules.DemandValidVisibility(input.Visibility);
        var mediaIds = input.MediaIds.Distinct().ToArray();
        SocialPostRules.DemandContent(input.Text, mediaIds.Length);
        var media = await db.SocialPostMedia.Where(x => mediaIds.Contains(x.Id)).ToListAsync(ct);
        if (media.Count != mediaIds.Length || media.Any(x => x.UploadedByUserId != me || x.PostId.HasValue))
            throw new BusinessException("Collaboration:InvalidSocialMedia");

        var now = clock.Now.ToUniversalTime();
        var post = new SocialPost(guidGenerator.Create(), me, CurrentDisplayName(), input.Text, input.Visibility, now);
        foreach (var item in mediaIds.Select(id => media.Single(mediaItem => mediaItem.Id == id)))
        {
            item.AttachTo(post.Id);
            post.Media.Add(item);
        }

        db.SocialPosts.Add(post);
        await db.SaveChangesAsync(ct);
        return SocialMapping.Post(post, 0);
    }

    internal async Task<SocialPost> RequireVisibleAsync(Guid postId, CancellationToken ct = default)
    {
        var me = UserId;
        return await db.SocialPosts.Include(x => x.Media).SingleOrDefaultAsync(x => x.Id == postId &&
            (x.Visibility == SocialPostVisibility.Public || x.AuthorUserId == me), ct)
            ?? throw new AbpAuthorizationException("Social post is not visible to the current user.");
    }

    private async Task<PagedSocialPostsDto> GetPostsAsync(IQueryable<SocialPost> query, int skip, int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 50);
        skip = Math.Max(skip, 0);
        var total = await query.LongCountAsync(ct);
        var posts = await query.Include(x => x.Media)
            .OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(ct);
        var ids = posts.Select(x => x.Id).ToArray();
        var counts = await db.SocialPostComments.AsNoTracking().Where(x => ids.Contains(x.PostId))
            .GroupBy(x => x.PostId).Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return new(total, posts.Select(post => SocialMapping.Post(post,
            counts.GetValueOrDefault(post.Id))).ToArray());
    }

    private string CurrentDisplayName() => UserDisplayNames.FromPerson(
        currentUser.SurName, currentUser.Name, currentUser.UserName, currentUser.FindClaim("name")?.Value);
}

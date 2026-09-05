using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using HCS.CollaborationService.Storage;
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
    IClock clock,
    SocialLinkPreviewFetcher linkPreviewFetcher,
    SocialNotificationService socialNotifications,
    SocialMediaStore mediaStore) : ApplicationService
{
    private Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException("Authenticated user required.");

    public async Task<PagedSocialPostsDto> GetFeedAsync(int skip = 0, int take = 20,
        string? keyword = null, DateOnly? from = null, DateOnly? to = null, string? hashtag = null,
        Guid? postId = null, CancellationToken ct = default) =>
        await GetPostsAsync(ApplySearch(db.SocialPosts.AsNoTracking()
            .Where(x => x.Visibility == SocialPostVisibility.Public), keyword, from, to, hashtag, postId), skip, take, ct);

    public async Task<PagedSocialPostsDto> GetProfilePostsAsync(int skip = 0, int take = 20,
        SocialPostVisibility? visibility = null, string? keyword = null, DateOnly? from = null,
        DateOnly? to = null, string? hashtag = null, Guid? postId = null, CancellationToken ct = default) =>
        await GetPostsAsync(ApplySearch(db.SocialPosts.AsNoTracking().Where(x => x.AuthorUserId == UserId &&
            (!visibility.HasValue || x.Visibility == visibility.Value)), keyword, from, to, hashtag, postId), skip, take, ct);

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
        var linkUrl = SocialPostRules.ExtractFirstUrl(input.Text);
        if (linkUrl is not null)
        {
            var preview = await linkPreviewFetcher.FetchAsync(linkUrl, ct);
            post.SetLinkPreview(linkUrl, preview?.Title, preview?.Description, preview?.SiteName, preview?.ImageUrl);
        }
        foreach (var item in mediaIds.Select(id => media.Single(mediaItem => mediaItem.Id == id)))
        {
            item.AttachTo(post.Id);
            post.Media.Add(item);
        }

        db.SocialPosts.Add(post);
        await db.SaveChangesAsync(ct);
        return SocialMapping.Post(post, 0, SocialMapping.EmptyReactions(), 0);
    }

    public async Task<SocialPostDto> UpdateAsync(Guid postId, UpdateSocialPostInput input,
        CancellationToken ct = default)
    {
        var post = await RequireOwnedAsync(postId, ct);
        var mediaCount = await db.SocialPostMedia.CountAsync(x => x.PostId == postId, ct);
        SocialPostRules.DemandContent(input.Text, mediaCount);

        var previousLinkUrl = post.LinkUrl;
        post.UpdateContent(input.Text, input.Visibility);
        var linkUrl = SocialPostRules.ExtractFirstUrl(input.Text);
        if (!string.Equals(previousLinkUrl, linkUrl, StringComparison.Ordinal))
        {
            post.ClearLinkPreview();
            if (linkUrl is not null)
            {
                var preview = await linkPreviewFetcher.FetchAsync(linkUrl, ct);
                post.SetLinkPreview(linkUrl, preview?.Title, preview?.Description,
                    preview?.SiteName, preview?.ImageUrl);
            }
        }

        await db.SaveChangesAsync(ct);
        return await GetPostSnapshotAsync(post, UserId, ct);
    }

    public async Task DeleteAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await RequireOwnedAsync(postId, ct);
        var blobNames = await db.SocialPostMedia.AsNoTracking()
            .Where(x => x.PostId == postId)
            .Select(x => x.BlobName)
            .ToArrayAsync(ct);

        db.SocialPosts.Remove(post);
        await db.SaveChangesAsync(ct);
        await mediaStore.DeleteBlobsAsync(blobNames, ct);
    }

    public async Task<SocialReactionStateDto> ReactAsync(Guid postId, SetSocialReactionInput input,
        CancellationToken ct = default)
    {
        var post = await RequireVisibleAsync(postId, ct);
        SocialReactionRules.DemandValid(input.ReactionType);
        var me = UserId;
        await using (var transaction = await SocialReactionTransaction.BeginAsync(db, "post", postId, me, ct))
        {
            var reaction = await db.SocialPostReactions.SingleOrDefaultAsync(x => x.PostId == postId && x.UserId == me, ct);
            if (input.Remove)
            {
                if (reaction is not null)
                    db.SocialPostReactions.Remove(reaction);
            }
            else if (reaction is null)
            {
                db.SocialPostReactions.Add(new SocialPostReaction(guidGenerator.Create(), postId, me, input.ReactionType));
                if (post.AuthorUserId != me)
                    socialNotifications.AddPostReaction(postId, post.Visibility, post.AuthorUserId,
                        CurrentDisplayName(), clock.Now.ToUniversalTime());
            }
            else if (reaction.ReactionType != input.ReactionType)
                reaction.ChangeTo(input.ReactionType);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        socialNotifications.PublishAfterCommit();
        return new(await GetPostReactionSummaryAsync(postId, me, ct));
    }

    public async Task<SocialShareResultDto> ShareAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await RequireVisibleAsync(postId, ct);
        var me = UserId;
        var alreadyShared = await db.SocialPostShares.AnyAsync(x => x.PostId == postId && x.UserId == me, ct);
        if (!alreadyShared)
        {
            db.SocialPostShares.Add(new SocialPostShare(guidGenerator.Create(), postId, me));
            await db.SaveChangesAsync(ct);
        }

        var shareCount = await db.SocialPostShares.CountAsync(x => x.PostId == postId, ct);
        var shareUrl = post.Visibility == SocialPostVisibility.Internal
            ? $"/social/profile?visibility=internal&post={postId:D}"
            : $"/social?post={postId:D}";
        return new(postId, shareUrl, shareCount, alreadyShared);
    }

    internal async Task<SocialPost> RequireVisibleAsync(Guid postId, CancellationToken ct = default)
    {
        var me = UserId;
        return await db.SocialPosts.SingleOrDefaultAsync(x => x.Id == postId &&
            (x.Visibility == SocialPostVisibility.Public || x.AuthorUserId == me), ct)
            ?? throw new AbpAuthorizationException("Social post is not visible to the current user.");
    }

    private async Task<SocialPost> RequireOwnedAsync(Guid postId, CancellationToken ct)
    {
        var me = UserId;
        return await db.SocialPosts.SingleOrDefaultAsync(x => x.Id == postId && x.AuthorUserId == me, ct)
            ?? throw new AbpAuthorizationException("Only the post author can modify this post.");
    }

    private async Task<SocialPostDto> GetPostSnapshotAsync(SocialPost post, Guid userId,
        CancellationToken ct)
    {
        await db.Entry(post).Collection(x => x.Media).LoadAsync(ct);
        var commentCount = await db.SocialPostComments.CountAsync(x => x.PostId == post.Id, ct);
        var shareCount = await db.SocialPostShares.CountAsync(x => x.PostId == post.Id, ct);
        var reactions = await GetPostReactionSummaryAsync(post.Id, userId, ct);
        return SocialMapping.Post(post, commentCount, reactions, shareCount);
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
        var me = UserId;
        var counts = await db.SocialPostComments.AsNoTracking().Where(x => ids.Contains(x.PostId))
            .GroupBy(x => x.PostId).Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var reactionCounts = await db.SocialPostReactions.AsNoTracking().Where(x => ids.Contains(x.PostId))
            .GroupBy(x => new { x.PostId, x.ReactionType })
            .Select(x => new { x.Key.PostId, x.Key.ReactionType, Count = x.Count() })
            .ToListAsync(ct);
        var currentReactions = await db.SocialPostReactions.AsNoTracking()
            .Where(x => ids.Contains(x.PostId) && x.UserId == me)
            .ToDictionaryAsync(x => x.PostId, x => (SocialReactionType?)x.ReactionType, ct);
        var reactionSummaries = reactionCounts.GroupBy(x => x.PostId).ToDictionary(group => group.Key,
            group => SocialMapping.Reactions(
                group.OrderByDescending(x => x.Count).ThenBy(x => x.ReactionType)
                    .Select(x => new SocialReactionCountDto(x.ReactionType, x.Count)),
                group.Sum(x => x.Count), currentReactions.GetValueOrDefault(group.Key)));
        var shares = await db.SocialPostShares.AsNoTracking().Where(x => ids.Contains(x.PostId))
            .GroupBy(x => x.PostId).Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return new(total, posts.Select(post => SocialMapping.Post(post,
            counts.GetValueOrDefault(post.Id),
            reactionSummaries.GetValueOrDefault(post.Id) ?? SocialMapping.EmptyReactions(),
            shares.GetValueOrDefault(post.Id))).ToArray());
    }

    private async Task<SocialReactionSummaryDto> GetPostReactionSummaryAsync(Guid postId, Guid userId,
        CancellationToken ct)
    {
        var groupedCounts = await db.SocialPostReactions.AsNoTracking().Where(x => x.PostId == postId)
            .GroupBy(x => x.ReactionType)
            .Select(x => new { Type = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Type).ToListAsync(ct);
        var counts = groupedCounts.Select(x => new SocialReactionCountDto(x.Type, x.Count)).ToArray();
        var current = await db.SocialPostReactions.AsNoTracking()
            .Where(x => x.PostId == postId && x.UserId == userId)
            .Select(x => (SocialReactionType?)x.ReactionType).SingleOrDefaultAsync(ct);
        return SocialMapping.Reactions(counts, counts.Sum(x => x.Count), current);
    }

    private static IQueryable<SocialPost> ApplySearch(IQueryable<SocialPost> query, string? keyword,
        DateOnly? from, DateOnly? to, string? hashtag, Guid? postId)
    {
        if (postId.HasValue)
            query = query.Where(x => x.Id == postId.Value);

        var normalizedKeyword = keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            var pattern = $"%{EscapeLike(normalizedKeyword)}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Text, pattern, "\\") ||
                EF.Functions.ILike(x.AuthorName, pattern, "\\") ||
                (x.LinkUrl != null && EF.Functions.ILike(x.LinkUrl, pattern, "\\")) ||
                (x.LinkTitle != null && EF.Functions.ILike(x.LinkTitle, pattern, "\\")) ||
                (x.LinkDescription != null && EF.Functions.ILike(x.LinkDescription, pattern, "\\")));
        }

        if (from.HasValue)
        {
            var fromUtc = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.CreationTime >= fromUtc);
        }

        if (to.HasValue)
        {
            var toExclusiveUtc = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.CreationTime < toExclusiveUtc);
        }

        if (!string.IsNullOrWhiteSpace(hashtag))
        {
            var normalizedHashtag = SocialPostRules.NormalizeHashtag(hashtag);
            if (normalizedHashtag is null)
                return query.Where(_ => false);

            query = query.Where(x => EF.Functions.ILike(x.Hashtags,
                $"%|{EscapeLike(normalizedHashtag)}|%", "\\"));
        }

        return query;
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\")
        .Replace("%", "\\%").Replace("_", "\\_");

    private string CurrentDisplayName() => UserDisplayNames.FromPerson(
        currentUser.SurName, currentUser.Name, currentUser.UserName, currentUser.FindClaim("name")?.Value);
}

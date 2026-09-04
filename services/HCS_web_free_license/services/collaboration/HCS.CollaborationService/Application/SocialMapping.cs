using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Domain;

namespace HCS.CollaborationService.Application;

internal static class SocialMapping
{
    public static SocialPostDto Post(SocialPost post, int commentCount,
        SocialReactionSummaryDto reactions, int shareCount) =>
        new(post.Id, post.AuthorUserId, post.AuthorName, AvatarUrl(post.AuthorUserId), post.Text,
            post.Visibility, post.CreationTime,
            post.Media.OrderBy(media => media.CreationTime).Select(Media).ToArray(), commentCount,
            post.LinkUrl is null ? null : new SocialLinkPreviewDto(post.LinkUrl, post.LinkTitle,
                post.LinkDescription, post.LinkSiteName, post.LinkImageUrl), reactions, shareCount);

    public static SocialCommentDto Comment(SocialPostComment comment, SocialReactionSummaryDto reactions) =>
        new(comment.Id, comment.PostId, comment.AuthorUserId, comment.AuthorName,
            AvatarUrl(comment.AuthorUserId), comment.Text, comment.CreationTime, comment.ParentCommentId, reactions);

    public static SocialPostMediaDto Media(SocialPostMedia media) =>
        new(media.Id, media.FileName, media.ContentType, media.Size, media.Kind,
            $"/api/social/media/{media.Id:D}");

    public static string AvatarUrl(Guid userId) => $"/api/identity/users/{userId:D}/avatar";

    public static SocialReactionSummaryDto EmptyReactions() => new(0, [], null);

    public static SocialReactionSummaryDto Reactions(IEnumerable<SocialReactionCountDto> counts,
        int totalCount, SocialReactionType? currentUserReaction) =>
        new(totalCount, counts.ToArray(), currentUserReaction);

    public static SocialReactionSummaryDto Reactions<T>(IEnumerable<T> reactions, Guid currentUserId,
        Func<T, Guid> userId, Func<T, SocialReactionType> reactionType)
    {
        var items = reactions.ToArray();
        return new(items.Length,
            items.GroupBy(reactionType)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => new SocialReactionCountDto(group.Key, group.Count()))
                .ToArray(),
            items.FirstOrDefault(item => userId(item) == currentUserId) is { } current
                ? reactionType(current)
                : null);
    }
}

using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Domain;

namespace HCS.CollaborationService.Application;

internal static class SocialMapping
{
    public static SocialPostDto Post(SocialPost post, int commentCount) =>
        new(post.Id, post.AuthorUserId, post.AuthorName, AvatarUrl(post.AuthorUserId), post.Text,
            post.Visibility, post.CreationTime,
            post.Media.OrderBy(media => media.CreationTime).Select(Media).ToArray(), commentCount);

    public static SocialCommentDto Comment(SocialPostComment comment) =>
        new(comment.Id, comment.PostId, comment.AuthorUserId, comment.AuthorName,
            AvatarUrl(comment.AuthorUserId), comment.Text, comment.CreationTime, comment.ParentCommentId);

    public static SocialPostMediaDto Media(SocialPostMedia media) =>
        new(media.Id, media.FileName, media.ContentType, media.Size, media.Kind,
            $"/api/social/media/{media.Id:D}");

    public static string AvatarUrl(Guid userId) => $"/api/identity/users/{userId:D}/avatar";
}

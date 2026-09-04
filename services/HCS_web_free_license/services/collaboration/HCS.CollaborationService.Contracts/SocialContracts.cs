using System.ComponentModel.DataAnnotations;

namespace HCS.CollaborationService.Contracts;

public enum SocialPostVisibility
{
    Public = 0,
    Internal = 1
}

public enum SocialMediaKind
{
    Image = 0,
    Video = 1
}

public static partial class CollaborationPermissions
{
    public const string Social = "Collaboration.Social";
}

public sealed record SocialPostMediaDto(Guid Id, string FileName, string ContentType, long Size,
    SocialMediaKind Kind, string Url);

public sealed record SocialPostDto(Guid Id, Guid AuthorUserId, string AuthorName, string AvatarUrl,
    string Text, SocialPostVisibility Visibility, DateTime CreatedAt,
    IReadOnlyList<SocialPostMediaDto> Media, int CommentCount);

public sealed record SocialCommentDto(Guid Id, Guid PostId, Guid AuthorUserId, string AuthorName,
    string AvatarUrl, string Text, DateTime CreatedAt, Guid? ParentCommentId);

public sealed record PagedSocialPostsDto(long TotalCount, IReadOnlyList<SocialPostDto> Items);

public sealed class CreateSocialPostInput
{
    [StringLength(4000)] public string? Text { get; init; }
    public SocialPostVisibility Visibility { get; init; }
    public IReadOnlyCollection<Guid> MediaIds { get; init; } = [];
}

public sealed class CreateSocialCommentInput
{
    [Required, StringLength(2000)] public string Text { get; init; } = string.Empty;
    public Guid? ParentCommentId { get; init; }
}

public sealed record UploadSocialMediaResult(Guid Id, string FileName, string ContentType,
    long Size, SocialMediaKind Kind, string Url);

public sealed record AuthorizedSocialMediaDownload(string FileName, string ContentType, Stream Content);

public static class SocialPostRules
{
    public const int MaxMediaItems = 10;

    public static void DemandValidVisibility(SocialPostVisibility visibility)
    {
        if (!Enum.IsDefined(visibility))
            throw new Volo.Abp.BusinessException("Collaboration:InvalidSocialVisibility");
    }

    public static void DemandContent(string? text, int mediaCount)
    {
        if (mediaCount == 0 && string.IsNullOrWhiteSpace(text))
            throw new Volo.Abp.BusinessException("Collaboration:EmptySocialPost");
        if (mediaCount > MaxMediaItems)
            throw new Volo.Abp.BusinessException("Collaboration:TooManySocialMediaItems");
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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

public enum SocialReactionType
{
    Like = 0,
    Love = 1,
    Haha = 2,
    Wow = 3,
    Sad = 4,
    Angry = 5
}

public static partial class CollaborationPermissions
{
    public const string Social = "Collaboration.Social";
}

public sealed record SocialPostMediaDto(Guid Id, string FileName, string ContentType, long Size,
    SocialMediaKind Kind, string Url);

public sealed record SocialLinkPreviewDto(string Url, string? Title, string? Description,
    string? SiteName, string? ImageUrl);

public sealed record SocialReactionCountDto(SocialReactionType Type, int Count);

public sealed record SocialReactionSummaryDto(int TotalCount,
    IReadOnlyList<SocialReactionCountDto> Counts, SocialReactionType? CurrentUserReaction);

public sealed record SocialPostDto(Guid Id, Guid AuthorUserId, string AuthorName, string AvatarUrl,
    string Text, SocialPostVisibility Visibility, DateTime CreatedAt,
    IReadOnlyList<SocialPostMediaDto> Media, int CommentCount, SocialLinkPreviewDto? LinkPreview,
    SocialReactionSummaryDto Reactions, int ShareCount);

public sealed record SocialCommentDto(Guid Id, Guid PostId, Guid AuthorUserId, string AuthorName,
    string AvatarUrl, string Text, DateTime CreatedAt, Guid? ParentCommentId,
    SocialReactionSummaryDto Reactions);

public sealed record SocialReactionStateDto(SocialReactionSummaryDto Reactions);

public sealed record SocialShareResultDto(Guid PostId, string ShareUrl, int ShareCount, bool AlreadyShared);

public sealed record PagedSocialPostsDto(long TotalCount, IReadOnlyList<SocialPostDto> Items);

public sealed class CreateSocialPostInput
{
    [StringLength(4000)] public string? Text { get; init; }
    public SocialPostVisibility Visibility { get; init; }
    public IReadOnlyCollection<Guid> MediaIds { get; init; } = [];
}

public sealed class UpdateSocialPostInput
{
    [StringLength(4000)] public string? Text { get; init; }
    public SocialPostVisibility Visibility { get; init; }
}

public sealed class CreateSocialCommentInput
{
    [Required, StringLength(2000)] public string Text { get; init; } = string.Empty;
    public Guid? ParentCommentId { get; init; }
}

public sealed class SetSocialReactionInput
{
    public SocialReactionType ReactionType { get; init; }
    public bool Remove { get; init; }
}

public sealed record UploadSocialMediaResult(Guid Id, string FileName, string ContentType,
    long Size, SocialMediaKind Kind, string Url);

public sealed record AuthorizedSocialMediaDownload(string FileName, string ContentType, Stream Content);

public static class SocialPostRules
{
    public const int MaxMediaItems = 10;

    private static readonly Regex UrlRegex = new(@"https?://[^\s<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex HashtagRegex = new(@"(?<!\w)#(?<tag>[\p{L}\p{N}_-]{1,64})", RegexOptions.CultureInvariant);
    private static readonly Regex HashtagValueRegex = new(@"\A[\p{L}\p{N}_-]{1,64}\z", RegexOptions.CultureInvariant);

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

    public static string? ExtractFirstUrl(string? text)
    {
        var match = UrlRegex.Match(text ?? string.Empty);
        if (!match.Success)
            return null;

        var value = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}');
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    public static IReadOnlyList<string> ExtractHashtags(string? text) => HashtagRegex.Matches(text ?? string.Empty)
        .Select(match => match.Groups["tag"].Value.ToLowerInvariant())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(100)
        .ToArray();

    public static string BuildHashtagIndex(string? text) =>
        string.Concat(ExtractHashtags(text).Select(tag => $"|{tag}|"));

    public static string? NormalizeHashtag(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().TrimStart('#').ToLowerInvariant();
        return HashtagValueRegex.IsMatch(normalized) ? normalized : null;
    }
}

public static class SocialReactionRules
{
    public static void DemandValid(SocialReactionType reactionType)
    {
        if (!Enum.IsDefined(reactionType))
            throw new Volo.Abp.BusinessException("Collaboration:InvalidSocialReaction");
    }
}

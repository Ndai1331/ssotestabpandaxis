using HCS.CollaborationService.Contracts;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace HCS.CollaborationService.Domain;

public sealed class SocialPost : CreationAuditedAggregateRoot<Guid>
{
    public Guid AuthorUserId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public SocialPostVisibility Visibility { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string Hashtags { get; private set; } = string.Empty;
    public string? LinkUrl { get; private set; }
    public string? LinkTitle { get; private set; }
    public string? LinkDescription { get; private set; }
    public string? LinkSiteName { get; private set; }
    public string? LinkImageUrl { get; private set; }
    public ICollection<SocialPostMedia> Media { get; private set; } = [];
    public ICollection<SocialPostComment> Comments { get; private set; } = [];
    public ICollection<SocialPostReaction> Reactions { get; private set; } = [];
    public ICollection<SocialPostShare> Shares { get; private set; } = [];

    private SocialPost() { }

    public SocialPost(Guid id, Guid authorUserId, string authorName, string? text,
        SocialPostVisibility visibility, DateTime? creationTimeUtc = null) : base(id)
    {
        AuthorUserId = authorUserId;
        AuthorName = Check.NotNullOrWhiteSpace(authorName, nameof(authorName), 256);
        Text = Check.Length(text?.Trim() ?? string.Empty, nameof(text), 4000) ?? string.Empty;
        Hashtags = Check.Length(SocialPostRules.BuildHashtagIndex(Text), nameof(Hashtags), 8192) ?? string.Empty;
        SocialPostRules.DemandValidVisibility(visibility);
        Visibility = visibility;
        CreationTime = creationTimeUtc ?? DateTime.UtcNow;
    }

    public void SetLinkPreview(string url, string? title, string? description, string? siteName, string? imageUrl)
    {
        LinkUrl = Check.Length(url, nameof(url), 2048);
        LinkTitle = Check.Length(title?.Trim(), nameof(title), 512);
        LinkDescription = Check.Length(description?.Trim(), nameof(description), 2000);
        LinkSiteName = Check.Length(siteName?.Trim(), nameof(siteName), 256);
        LinkImageUrl = Check.Length(imageUrl, nameof(imageUrl), 2048);
    }

    public void UpdateContent(string? text, SocialPostVisibility visibility)
    {
        SocialPostRules.DemandValidVisibility(visibility);
        Text = Check.Length(text?.Trim() ?? string.Empty, nameof(text), 4000) ?? string.Empty;
        Hashtags = Check.Length(SocialPostRules.BuildHashtagIndex(Text), nameof(Hashtags), 8192) ?? string.Empty;
        Visibility = visibility;
    }

    public void ClearLinkPreview()
    {
        LinkUrl = null;
        LinkTitle = null;
        LinkDescription = null;
        LinkSiteName = null;
        LinkImageUrl = null;
    }
}

public sealed class SocialPostMedia : CreationAuditedEntity<Guid>
{
    public Guid? PostId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string BlobName { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public SocialMediaKind Kind { get; private set; }

    private SocialPostMedia() { }

    public SocialPostMedia(Guid id, Guid userId, string blobName, string fileName,
        string contentType, long size, SocialMediaKind kind) : base(id)
    {
        UploadedByUserId = userId;
        BlobName = Check.NotNullOrWhiteSpace(blobName, nameof(blobName), 512);
        FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName), 256);
        ContentType = Check.NotNullOrWhiteSpace(contentType, nameof(contentType), 128);
        Size = size;
        Kind = kind;
    }

    public void AttachTo(Guid postId)
    {
        if (PostId.HasValue)
            throw new BusinessException("Collaboration:SocialMediaAlreadyUsed");
        PostId = postId;
    }
}

public sealed class SocialPostComment : CreationAuditedEntity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public Guid? ParentCommentId { get; private set; }
    public ICollection<SocialCommentReaction> Reactions { get; private set; } = [];

    private SocialPostComment() { }

    public SocialPostComment(Guid id, Guid postId, Guid authorUserId, string authorName,
        string text, Guid? parentCommentId = null, DateTime? creationTimeUtc = null) : base(id)
    {
        PostId = postId;
        AuthorUserId = authorUserId;
        AuthorName = Check.NotNullOrWhiteSpace(authorName, nameof(authorName), 256);
        Text = Check.NotNullOrWhiteSpace(text, nameof(text), 2000);
        ParentCommentId = parentCommentId;
        CreationTime = creationTimeUtc ?? DateTime.UtcNow;
    }
}

public sealed class SocialPostReaction : CreationAuditedEntity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }
    public SocialReactionType ReactionType { get; private set; }

    private SocialPostReaction() { }

    public SocialPostReaction(Guid id, Guid postId, Guid userId, SocialReactionType reactionType) : base(id)
    {
        SocialReactionRules.DemandValid(reactionType);
        PostId = postId;
        UserId = userId;
        ReactionType = reactionType;
        CreationTime = DateTime.UtcNow;
    }

    public void ChangeTo(SocialReactionType reactionType)
    {
        SocialReactionRules.DemandValid(reactionType);
        ReactionType = reactionType;
    }
}

public sealed class SocialCommentReaction : CreationAuditedEntity<Guid>
{
    public Guid CommentId { get; private set; }
    public Guid UserId { get; private set; }
    public SocialReactionType ReactionType { get; private set; }

    private SocialCommentReaction() { }

    public SocialCommentReaction(Guid id, Guid commentId, Guid userId, SocialReactionType reactionType) : base(id)
    {
        SocialReactionRules.DemandValid(reactionType);
        CommentId = commentId;
        UserId = userId;
        ReactionType = reactionType;
        CreationTime = DateTime.UtcNow;
    }

    public void ChangeTo(SocialReactionType reactionType)
    {
        SocialReactionRules.DemandValid(reactionType);
        ReactionType = reactionType;
    }
}

public sealed class SocialPostShare : CreationAuditedEntity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }

    private SocialPostShare() { }

    public SocialPostShare(Guid id, Guid postId, Guid userId) : base(id)
    {
        PostId = postId;
        UserId = userId;
        CreationTime = DateTime.UtcNow;
    }
}

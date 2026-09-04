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
    public ICollection<SocialPostMedia> Media { get; private set; } = [];
    public ICollection<SocialPostComment> Comments { get; private set; } = [];

    private SocialPost() { }

    public SocialPost(Guid id, Guid authorUserId, string authorName, string? text,
        SocialPostVisibility visibility, DateTime? creationTimeUtc = null) : base(id)
    {
        AuthorUserId = authorUserId;
        AuthorName = Check.NotNullOrWhiteSpace(authorName, nameof(authorName), 256);
        Text = Check.Length(text?.Trim() ?? string.Empty, nameof(text), 4000) ?? string.Empty;
        SocialPostRules.DemandValidVisibility(visibility);
        Visibility = visibility;
        CreationTime = creationTimeUtc ?? DateTime.UtcNow;
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

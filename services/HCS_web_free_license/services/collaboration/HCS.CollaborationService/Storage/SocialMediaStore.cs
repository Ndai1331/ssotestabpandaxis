using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Users;

namespace HCS.CollaborationService.Storage;

[BlobContainerName("hcs-social")]
public sealed class SocialMediaContainer;

public sealed class SocialMediaStore(
    IBlobContainer<SocialMediaContainer> container,
    CollaborationDbContext db,
    ICurrentUser currentUser,
    IGuidGenerator guidGenerator,
    IConfiguration configuration,
    ILogger<SocialMediaStore> logger) : ITransientDependency
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "video/mp4", "video/webm"
    };
    private static readonly HashSet<string> AllowedCommentAttachmentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "text/plain", "application/pdf",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    public async Task<UploadSocialMediaResult> UploadAsync(
        string fileName, string contentType, Stream content, long size, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var maxBytes = configuration.GetValue<long?>("SocialPolicy:MaxBytes") ?? 25 * 1024 * 1024;
        if (size <= 0 || size > maxBytes)
            throw new BusinessException("Collaboration:InvalidSocialMediaSize");
        if (!AllowedTypes.Contains(contentType) || contentType.Length > 128)
            throw new BusinessException("Collaboration:InvalidSocialMediaType");
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 256)
            throw new BusinessException("Collaboration:InvalidFileName");

        var id = guidGenerator.Create();
        var blobName = $"posts/{userId:N}/{id:N}";
        await using var buffer = await AttachmentContent.BufferAsync(content, size, ct);
        if (buffer.Length != size)
            throw new BusinessException("Collaboration:InvalidSocialMediaSize");
        await container.SaveAsync(blobName, buffer, overrideExisting: false, cancellationToken: ct);
        var kind = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            ? SocialMediaKind.Video : SocialMediaKind.Image;
        var media = new SocialPostMedia(id, userId, blobName, safeName, contentType, size, kind);
        db.SocialPostMedia.Add(media);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await container.DeleteAsync(blobName, ct);
            throw;
        }

        return new(id, safeName, contentType, size, kind, $"/api/social/media/{id:D}");
    }

    public async Task<UploadSocialCommentAttachmentResult> UploadCommentAttachmentAsync(
        string fileName, string contentType, Stream content, long size, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var maxBytes = configuration.GetValue<long?>("SocialPolicy:MaxBytes") ?? 25 * 1024 * 1024;
        if (size <= 0 || size > maxBytes)
            throw new BusinessException("Collaboration:InvalidSocialCommentAttachmentSize");
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 256)
            throw new BusinessException("Collaboration:InvalidFileName");
        var normalizedContentType = NormalizeContentType(contentType, safeName);
        if (normalizedContentType is null || !AllowedCommentAttachmentTypes.Contains(normalizedContentType))
            throw new BusinessException("Collaboration:InvalidSocialCommentAttachmentType");

        var id = guidGenerator.Create();
        var blobName = $"comments/{userId:N}/{id:N}";
        await using var buffer = await AttachmentContent.BufferAsync(content, size, ct);
        if (buffer.Length != size)
            throw new BusinessException("Collaboration:InvalidSocialCommentAttachmentSize");
        await container.SaveAsync(blobName, buffer, overrideExisting: false, cancellationToken: ct);
        var attachment = new SocialCommentAttachment(id, userId, blobName, safeName,
            normalizedContentType, size);
        db.SocialCommentAttachments.Add(attachment);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await container.DeleteAsync(blobName, ct);
            throw;
        }

        return new(id, safeName, normalizedContentType, size, $"/api/social/comment-media/{id:D}");
    }

    public async Task<AuthorizedSocialMediaDownload> DownloadAsync(Guid mediaId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var media = await db.SocialPostMedia.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mediaId, ct)
            ?? throw new BusinessException("Collaboration:SocialMediaNotFound");
        if (media.PostId is null)
        {
            if (media.UploadedByUserId != userId) throw new AbpAuthorizationException();
        }
        else if (!await db.SocialPosts.AnyAsync(x => x.Id == media.PostId &&
            (x.Visibility == SocialPostVisibility.Public || x.AuthorUserId == userId), ct))
        {
            throw new AbpAuthorizationException();
        }

        return new(media.FileName, media.ContentType, await container.GetAsync(media.BlobName, ct));
    }

    public async Task DeleteUnattachedAsync(Guid mediaId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var media = await db.SocialPostMedia.SingleOrDefaultAsync(x => x.Id == mediaId, ct)
            ?? throw new BusinessException("Collaboration:SocialMediaNotFound");
        if (media.UploadedByUserId != userId || media.PostId.HasValue)
            throw new AbpAuthorizationException();
        db.SocialPostMedia.Remove(media);
        await db.SaveChangesAsync(ct);
        await container.DeleteAsync(media.BlobName, ct);
    }

    public async Task<AuthorizedSocialMediaDownload> DownloadCommentAttachmentAsync(
        Guid attachmentId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var attachment = await db.SocialCommentAttachments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == attachmentId, ct)
            ?? throw new BusinessException("Collaboration:SocialCommentAttachmentNotFound");
        if (attachment.CommentId is null)
        {
            if (attachment.UploadedByUserId != userId)
                throw new AbpAuthorizationException();
        }
        else if (!await (from comment in db.SocialPostComments.AsNoTracking()
                         join post in db.SocialPosts.AsNoTracking() on comment.PostId equals post.Id
                         where comment.Id == attachment.CommentId.Value
                         && (post.Visibility == SocialPostVisibility.Public || post.AuthorUserId == userId)
                         select comment.Id).AnyAsync(ct))
        {
            throw new AbpAuthorizationException();
        }

        return new(attachment.FileName, attachment.ContentType,
            await container.GetAsync(attachment.BlobName, ct));
    }

    public async Task DeleteUnattachedCommentAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var attachment = await db.SocialCommentAttachments
            .SingleOrDefaultAsync(x => x.Id == attachmentId, ct)
            ?? throw new BusinessException("Collaboration:SocialCommentAttachmentNotFound");
        if (attachment.UploadedByUserId != userId || attachment.CommentId.HasValue)
            throw new AbpAuthorizationException();
        db.SocialCommentAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);
        await container.DeleteAsync(attachment.BlobName, ct);
    }

    internal async Task DeleteBlobsAsync(IEnumerable<string> blobNames, CancellationToken ct)
    {
        foreach (var blobName in blobNames.Distinct(StringComparer.Ordinal))
        {
            try
            {
                await container.DeleteAsync(blobName, ct);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not delete social media blob {BlobName} after its post was deleted.", blobName);
            }
        }
    }

    private static string? NormalizeContentType(string contentType, string fileName)
    {
        var normalized = contentType?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized) &&
            !normalized.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return normalized.Length <= 128 ? normalized : null;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => null
        };
    }
}

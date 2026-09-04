using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.EntityFrameworkCore;
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
    IConfiguration configuration) : ITransientDependency
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "video/mp4", "video/webm"
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
}

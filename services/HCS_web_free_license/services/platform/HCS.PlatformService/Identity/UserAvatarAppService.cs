using System.Security.Claims;
using HCS.EntityFrameworkCore;
using HCS.Identity;
using HCS.PlatformService.Storage;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;

namespace HCS.PlatformService.Identity;

public sealed class UserAvatarAppService(
    HCSDbContext db,
    IBlobContainer<AvatarBlobContainer> blobs,
    IHttpContextAccessor httpContext) : ITransientDependency
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public async Task<UserAvatarContent> GetAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var meta = await GetMetaAsync(userId, cancellationToken);
        var stream = await OpenBlobAsync(meta.BlobName, cancellationToken);
        return new UserAvatarContent(stream, meta.ContentType, meta.FileName, meta.LastModificationTime);
    }

    public async Task<UserAvatarMeta> GetMetaAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var targetUserId = ResolveTargetUser(userId);
        var avatar = await db.UserAvatars.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Avatar not found.");
        return new UserAvatarMeta(avatar.ContentType, avatar.FileName, avatar.BlobName, avatar.Size, avatar.LastModificationTime);
    }

    public Task<Stream> OpenBlobAsync(string blobName, CancellationToken cancellationToken = default) =>
        blobs.GetAsync(blobName, cancellationToken);

    public async Task UploadAsync(string fileName, string contentType, Stream content, long size, CancellationToken cancellationToken = default)
    {
        var userId = RequireUser();
        if (size is <= 0 or > UserAvatar.MaxSizeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Avatar must be between 1 byte and 2 MB.");
        }

        var normalizedType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        if (!AllowedContentTypes.Contains(normalizedType))
        {
            throw new BusinessException(code: "HCS:InvalidAvatarType", message: "Avatar must be jpeg, png, webp, or gif.");
        }

        var blobName = AvatarBlobNamePolicy.ForUser(userId);
        var now = DateTime.UtcNow;
        await blobs.SaveAsync(blobName, content, overrideExisting: true, cancellationToken: cancellationToken);

        var existing = await db.UserAvatars.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (existing is null)
        {
            db.UserAvatars.Add(new UserAvatar(userId, fileName, normalizedType, blobName, size, now));
        }
        else
        {
            existing.Replace(fileName, normalizedType, blobName, size, now);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await blobs.DeleteAsync(blobName, cancellationToken: cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUser();
        var existing = await db.UserAvatars.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        var blobName = existing.BlobName;
        db.UserAvatars.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        await blobs.DeleteAsync(blobName, cancellationToken: cancellationToken);
    }

    private Guid ResolveTargetUser(Guid? userId)
    {
        var current = RequireUser();
        return userId ?? current;
    }

    private Guid RequireUser()
    {
        var principal = httpContext.HttpContext?.User ?? new ClaimsPrincipal();
        var value = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            throw new AbpAuthorizationException("An authenticated user is required.");
        }

        return userId;
    }
}

public sealed record UserAvatarContent(Stream Content, string ContentType, string FileName, DateTime LastModificationTime);

public sealed record UserAvatarMeta(string ContentType, string FileName, string BlobName, long Size, DateTime LastModificationTime);

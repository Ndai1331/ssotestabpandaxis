using System.Security.Cryptography;
using HCS.Branding;
using HCS.EntityFrameworkCore;
using HCS.PlatformService.Storage;
using HCS.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SettingManagement;

namespace HCS.PlatformService.Branding;

public sealed class SystemBrandingAppService(
    HCSDbContext db,
    IBlobContainer<BrandingBlobContainer> blobs,
    ISettingManager settingManager,
    ILogger<SystemBrandingAppService> logger) : ISystemBrandingAppService, ITransientDependency
{
    public const long MaxLogoBytes = 2 * 1024 * 1024;
    public const long MaxFaviconBytes = 512 * 1024;
    public const long MaxBackgroundBytes = 5 * 1024 * 1024;

    public Task<SystemBrandingDto> GetAsync() => GetSnapshotAsync();

    public Task<SystemBrandingDto> GetPublicAsync() => GetSnapshotAsync();

    public async Task<SystemBrandingDto> UpdateAsync(
        SystemBrandingUpdateRequest input,
        CancellationToken cancellationToken = default)
    {
        var title = input.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BusinessException("HCS:BrandingTitleRequired", "Branding title is required.");
        }

        if (title.Length > 120)
        {
            throw new BusinessException("HCS:BrandingTitleTooLong", "Branding title cannot exceed 120 characters.");
        }

        var description = input.Description?.Trim() ?? string.Empty;
        if (description.Length > 500)
        {
            throw new BusinessException("HCS:BrandingDescriptionTooLong", "Branding description cannot exceed 500 characters.");
        }

        var prepared = new List<PreparedAsset>();
        try
        {
            await PrepareIfPresentAsync(input.Logo, SystemBrandingDefaults.LogoSlot, MaxLogoBytes, prepared, cancellationToken);
            await PrepareIfPresentAsync(input.Favicon, SystemBrandingDefaults.FaviconSlot, MaxFaviconBytes, prepared, cancellationToken);
            await PrepareIfPresentAsync(input.Background, SystemBrandingDefaults.BackgroundSlot, MaxBackgroundBytes, prepared, cancellationToken);

            var currentRevision = await ReadRevisionAsync(cancellationToken);
            var nextRevision = checked(currentRevision + 1);
            var existing = await db.SystemBrandingAssets.ToDictionaryAsync(x => x.Slot, StringComparer.Ordinal, cancellationToken);
            var oldBlobNames = new List<string>();

            foreach (var asset in prepared)
            {
                var blobName = BrandingBlobNamePolicy.ForSlot(asset.Slot);
                await using var content = new MemoryStream(asset.Bytes, writable: false);
                await blobs.SaveAsync(blobName, content, overrideExisting: false, cancellationToken: cancellationToken);
                asset.UploadedBlobName = blobName;
                if (existing.TryGetValue(asset.Slot, out var current))
                {
                    oldBlobNames.Add(current.BlobName);
                    current.Replace(asset.FileName, asset.ContentType, blobName, asset.Bytes.LongLength, asset.Sha256, nextRevision, DateTime.UtcNow);
                }
                else
                {
                    db.SystemBrandingAssets.Add(new SystemBrandingAsset(
                        asset.Slot,
                        asset.FileName,
                        asset.ContentType,
                        blobName,
                        asset.Bytes.LongLength,
                        asset.Sha256,
                        nextRevision,
                        DateTime.UtcNow));
                }
            }

            foreach (var (slot, remove) in new[]
                     {
                         (SystemBrandingDefaults.LogoSlot, input.RemoveLogo),
                         (SystemBrandingDefaults.FaviconSlot, input.RemoveFavicon),
                         (SystemBrandingDefaults.BackgroundSlot, input.RemoveBackground)
                     })
            {
                if (!remove || prepared.Any(x => x.Slot == slot) || !existing.TryGetValue(slot, out var current))
                {
                    continue;
                }

                oldBlobNames.Add(current.BlobName);
                db.SystemBrandingAssets.Remove(current);
            }

            await settingManager.SetGlobalAsync(HCSSettings.BrandingTitle, title);
            await settingManager.SetGlobalAsync(HCSSettings.BrandingDescription, description);
            await settingManager.SetGlobalAsync(HCSSettings.BrandingRevision, nextRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await SetAssetRevisionAsync(HCSSettings.BrandingLogoRevision, SystemBrandingDefaults.LogoSlot, prepared, input.RemoveLogo, nextRevision, cancellationToken);
            await SetAssetRevisionAsync(HCSSettings.BrandingFaviconRevision, SystemBrandingDefaults.FaviconSlot, prepared, input.RemoveFavicon, nextRevision, cancellationToken);
            await SetAssetRevisionAsync(HCSSettings.BrandingBackgroundRevision, SystemBrandingDefaults.BackgroundSlot, prepared, input.RemoveBackground, nextRevision, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var oldBlobName in oldBlobNames.Distinct(StringComparer.Ordinal))
            {
                try
                {
                    await blobs.DeleteAsync(oldBlobName, cancellationToken: cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not delete replaced branding blob {BlobName}.", oldBlobName);
                }
            }

            return await GetSnapshotAsync(cancellationToken);
        }
        catch
        {
            foreach (var blobName in prepared.Select(x => x.UploadedBlobName).Where(x => x is not null).Select(x => x!))
            {
                try
                {
                    await blobs.DeleteAsync(blobName, cancellationToken: cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not clean up failed branding blob {BlobName}.", blobName);
                }
            }

            throw;
        }
    }

    public async Task<SystemBrandingAssetContent?> GetAssetAsync(
        string slot,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlot = NormalizeSlot(slot);
        var asset = await db.SystemBrandingAssets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slot == normalizedSlot, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var content = await blobs.GetAsync(asset.BlobName, cancellationToken);
        return new SystemBrandingAssetContent(content, asset.ContentType, asset.FileName, asset.Sha256, asset.Revision);
    }

    private async Task<SystemBrandingDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var title = await settingManager.GetOrNullGlobalAsync(HCSSettings.BrandingTitle)
            ?? SystemBrandingDefaults.Title;
        var description = await settingManager.GetOrNullGlobalAsync(HCSSettings.BrandingDescription)
            ?? SystemBrandingDefaults.Description;
        var revision = await ReadRevisionAsync(cancellationToken);
        var assets = await db.SystemBrandingAssets.AsNoTracking()
            .ToDictionaryAsync(x => x.Slot, StringComparer.Ordinal, cancellationToken);

        return new SystemBrandingDto
        {
            Title = string.IsNullOrWhiteSpace(title) ? SystemBrandingDefaults.Title : title,
            Description = string.IsNullOrWhiteSpace(description) ? SystemBrandingDefaults.Description : description,
            Revision = revision,
            Logo = MapAsset(assets, SystemBrandingDefaults.LogoSlot),
            Favicon = MapAsset(assets, SystemBrandingDefaults.FaviconSlot),
            Background = MapAsset(assets, SystemBrandingDefaults.BackgroundSlot)
        };
    }

    private async Task<long> ReadRevisionAsync(CancellationToken cancellationToken)
    {
        var value = await settingManager.GetOrNullGlobalAsync(HCSSettings.BrandingRevision);
        return long.TryParse(value, out var revision) && revision > 0 ? revision : 0;
    }

    private async Task SetAssetRevisionAsync(
        string settingName,
        string slot,
        IReadOnlyCollection<PreparedAsset> prepared,
        bool remove,
        long revision,
        CancellationToken cancellationToken)
    {
        if (prepared.Any(x => x.Slot == slot))
        {
            await settingManager.SetGlobalAsync(settingName, revision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (remove)
        {
            await settingManager.SetGlobalAsync(settingName, "0");
        }
    }

    private static SystemBrandingAssetDto? MapAsset(
        IReadOnlyDictionary<string, SystemBrandingAsset> assets,
        string slot)
    {
        if (!assets.TryGetValue(slot, out var asset))
        {
            return null;
        }

        return new SystemBrandingAssetDto
        {
            Slot = asset.Slot,
            Url = $"/api/hcs/system-branding/assets/{asset.Slot}?v={asset.Revision}",
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            Size = asset.Size,
            Revision = asset.Revision
        };
    }

    private static string NormalizeSlot(string slot)
    {
        var normalized = slot.Trim().ToLowerInvariant();
        if (!Array.Exists(SystemBrandingDefaults.Slots, value => value == normalized))
        {
            throw new BusinessException("HCS:InvalidBrandingAssetSlot", "Unknown branding asset slot.");
        }

        return normalized;
    }

    private static async Task PrepareIfPresentAsync(
        IFormFile? file,
        string slot,
        long maximumBytes,
        ICollection<PreparedAsset> target,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return;
        }

        if (file.Length <= 0 || file.Length > maximumBytes)
        {
            throw new BusinessException("HCS:BrandingAssetTooLarge", $"The {slot} image is too large.");
        }

        await using var input = file.OpenReadStream();
        await using var buffer = new MemoryStream(capacity: checked((int)file.Length));
        await input.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.LongLength <= 0 || bytes.LongLength > maximumBytes)
        {
            throw new BusinessException("HCS:BrandingAssetTooLarge", $"The {slot} image is too large.");
        }

        var contentType = DetectContentType(bytes, slot);
        if (contentType is null)
        {
            throw new BusinessException("HCS:InvalidBrandingAssetType", $"The {slot} image format is not supported.");
        }

        target.Add(new PreparedAsset(
            slot,
            Path.GetFileName(file.FileName),
            contentType,
            bytes,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()));
    }

    private static string? DetectContentType(byte[] bytes, string slot)
    {
        var isPng = bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var isJpeg = bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        var isWebp = bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8.ToArray()) && bytes[8..12].SequenceEqual("WEBP"u8.ToArray());
        var isIco = bytes.Length >= 4 && bytes[..4].SequenceEqual(new byte[] { 0, 0, 1, 0 });

        if (slot == SystemBrandingDefaults.FaviconSlot)
        {
            if (isIco) return "image/x-icon";
            if (isPng) return "image/png";
            return null;
        }

        if (isPng) return "image/png";
        if (isJpeg) return "image/jpeg";
        if (isWebp) return "image/webp";
        return null;
    }

    private sealed class PreparedAsset(
        string slot,
        string fileName,
        string contentType,
        byte[] bytes,
        string sha256)
    {
        public string Slot { get; } = slot;
        public string FileName { get; } = fileName;
        public string ContentType { get; } = contentType;
        public byte[] Bytes { get; } = bytes;
        public string Sha256 { get; } = sha256;
        public string? UploadedBlobName { get; set; }
    }
}

public sealed class SystemBrandingUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool RemoveLogo { get; set; }
    public bool RemoveFavicon { get; set; }
    public bool RemoveBackground { get; set; }
    public IFormFile? Logo { get; set; }
    public IFormFile? Favicon { get; set; }
    public IFormFile? Background { get; set; }
}

public sealed record SystemBrandingAssetContent(
    Stream Content,
    string ContentType,
    string FileName,
    string Sha256,
    long Revision);

using System;
using System.IO;

namespace HCS.Branding;

public sealed class SystemBrandingAsset
{
    private SystemBrandingAsset()
    {
    }

    public SystemBrandingAsset(
        string slot,
        string fileName,
        string contentType,
        string blobName,
        long size,
        string sha256,
        long revision,
        DateTime now)
    {
        Slot = NormalizeSlot(slot);
        Replace(fileName, contentType, blobName, size, sha256, revision, now);
    }

    public string Slot { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public long Revision { get; private set; }
    public DateTime CreationTime { get; private set; }
    public DateTime LastModificationTime { get; private set; }

    public void Replace(
        string fileName,
        string contentType,
        string blobName,
        long size,
        string sha256,
        long revision,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType) ||
            string.IsNullOrWhiteSpace(blobName) || string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("Branding asset metadata is required.");
        }

        if (size <= 0 || revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        FileName = Path.GetFileName(fileName.Trim());
        ContentType = contentType.Trim();
        BlobName = blobName.Trim();
        Size = size;
        Sha256 = sha256.Trim();
        Revision = revision;
        if (CreationTime == default)
        {
            CreationTime = now;
        }

        LastModificationTime = now;
    }

    private static string NormalizeSlot(string slot)
    {
        var normalized = slot.Trim().ToLowerInvariant();
        if (!Array.Exists(SystemBrandingDefaults.Slots, value => value == normalized))
        {
            throw new ArgumentException("Unknown branding asset slot.", nameof(slot));
        }

        return normalized;
    }
}

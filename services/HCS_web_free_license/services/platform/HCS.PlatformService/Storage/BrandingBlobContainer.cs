using Volo.Abp.BlobStoring;

namespace HCS.PlatformService.Storage;

[BlobContainerName("hcs-branding")]
public sealed class BrandingBlobContainer;

public static class BrandingBlobNamePolicy
{
    public static string ForSlot(string slot) => $"branding/{slot}/{Guid.NewGuid():N}";
}

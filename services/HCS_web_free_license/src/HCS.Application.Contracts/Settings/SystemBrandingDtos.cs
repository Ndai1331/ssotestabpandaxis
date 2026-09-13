using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace HCS.Branding;

public sealed class SystemBrandingDto
{
    public string Title { get; set; } = SystemBrandingDefaults.Title;
    public string Description { get; set; } = SystemBrandingDefaults.Description;
    public long Revision { get; set; }
    public SystemBrandingAssetDto? Logo { get; set; }
    public SystemBrandingAssetDto? Favicon { get; set; }
    public SystemBrandingAssetDto? Background { get; set; }
}

public sealed class SystemBrandingAssetDto
{
    public string Slot { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public long Revision { get; set; }
}

public sealed class UpdateSystemBrandingDto
{
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool RemoveLogo { get; set; }
    public bool RemoveFavicon { get; set; }
    public bool RemoveBackground { get; set; }
}

public interface ISystemBrandingAppService
{
    Task<SystemBrandingDto> GetAsync();
    Task<SystemBrandingDto> GetPublicAsync();
}

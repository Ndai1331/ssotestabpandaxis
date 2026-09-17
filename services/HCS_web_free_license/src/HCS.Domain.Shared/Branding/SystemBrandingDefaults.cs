namespace HCS.Branding;

public static class SystemBrandingDefaults
{
    public const string Title = "HCS";
    public const string Description = "Hệ thống hành chính số";

    public const string LogoSlot = "logo";
    public const string FaviconSlot = "favicon";
    public const string BackgroundSlot = "background";

    /// <summary>
    /// Neutral globe used for both logo and favicon until system-branding assets load.
    /// </summary>
    public const string DefaultIconUrl = "/images/logo/default-globe.svg";
    public const string DefaultIconContentType = "image/svg+xml";
    public const string DefaultLogoUrl = DefaultIconUrl;
    public const string DefaultFaviconUrl = DefaultIconUrl;

    public static readonly string[] Slots = [LogoSlot, FaviconSlot, BackgroundSlot];
}

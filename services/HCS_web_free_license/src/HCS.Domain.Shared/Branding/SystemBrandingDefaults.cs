namespace HCS.Branding;

public static class SystemBrandingDefaults
{
    public const string Title = "HCS";
    public const string Description = "Hệ thống hành chính số";

    public const string LogoSlot = "logo";
    public const string FaviconSlot = "favicon";
    public const string BackgroundSlot = "background";

    /// <summary>
    /// Built-in mark used for both logo and favicon until system-branding uploads replace them.
    /// </summary>
    public const string DefaultIconUrl = "/images/logo/hcs-icon.png";
    public const string DefaultLogoUrl = DefaultIconUrl;
    public const string DefaultFaviconUrl = DefaultIconUrl;

    public static readonly string[] Slots = [LogoSlot, FaviconSlot, BackgroundSlot];
}

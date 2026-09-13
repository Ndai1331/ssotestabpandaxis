namespace HCS.Settings;

public static class HCSSettings
{
    private const string Prefix = "HCS";

    public const string ShowSsoLoginButton = Prefix + ".Authentication.ShowSsoLoginButton";
    public const string BrandingTitle = Prefix + ".Branding.Title";
    public const string BrandingDescription = Prefix + ".Branding.Description";
    public const string BrandingRevision = Prefix + ".Branding.Revision";
    public const string BrandingLogoRevision = Prefix + ".Branding.LogoRevision";
    public const string BrandingFaviconRevision = Prefix + ".Branding.FaviconRevision";
    public const string BrandingBackgroundRevision = Prefix + ".Branding.BackgroundRevision";
}

using System;

namespace HCS.Settings;

public static class HCSSettings
{
    private const string Prefix = "HCS";

    public const string ShowSsoLoginButton = Prefix + ".Authentication.ShowSsoLoginButton";
    public const string KeycloakEnabled = Prefix + ".Authentication.Keycloak.Enabled";
    public const string KeycloakBaseUrl = Prefix + ".Authentication.Keycloak.BaseUrl";
    public const string KeycloakRealm = Prefix + ".Authentication.Keycloak.Realm";
    public const string KeycloakClientId = Prefix + ".Authentication.Keycloak.ClientId";
    public const string KeycloakClientSecret = Prefix + ".Authentication.Keycloak.ClientSecret";
    public const string KeycloakRequireHttpsMetadata = Prefix + ".Authentication.Keycloak.RequireHttpsMetadata";
    public const string KeycloakAdminUser = Prefix + ".Authentication.Keycloak.AdminUser";
    public const string KeycloakAdminSecret = Prefix + ".Authentication.Keycloak.AdminSecret";
    public const string KeycloakAppAccessGroup = Prefix + ".Authentication.Keycloak.AppAccessGroup";
    public const string KeycloakRoleMappings = Prefix + ".Authentication.Keycloak.RoleMappings";
    public const string KeycloakRevision = Prefix + ".Authentication.Keycloak.Revision";
    public const string AllowSigningFromDocuments = Prefix + ".Workflow.AllowSigningFromDocuments";
    public const string ChatAttachmentMaxMegabytes = Prefix + ".Chat.AttachmentMaxMegabytes";
    public const int ChatAttachmentMaxMegabytesDefault = 512;
    public const int ChatAttachmentMaxMegabytesMin = 1;
    public const int ChatAttachmentMaxMegabytesMax = 2048;
    public const string BrandingTitle = Prefix + ".Branding.Title";
    public const string BrandingDescription = Prefix + ".Branding.Description";
    public const string BrandingShowText = Prefix + ".Branding.ShowText";
    public const string BrandingRevision = Prefix + ".Branding.Revision";
    public const string BrandingLogoRevision = Prefix + ".Branding.LogoRevision";
    public const string BrandingFaviconRevision = Prefix + ".Branding.FaviconRevision";
    public const string BrandingBackgroundRevision = Prefix + ".Branding.BackgroundRevision";
    public const string StorageProvider = Prefix + ".Storage.Provider";
    public const string StorageEndPoint = Prefix + ".Storage.EndPoint";
    public const string StorageAccessKey = Prefix + ".Storage.AccessKey";
    public const string StorageSecretKey = Prefix + ".Storage.SecretKey";
    public const string StorageWithSsl = Prefix + ".Storage.WithSSL";
    public const string StorageCreateBucketIfNotExists = Prefix + ".Storage.CreateBucketIfNotExists";
    public const string StorageRegion = Prefix + ".Storage.Region";
    public const string StorageFtpHost = Prefix + ".Storage.FtpHost";
    public const string StorageFtpPort = Prefix + ".Storage.FtpPort";
    public const string StorageFtpUser = Prefix + ".Storage.FtpUser";
    public const string StorageFtpPassword = Prefix + ".Storage.FtpPassword";
    public const string StorageFtpPassive = Prefix + ".Storage.FtpPassive";
    public const string StorageFtpBasePath = Prefix + ".Storage.FtpBasePath";
    public const string StorageRevision = Prefix + ".Storage.Revision";
    public const string LegacySigningReportEnabled = Prefix + ".LegacySigningReport.Enabled";
    public const string LegacySigningReportSqlServerConnectionString =
        Prefix + ".LegacySigningReport.SqlServerConnectionString";

    public static bool IsEnabledOrDefault(string? value) =>
        !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);

    public static int ParseChatAttachmentMaxMegabytes(string? value)
    {
        if (!int.TryParse(value, out var parsed))
        {
            return ChatAttachmentMaxMegabytesDefault;
        }

        return ClampChatAttachmentMaxMegabytes(parsed);
    }

    public static int ClampChatAttachmentMaxMegabytes(int megabytes) =>
        Math.Clamp(megabytes, ChatAttachmentMaxMegabytesMin, ChatAttachmentMaxMegabytesMax);

    public static long ChatAttachmentMaxBytes(int megabytes) =>
        (long)ClampChatAttachmentMaxMegabytes(megabytes) * 1024 * 1024;
}

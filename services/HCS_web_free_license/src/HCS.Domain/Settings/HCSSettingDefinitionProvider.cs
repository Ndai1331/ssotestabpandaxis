using HCS.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace HCS.Settings;

public class HCSSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                HCSSettings.ShowSsoLoginButton,
                defaultValue: "true",
                displayName: L("Settings:SsoLoginButton"),
                description: L("Settings:SsoLoginButtonDescription")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakEnabled,
                defaultValue: null,
                displayName: L("Settings:SsoEnabled"),
                description: L("Settings:SsoEnabledDescription")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakBaseUrl,
                defaultValue: null,
                displayName: L("Settings:SsoBaseUrl")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakRealm,
                defaultValue: KeycloakSettingDefaults.Realm,
                displayName: L("Settings:SsoRealm")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakClientId,
                defaultValue: KeycloakSettingDefaults.ClientId,
                displayName: L("Settings:SsoClientId")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakClientSecret,
                defaultValue: null,
                displayName: L("Settings:SsoClientSecret"),
                isVisibleToClients: false,
                isEncrypted: true));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakRequireHttpsMetadata,
                defaultValue: null,
                displayName: L("Settings:SsoRequireHttps")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakAdminUser,
                defaultValue: null,
                displayName: L("Settings:SsoAdminUser")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakAdminSecret,
                defaultValue: null,
                displayName: L("Settings:SsoAdminSecret"),
                isVisibleToClients: false,
                isEncrypted: true));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakAppAccessGroup,
                defaultValue: KeycloakSettingDefaults.AppAccessGroup,
                displayName: L("Settings:SsoAppAccessGroup")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakRoleMappings,
                defaultValue: KeycloakRoleMappingSerializer.Serialize(KeycloakSettingDefaults.RoleMappings),
                displayName: L("Settings:SsoRoleMappings")));
        context.Add(
            new SettingDefinition(
                HCSSettings.KeycloakRevision,
                defaultValue: "0",
                displayName: L("Settings:SsoRevision")));
        context.Add(
            new SettingDefinition(
                HCSSettings.AllowSigningFromDocuments,
                defaultValue: "true",
                displayName: L("Settings:AllowSigningFromDocuments"),
                description: L("Settings:AllowSigningFromDocumentsDescription"),
                isVisibleToClients: true));
        context.Add(
            new SettingDefinition(
                HCSSettings.LegacySigningReportEnabled,
                defaultValue: "true",
                displayName: L("Settings:ProposalStatistics"),
                description: L("Settings:ProposalStatisticsDescription"),
                isVisibleToClients: true));
        context.Add(
            new SettingDefinition(
                HCSSettings.BrandingTitle,
                defaultValue: HCS.Branding.SystemBrandingDefaults.Title,
                displayName: L("Settings:BrandingTitle"),
                description: L("Settings:BrandingTitleDescription")));
        context.Add(
            new SettingDefinition(
                HCSSettings.BrandingDescription,
                defaultValue: HCS.Branding.SystemBrandingDefaults.Description,
                displayName: L("Settings:BrandingDescription"),
                description: L("Settings:BrandingDescriptionDescription")));
        context.Add(new SettingDefinition(HCSSettings.BrandingShowText));
        context.Add(
            new SettingDefinition(
                HCSSettings.BrandingRevision,
                defaultValue: "0",
                displayName: L("Settings:BrandingRevision")));
        context.Add(new SettingDefinition(HCSSettings.BrandingLogoRevision, defaultValue: "0"));
        context.Add(new SettingDefinition(HCSSettings.BrandingFaviconRevision, defaultValue: "0"));
        context.Add(new SettingDefinition(HCSSettings.BrandingBackgroundRevision, defaultValue: "0"));
        context.Add(
            new SettingDefinition(
                HCSSettings.LegacySigningReportSqlServerConnectionString,
                defaultValue: null,
                displayName: L("Settings:LegacySqlConnectionString"),
                description: L("Settings:LegacySqlConnectionHint"),
                isVisibleToClients: false,
                isEncrypted: true));
        StorageSettingDefinitions.Add(context);
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<HCSResource>(name);
    }
}

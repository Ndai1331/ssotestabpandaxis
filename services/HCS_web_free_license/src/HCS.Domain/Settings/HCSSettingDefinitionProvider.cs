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
        context.Add(
            new SettingDefinition(
                HCSSettings.BrandingRevision,
                defaultValue: "0",
                displayName: L("Settings:BrandingRevision")));
        context.Add(new SettingDefinition(HCSSettings.BrandingLogoRevision, defaultValue: "0"));
        context.Add(new SettingDefinition(HCSSettings.BrandingFaviconRevision, defaultValue: "0"));
        context.Add(new SettingDefinition(HCSSettings.BrandingBackgroundRevision, defaultValue: "0"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<HCSResource>(name);
    }
}

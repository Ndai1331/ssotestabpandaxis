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
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<HCSResource>(name);
    }
}

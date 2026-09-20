using HCS.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace HCS.Settings;

public static class StorageSettingDefinitions
{
    public static void Add(ISettingDefinitionContext context)
    {
        if (context.GetOrNull(HCSSettings.StorageProvider) is not null)
        {
            return;
        }

        context.Add(
            new SettingDefinition(
                HCSSettings.StorageProvider,
                defaultValue: StorageSettingDefaults.Provider,
                displayName: L("Settings:StorageProvider")));
        context.Add(new SettingDefinition(HCSSettings.StorageEndPoint, displayName: L("Settings:StorageEndPoint")));
        context.Add(
            new SettingDefinition(
                HCSSettings.StorageAccessKey,
                displayName: L("Settings:StorageAccessKey"),
                isVisibleToClients: false,
                isEncrypted: true));
        context.Add(
            new SettingDefinition(
                HCSSettings.StorageSecretKey,
                displayName: L("Settings:StorageSecretKey"),
                isVisibleToClients: false,
                isEncrypted: true));
        context.Add(new SettingDefinition(HCSSettings.StorageWithSsl, displayName: L("Settings:StorageWithSsl")));
        context.Add(
            new SettingDefinition(
                HCSSettings.StorageCreateBucketIfNotExists,
                defaultValue: "true",
                displayName: L("Settings:StorageCreateBucket")));
        context.Add(new SettingDefinition(HCSSettings.StorageRegion, displayName: L("Settings:StorageRegion")));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpHost, displayName: L("Settings:StorageFtpHost")));
        context.Add(
            new SettingDefinition(
                HCSSettings.StorageFtpPort,
                defaultValue: StorageSettingDefaults.FtpPort.ToString(),
                displayName: L("Settings:StorageFtpPort")));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpUser, displayName: L("Settings:StorageFtpUser")));
        context.Add(
            new SettingDefinition(
                HCSSettings.StorageFtpPassword,
                displayName: L("Settings:StorageFtpPassword"),
                isVisibleToClients: false,
                isEncrypted: true));
        context.Add(
            new SettingDefinition(
                HCSSettings.StorageFtpPassive,
                defaultValue: "true",
                displayName: L("Settings:StorageFtpPassive")));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpBasePath, displayName: L("Settings:StorageFtpBasePath")));
        context.Add(new SettingDefinition(HCSSettings.StorageRevision, defaultValue: "0", displayName: L("Settings:StorageRevision")));
    }

    private static LocalizableString L(string name) => LocalizableString.Create<HCSResource>(name);
}

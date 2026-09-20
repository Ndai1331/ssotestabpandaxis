using HCS.Settings;
using Volo.Abp.BlobStoring.Minio;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.Settings;

namespace HCS.BlobStorage;

[DependsOn(
    typeof(HCSDomainSharedModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpBlobStoringMinioModule))]
public class HcsBlobStorageModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDbConnectionOptions>(options =>
        {
            options.Databases.Configure("AbpSettingManagement", database =>
            {
                database.MappedConnections.Add("Default");
            });
        });
        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure<SettingManagementDbContext>(db => db.UseNpgsql());
        });
    }
}

public class StorageSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        if (context.GetOrNull(HCSSettings.StorageProvider) is not null)
        {
            return;
        }

        context.Add(
            new SettingDefinition(HCSSettings.StorageProvider, defaultValue: StorageSettingDefaults.Provider));
        context.Add(new SettingDefinition(HCSSettings.StorageEndPoint));
        context.Add(new SettingDefinition(HCSSettings.StorageAccessKey, isVisibleToClients: false, isEncrypted: true));
        context.Add(new SettingDefinition(HCSSettings.StorageSecretKey, isVisibleToClients: false, isEncrypted: true));
        context.Add(new SettingDefinition(HCSSettings.StorageWithSsl));
        context.Add(new SettingDefinition(HCSSettings.StorageCreateBucketIfNotExists, defaultValue: "true"));
        context.Add(new SettingDefinition(HCSSettings.StorageRegion));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpHost));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpPort, defaultValue: StorageSettingDefaults.FtpPort.ToString()));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpUser));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpPassword, isVisibleToClients: false, isEncrypted: true));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpPassive, defaultValue: "true"));
        context.Add(new SettingDefinition(HCSSettings.StorageFtpBasePath));
        context.Add(new SettingDefinition(HCSSettings.StorageRevision, defaultValue: "0"));
    }
}

using HCS.Settings;
using Microsoft.Extensions.Configuration;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Minio;

namespace HCS.BlobStorage;

public static class HcsBlobContainerExtensions
{
    public static void UseHcsStorage(
        this BlobContainerConfiguration container,
        IConfiguration configuration,
        Action<MinioBlobProviderConfiguration>? extra = null)
    {
        container.UseMinio(minio =>
        {
            minio.EndPoint = configuration["Minio:EndPoint"] ?? StorageSettingDefaults.EndPoint;
            var accessKey = configuration["Minio:AccessKey"];
            if (!string.IsNullOrWhiteSpace(accessKey))
            {
                minio.AccessKey = accessKey;
            }

            var secretKey = configuration["Minio:SecretKey"];
            if (!string.IsNullOrWhiteSpace(secretKey))
            {
                minio.SecretKey = secretKey;
            }

            minio.WithSSL = configuration.GetValue("Minio:WithSSL", false);
            minio.CreateBucketIfNotExists = configuration.GetValue("Minio:CreateBucketIfNotExists", true);
            extra?.Invoke(minio);
        });
        container.ProviderType = typeof(SettingsAwareMinioBlobProvider);
    }
}

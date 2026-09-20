using System.Collections.Generic;
using HCS.BlobStorage;
using HCS.Settings;
using Microsoft.Extensions.Configuration;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Minio;
using Xunit;

namespace HCS;

public sealed class StorageSettingsResolutionTests
{
    [Fact]
    public void Resolve_PrefersStoredValuesOverEnvironmentFallback()
    {
        var resolved = StorageSettingsResolution.Resolve(
            new StorageSettingValues
            {
                Provider = "AmazonS3",
                EndPoint = "s3.ap-southeast-1.amazonaws.com",
                AccessKey = "db-access",
                SecretKey = "db-secret",
                WithSsl = "true",
                CreateBucketIfNotExists = "false",
                Region = "ap-southeast-1",
                Revision = "3"
            },
            new StorageSettingValues
            {
                Provider = "Minio",
                EndPoint = "minio:9000",
                AccessKey = "env-access",
                SecretKey = "env-secret",
                WithSsl = "false",
                CreateBucketIfNotExists = "true"
            });

        Assert.Equal(StorageProviderKind.AmazonS3, resolved.Provider);
        Assert.Equal("s3.ap-southeast-1.amazonaws.com", resolved.EndPoint);
        Assert.Equal("s3.ap-southeast-1.amazonaws.com", resolved.BlobEndPoint);
        Assert.Equal("db-access", resolved.AccessKey);
        Assert.Equal("db-secret", resolved.SecretKey);
        Assert.True(resolved.WithSsl);
        Assert.False(resolved.CreateBucketIfNotExists);
        Assert.Equal("ap-southeast-1", resolved.Region);
        Assert.Equal("3", resolved.Revision);
        Assert.True(resolved.HasAccessKey);
        Assert.True(resolved.HasSecretKey);
    }

    [Fact]
    public void Resolve_UsesEnvironmentWhenDatabaseIsEmpty()
    {
        var resolved = StorageSettingsResolution.Resolve(
            new StorageSettingValues(),
            new StorageSettingValues
            {
                EndPoint = "minio:9000",
                AccessKey = "env-access",
                SecretKey = "env-secret",
                WithSsl = "false"
            });

        Assert.Equal(StorageProviderKind.Minio, resolved.Provider);
        Assert.Equal("minio:9000", resolved.EndPoint);
        Assert.Equal("env-access", resolved.AccessKey);
        Assert.Equal("env-secret", resolved.SecretKey);
        Assert.False(resolved.WithSsl);
    }

    [Fact]
    public void EmptySecretDoesNotReplaceStoredSecret()
    {
        Assert.False(StorageSettingsResolution.ShouldReplaceSecret(null));
        Assert.False(StorageSettingsResolution.ShouldReplaceSecret("  "));
        Assert.True(StorageSettingsResolution.ShouldReplaceSecret("new-secret"));
        Assert.Equal("2", StorageSettingsResolution.NextRevision("1"));

        var merged = StorageSettingsAppService.MergeInput(
            new UpdateStorageSettingsDto
            {
                Provider = "Minio",
                EndPoint = "minio:9000",
                AccessKey = " ",
                SecretKey = null,
                FtpPassword = null
            },
            new StorageSettingValues
            {
                AccessKey = "kept-access",
                SecretKey = "kept-secret",
                FtpPassword = "kept-ftp"
            });

        Assert.Equal("kept-access", merged.AccessKey);
        Assert.Equal("kept-secret", merged.SecretKey);
        Assert.Equal("kept-ftp", merged.FtpPassword);
    }

    [Fact]
    public void FtpDoesNotChangeMinioBlobSelector()
    {
        var resolved = StorageSettingsResolution.Resolve(
            new StorageSettingValues
            {
                Provider = "Ftp",
                FtpHost = "ftp.example",
                EndPoint = "minio:9000",
                AccessKey = "minio-access",
                SecretKey = "minio-secret"
            },
            new StorageSettingValues
            {
                EndPoint = "localhost:9000",
                AccessKey = "env-access",
                SecretKey = "env-secret"
            });

        Assert.Equal(StorageProviderKind.Ftp, resolved.Provider);
        Assert.Equal("minio:9000", resolved.BlobEndPoint);
        Assert.Equal("minio-access", resolved.AccessKey);

        var configuration = new BlobContainerConfiguration();
        configuration.UseHcsStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Minio:EndPoint"] = "localhost:9000",
            ["Minio:AccessKey"] = "env-access",
            ["Minio:SecretKey"] = "env-secret"
        }).Build());
        SettingsAwareMinioBlobProvider.Apply(configuration, resolved);

        Assert.Equal(typeof(SettingsAwareMinioBlobProvider), configuration.ProviderType);
        Assert.Equal("minio:9000", configuration.GetMinioConfiguration().EndPoint);
        Assert.Equal("minio-access", configuration.GetMinioConfiguration().AccessKey);
    }

    [Fact]
    public void AmazonS3_BuildsEndpointFromRegionWhenMissing()
    {
        var resolved = StorageSettingsResolution.Resolve(
            new StorageSettingValues { Provider = "AmazonS3", Region = "ap-southeast-1" },
            new StorageSettingValues());

        Assert.Equal("s3.ap-southeast-1.amazonaws.com", resolved.BlobEndPoint);
        Assert.True(resolved.WithSsl);
    }
}

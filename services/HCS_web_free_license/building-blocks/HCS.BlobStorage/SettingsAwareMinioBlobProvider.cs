using HCS.Settings;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Minio;
using Volo.Abp.DependencyInjection;

namespace HCS.BlobStorage;

public sealed class SettingsAwareMinioBlobProvider(
    MinioBlobProvider inner,
    IStorageSettingsResolver resolver) : IBlobProvider, ITransientDependency
{
    public async Task SaveAsync(BlobProviderSaveArgs args)
    {
        await ApplyAsync(args.Configuration, args.CancellationToken);
        await inner.SaveAsync(args);
    }

    public async Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        await ApplyAsync(args.Configuration, args.CancellationToken);
        return await inner.DeleteAsync(args);
    }

    public async Task<bool> ExistsAsync(BlobProviderExistsArgs args)
    {
        await ApplyAsync(args.Configuration, args.CancellationToken);
        return await inner.ExistsAsync(args);
    }

    public async Task<Stream?> GetOrNullAsync(BlobProviderGetArgs args)
    {
        await ApplyAsync(args.Configuration, args.CancellationToken);
        return await inner.GetOrNullAsync(args);
    }

    private async Task ApplyAsync(BlobContainerConfiguration configuration, CancellationToken cancellationToken)
    {
        var settings = await resolver.RefreshAsync(cancellationToken);
        Apply(configuration, settings);
    }

    public static void Apply(BlobContainerConfiguration configuration, StorageResolvedSettings settings)
    {
        var minio = configuration.GetMinioConfiguration();
        if (!string.IsNullOrWhiteSpace(settings.BlobEndPoint))
        {
            minio.EndPoint = settings.BlobEndPoint;
        }

        if (!string.IsNullOrWhiteSpace(settings.AccessKey))
        {
            minio.AccessKey = settings.AccessKey;
        }

        if (!string.IsNullOrWhiteSpace(settings.SecretKey))
        {
            minio.SecretKey = settings.SecretKey;
        }

        minio.WithSSL = settings.WithSsl;
        minio.CreateBucketIfNotExists = settings.CreateBucketIfNotExists;
    }
}

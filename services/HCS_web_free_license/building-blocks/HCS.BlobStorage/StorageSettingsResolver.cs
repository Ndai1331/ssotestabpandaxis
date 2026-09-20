using HCS.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SettingManagement;

namespace HCS.BlobStorage;

public interface IStorageSettingsResolver
{
    StorageResolvedSettings Current { get; }
    Task<StorageResolvedSettings> RefreshAsync(CancellationToken cancellationToken = default);
}

public sealed class StorageSettingsResolver(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory) : IStorageSettingsResolver, ISingletonDependency
{
    private readonly object _sync = new();
    private StorageResolvedSettings _current =
        StorageSettingsResolution.Resolve(new StorageSettingValues(), FromConfiguration(configuration));
    private DateTime _nextRefreshUtc = DateTime.MinValue;

    public StorageResolvedSettings Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
    }

    public async Task<StorageResolvedSettings> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow < _nextRefreshUtc)
        {
            return Current;
        }

        using var scope = scopeFactory.CreateScope();
        var settingManager = scope.ServiceProvider.GetService<ISettingManager>();
        StorageSettingValues stored;
        try
        {
            stored = settingManager is null
                ? new StorageSettingValues()
                : await ReadStoredAsync(settingManager);
        }
        catch
        {
            stored = new StorageSettingValues();
        }
        var resolved = StorageSettingsResolution.Resolve(stored, FromConfiguration(configuration));
        lock (_sync)
        {
            _current = resolved;
            _nextRefreshUtc = DateTime.UtcNow.AddSeconds(3);
        }

        return resolved;
    }

    public static StorageSettingValues FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Minio");
        return new StorageSettingValues
        {
            Provider = StorageSettingDefaults.Provider,
            EndPoint = section["EndPoint"],
            AccessKey = section["AccessKey"],
            SecretKey = section["SecretKey"],
            WithSsl = section["WithSSL"],
            CreateBucketIfNotExists = section["CreateBucketIfNotExists"]
        };
    }

    private static async Task<StorageSettingValues> ReadStoredAsync(ISettingManager settingManager)
    {
        return new StorageSettingValues
        {
            Provider = await ReadAsync(settingManager, HCSSettings.StorageProvider),
            EndPoint = await ReadAsync(settingManager, HCSSettings.StorageEndPoint),
            AccessKey = await ReadAsync(settingManager, HCSSettings.StorageAccessKey),
            SecretKey = await ReadAsync(settingManager, HCSSettings.StorageSecretKey),
            WithSsl = await ReadAsync(settingManager, HCSSettings.StorageWithSsl),
            CreateBucketIfNotExists = await ReadAsync(settingManager, HCSSettings.StorageCreateBucketIfNotExists),
            Region = await ReadAsync(settingManager, HCSSettings.StorageRegion),
            Revision = await ReadAsync(settingManager, HCSSettings.StorageRevision)
        };
    }

    private static Task<string> ReadAsync(ISettingManager settingManager, string name) =>
        settingManager.GetOrNullGlobalAsync(name, fallback: false);
}

using HCS.Settings;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SettingManagement;

namespace HCS.AuthServer;

public sealed class KeycloakSettingsResolver(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory) : ISingletonDependency
{
    private readonly object _sync = new();
    private KeycloakResolvedSettings _current =
        KeycloakSettingsResolution.Resolve(new KeycloakSettingValues(), FromConfiguration(configuration));

    public KeycloakResolvedSettings Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
    }

    public async Task<KeycloakResolvedSettings> RefreshAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var settingManager = scope.ServiceProvider.GetService<ISettingManager>();
        var stored = settingManager is null
            ? new KeycloakSettingValues()
            : await ReadStoredAsync(settingManager);
        var resolved = KeycloakSettingsResolution.Resolve(stored, FromConfiguration(configuration));
        lock (_sync)
        {
            _current = resolved;
        }

        return resolved;
    }

    public static KeycloakSettingValues FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(KeycloakOptions.SectionName);
        return new KeycloakSettingValues
        {
            Enabled = section["Enabled"],
            Authority = section["Authority"],
            MetadataAddress = section["MetadataAddress"],
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            RequireHttpsMetadata = section["RequireHttpsMetadata"]
        };
    }

    private static async Task<KeycloakSettingValues> ReadStoredAsync(ISettingManager settingManager)
    {
        return new KeycloakSettingValues
        {
            Enabled = await ReadAsync(settingManager, HCSSettings.KeycloakEnabled),
            ShowSsoLoginButton = await ReadAsync(settingManager, HCSSettings.ShowSsoLoginButton),
            BaseUrl = await ReadAsync(settingManager, HCSSettings.KeycloakBaseUrl),
            Realm = await ReadAsync(settingManager, HCSSettings.KeycloakRealm),
            ClientId = await ReadAsync(settingManager, HCSSettings.KeycloakClientId),
            ClientSecret = await ReadAsync(settingManager, HCSSettings.KeycloakClientSecret),
            RequireHttpsMetadata = await ReadAsync(settingManager, HCSSettings.KeycloakRequireHttpsMetadata),
            AppAccessGroup = await ReadAsync(settingManager, HCSSettings.KeycloakAppAccessGroup),
            RoleMappings = await ReadAsync(settingManager, HCSSettings.KeycloakRoleMappings),
            Revision = await ReadAsync(settingManager, HCSSettings.KeycloakRevision)
        };
    }

    private static Task<string> ReadAsync(ISettingManager settingManager, string name) =>
        settingManager.GetOrNullGlobalAsync(name, fallback: false);
}

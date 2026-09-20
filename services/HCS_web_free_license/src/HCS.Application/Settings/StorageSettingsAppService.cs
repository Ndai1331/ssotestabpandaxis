using System;
using System.Net;
using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Minio;
using Volo.Abp;
using Volo.Abp.SettingManagement;

namespace HCS.Settings;

[RemoteService(IsEnabled = false)]
[Authorize(HCSPermissions.SystemBranding.Update)]
public class StorageSettingsAppService(
    ISettingManager settingManager,
    IConfiguration configuration) : HCSAppService, IStorageSettingsAppService
{
    public async Task<StorageSettingsDto> GetAsync()
    {
        var resolved = StorageSettingsResolution.Resolve(await ReadStoredAsync(), FromEnvironment());
        return new StorageSettingsDto
        {
            Provider = resolved.Provider.ToString(),
            EndPoint = resolved.EndPoint,
            HasAccessKey = resolved.HasAccessKey,
            HasSecretKey = resolved.HasSecretKey,
            WithSsl = resolved.WithSsl,
            CreateBucketIfNotExists = resolved.CreateBucketIfNotExists,
            Region = resolved.Region,
            FtpHost = resolved.FtpHost,
            FtpPort = resolved.FtpPort,
            FtpUser = resolved.FtpUser,
            HasFtpPassword = resolved.HasFtpPassword,
            FtpPassive = resolved.FtpPassive,
            FtpBasePath = resolved.FtpBasePath
        };
    }

    public async Task UpdateAsync(UpdateStorageSettingsDto input)
    {
        Check.NotNull(input, nameof(input));
        var provider = StorageSettingsResolution.ParseProvider(input.Provider);
        if (provider is StorageProviderKind.Minio or StorageProviderKind.AmazonS3)
        {
            if (provider == StorageProviderKind.Minio)
            {
                Check.NotNullOrWhiteSpace(input.EndPoint, nameof(input.EndPoint));
            }
            else if (string.IsNullOrWhiteSpace(input.EndPoint) && string.IsNullOrWhiteSpace(input.Region))
            {
                throw new BusinessException("HCS:StorageEndpointRequired");
            }
        }

        await settingManager.SetGlobalAsync(HCSSettings.StorageProvider, provider.ToString());
        await settingManager.SetGlobalAsync(HCSSettings.StorageEndPoint, Trim(input.EndPoint));
        if (StorageSettingsResolution.ShouldReplaceSecret(input.AccessKey))
        {
            await settingManager.SetGlobalAsync(HCSSettings.StorageAccessKey, input.AccessKey!.Trim());
        }

        if (StorageSettingsResolution.ShouldReplaceSecret(input.SecretKey))
        {
            await settingManager.SetGlobalAsync(HCSSettings.StorageSecretKey, input.SecretKey!.Trim());
        }

        await settingManager.SetGlobalAsync(HCSSettings.StorageWithSsl, Bool(input.WithSsl));
        await settingManager.SetGlobalAsync(
            HCSSettings.StorageCreateBucketIfNotExists,
            Bool(input.CreateBucketIfNotExists));
        await settingManager.SetGlobalAsync(HCSSettings.StorageRegion, Trim(input.Region));
        await settingManager.SetGlobalAsync(HCSSettings.StorageFtpHost, Trim(input.FtpHost));
        await settingManager.SetGlobalAsync(
            HCSSettings.StorageFtpPort,
            input.FtpPort > 0 ? input.FtpPort.ToString() : StorageSettingDefaults.FtpPort.ToString());
        await settingManager.SetGlobalAsync(HCSSettings.StorageFtpUser, Trim(input.FtpUser));
        if (StorageSettingsResolution.ShouldReplaceSecret(input.FtpPassword))
        {
            await settingManager.SetGlobalAsync(HCSSettings.StorageFtpPassword, input.FtpPassword!.Trim());
        }

        await settingManager.SetGlobalAsync(HCSSettings.StorageFtpPassive, Bool(input.FtpPassive));
        await settingManager.SetGlobalAsync(HCSSettings.StorageFtpBasePath, Trim(input.FtpBasePath));
        var revision = await settingManager.GetOrNullGlobalAsync(HCSSettings.StorageRevision, fallback: false);
        await settingManager.SetGlobalAsync(
            HCSSettings.StorageRevision,
            StorageSettingsResolution.NextRevision(revision));
    }

    public async Task<StorageConnectionTestResultDto> TestAsync(UpdateStorageSettingsDto input)
    {
        Check.NotNull(input, nameof(input));
        var stored = await ReadStoredAsync();
        var resolved = StorageSettingsResolution.Resolve(MergeInput(input, stored), FromEnvironment());
        try
        {
            if (resolved.Provider == StorageProviderKind.Ftp)
            {
                return await TestFtpAsync(resolved);
            }

            return await TestObjectStorageAsync(resolved);
        }
        catch (Exception exception)
        {
            return Failed(exception.Message);
        }
    }

    internal static StorageSettingValues MergeInput(UpdateStorageSettingsDto input, StorageSettingValues stored)
    {
        return new StorageSettingValues
        {
            Provider = input.Provider,
            EndPoint = input.EndPoint,
            AccessKey = StorageSettingsResolution.ShouldReplaceSecret(input.AccessKey)
                ? input.AccessKey
                : stored.AccessKey,
            SecretKey = StorageSettingsResolution.ShouldReplaceSecret(input.SecretKey)
                ? input.SecretKey
                : stored.SecretKey,
            WithSsl = Bool(input.WithSsl),
            CreateBucketIfNotExists = Bool(input.CreateBucketIfNotExists),
            Region = input.Region,
            FtpHost = input.FtpHost,
            FtpPort = input.FtpPort > 0 ? input.FtpPort.ToString() : null,
            FtpUser = input.FtpUser,
            FtpPassword = StorageSettingsResolution.ShouldReplaceSecret(input.FtpPassword)
                ? input.FtpPassword
                : stored.FtpPassword,
            FtpPassive = Bool(input.FtpPassive),
            FtpBasePath = input.FtpBasePath
        };
    }

    private async Task<StorageSettingValues> ReadStoredAsync()
    {
        return new StorageSettingValues
        {
            Provider = await ReadAsync(HCSSettings.StorageProvider),
            EndPoint = await ReadAsync(HCSSettings.StorageEndPoint),
            AccessKey = await ReadAsync(HCSSettings.StorageAccessKey),
            SecretKey = await ReadAsync(HCSSettings.StorageSecretKey),
            WithSsl = await ReadAsync(HCSSettings.StorageWithSsl),
            CreateBucketIfNotExists = await ReadAsync(HCSSettings.StorageCreateBucketIfNotExists),
            Region = await ReadAsync(HCSSettings.StorageRegion),
            FtpHost = await ReadAsync(HCSSettings.StorageFtpHost),
            FtpPort = await ReadAsync(HCSSettings.StorageFtpPort),
            FtpUser = await ReadAsync(HCSSettings.StorageFtpUser),
            FtpPassword = await ReadAsync(HCSSettings.StorageFtpPassword),
            FtpPassive = await ReadAsync(HCSSettings.StorageFtpPassive),
            FtpBasePath = await ReadAsync(HCSSettings.StorageFtpBasePath),
            Revision = await ReadAsync(HCSSettings.StorageRevision)
        };
    }

    private Task<string> ReadAsync(string name) =>
        settingManager.GetOrNullGlobalAsync(name, fallback: false);

    private StorageSettingValues FromEnvironment()
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

    private async Task<StorageConnectionTestResultDto> TestObjectStorageAsync(StorageResolvedSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.BlobEndPoint))
        {
            return Failed(L["Settings:StorageTestMissingEndPoint"]);
        }

        var client = new MinioClient()
            .WithEndpoint(settings.BlobEndPoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.WithSsl)
            .Build();
        await client.ListBucketsAsync();
        return Succeeded(L["Settings:StorageTestObjectOk"]);
    }

    private async Task<StorageConnectionTestResultDto> TestFtpAsync(StorageResolvedSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.FtpHost))
        {
            return Failed(L["Settings:StorageTestMissingFtp"]);
        }

        var path = string.IsNullOrWhiteSpace(settings.FtpBasePath) ? "/" : settings.FtpBasePath.Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        var uri = new Uri($"ftp://{settings.FtpHost}:{settings.FtpPort}{path}");
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;
        request.Credentials = new NetworkCredential(settings.FtpUser, settings.FtpPassword);
        request.UsePassive = settings.FtpPassive;
        request.UseBinary = true;
        request.Timeout = 10000;
        request.EnableSsl = false;
        using var response = (FtpWebResponse)await request.GetResponseAsync();
        return Succeeded(string.Format(L["Settings:StorageTestFtpOk"], response.StatusDescription?.Trim()));
    }

    private static StorageConnectionTestResultDto Succeeded(string message) =>
        new() { Success = true, Message = message };

    private static StorageConnectionTestResultDto Failed(string message) =>
        new() { Success = false, Message = message };

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}

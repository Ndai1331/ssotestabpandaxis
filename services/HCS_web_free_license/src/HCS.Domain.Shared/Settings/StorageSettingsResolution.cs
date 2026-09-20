using System;

namespace HCS.Settings;

public enum StorageProviderKind
{
    Minio = 0,
    AmazonS3 = 1,
    Ftp = 2
}

public static class StorageSettingDefaults
{
    public const string Provider = "Minio";
    public const string EndPoint = "localhost:9000";
    public const int FtpPort = 21;
}

public sealed class StorageSettingValues
{
    public string? Provider { get; init; }
    public string? EndPoint { get; init; }
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public string? WithSsl { get; init; }
    public string? CreateBucketIfNotExists { get; init; }
    public string? Region { get; init; }
    public string? FtpHost { get; init; }
    public string? FtpPort { get; init; }
    public string? FtpUser { get; init; }
    public string? FtpPassword { get; init; }
    public string? FtpPassive { get; init; }
    public string? FtpBasePath { get; init; }
    public string? Revision { get; init; }
}

public sealed class StorageResolvedSettings
{
    public StorageProviderKind Provider { get; init; } = StorageProviderKind.Minio;
    public string EndPoint { get; init; } = StorageSettingDefaults.EndPoint;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool WithSsl { get; init; }
    public bool CreateBucketIfNotExists { get; init; } = true;
    public string Region { get; init; } = string.Empty;
    public string FtpHost { get; init; } = string.Empty;
    public int FtpPort { get; init; } = StorageSettingDefaults.FtpPort;
    public string FtpUser { get; init; } = string.Empty;
    public string FtpPassword { get; init; } = string.Empty;
    public bool FtpPassive { get; init; } = true;
    public string FtpBasePath { get; init; } = string.Empty;
    public bool HasAccessKey { get; init; }
    public bool HasSecretKey { get; init; }
    public bool HasFtpPassword { get; init; }
    public string Revision { get; init; } = "0";

    public string BlobEndPoint =>
        Provider == StorageProviderKind.AmazonS3
        && string.IsNullOrWhiteSpace(EndPoint)
        && !string.IsNullOrWhiteSpace(Region)
            ? $"s3.{Region.Trim()}.amazonaws.com"
            : EndPoint;
}

public static class StorageSettingsResolution
{
    public static bool ShouldReplaceSecret(string? incoming) => !string.IsNullOrWhiteSpace(incoming);

    public static string NextRevision(string? current) =>
        long.TryParse(current, out var value) && value >= 0
            ? (value + 1).ToString()
            : "1";

    public static StorageProviderKind ParseProvider(string? value)
    {
        if (string.Equals(value, "AmazonS3", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "S3", StringComparison.OrdinalIgnoreCase))
        {
            return StorageProviderKind.AmazonS3;
        }

        if (string.Equals(value, "Ftp", StringComparison.OrdinalIgnoreCase))
        {
            return StorageProviderKind.Ftp;
        }

        return StorageProviderKind.Minio;
    }

    public static StorageResolvedSettings Resolve(StorageSettingValues stored, StorageSettingValues fallback)
    {
        stored ??= new StorageSettingValues();
        fallback ??= new StorageSettingValues();

        var accessKey = First(stored.AccessKey, fallback.AccessKey);
        var secretKey = First(stored.SecretKey, fallback.SecretKey);
        var ftpPassword = First(stored.FtpPassword, fallback.FtpPassword);
        var provider = ParseProvider(First(stored.Provider, fallback.Provider, StorageSettingDefaults.Provider));
        var endPoint = First(stored.EndPoint, fallback.EndPoint);
        if (string.IsNullOrWhiteSpace(endPoint) && provider != StorageProviderKind.AmazonS3)
        {
            endPoint = StorageSettingDefaults.EndPoint;
        }

        return new StorageResolvedSettings
        {
            Provider = provider,
            EndPoint = endPoint,
            AccessKey = accessKey,
            SecretKey = secretKey,
            WithSsl = ParseBool(stored.WithSsl, ParseBool(fallback.WithSsl, provider == StorageProviderKind.AmazonS3)),
            CreateBucketIfNotExists = ParseBool(
                stored.CreateBucketIfNotExists,
                ParseBool(fallback.CreateBucketIfNotExists, defaultValue: true)),
            Region = First(stored.Region, fallback.Region),
            FtpHost = First(stored.FtpHost, fallback.FtpHost),
            FtpPort = ParseInt(First(stored.FtpPort, fallback.FtpPort), StorageSettingDefaults.FtpPort),
            FtpUser = First(stored.FtpUser, fallback.FtpUser),
            FtpPassword = ftpPassword,
            FtpPassive = ParseBool(stored.FtpPassive, ParseBool(fallback.FtpPassive, defaultValue: true)),
            FtpBasePath = First(stored.FtpBasePath, fallback.FtpBasePath),
            HasAccessKey = !string.IsNullOrWhiteSpace(accessKey),
            HasSecretKey = !string.IsNullOrWhiteSpace(secretKey),
            HasFtpPassword = !string.IsNullOrWhiteSpace(ftpPassword),
            Revision = First(stored.Revision, "0")
        };
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;

    private static string First(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}

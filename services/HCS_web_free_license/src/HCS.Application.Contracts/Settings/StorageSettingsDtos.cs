using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace HCS.Settings;

public interface IStorageSettingsAppService : IApplicationService
{
    Task<StorageSettingsDto> GetAsync();
    Task UpdateAsync(UpdateStorageSettingsDto input);
    Task<StorageConnectionTestResultDto> TestAsync(UpdateStorageSettingsDto input);
}

public sealed class StorageSettingsDto
{
    public string Provider { get; set; } = StorageSettingDefaults.Provider;
    public string EndPoint { get; set; } = string.Empty;
    public bool HasAccessKey { get; set; }
    public bool HasSecretKey { get; set; }
    public bool WithSsl { get; set; }
    public bool CreateBucketIfNotExists { get; set; } = true;
    public string Region { get; set; } = string.Empty;
    public string FtpHost { get; set; } = string.Empty;
    public int FtpPort { get; set; } = StorageSettingDefaults.FtpPort;
    public string FtpUser { get; set; } = string.Empty;
    public bool HasFtpPassword { get; set; }
    public bool FtpPassive { get; set; } = true;
    public string FtpBasePath { get; set; } = string.Empty;
}

public sealed class UpdateStorageSettingsDto
{
    [StringLength(32)]
    public string? Provider { get; set; }

    [StringLength(256)]
    public string? EndPoint { get; set; }

    [StringLength(256)]
    public string? AccessKey { get; set; }

    [StringLength(256)]
    public string? SecretKey { get; set; }

    public bool WithSsl { get; set; }

    public bool CreateBucketIfNotExists { get; set; } = true;

    [StringLength(64)]
    public string? Region { get; set; }

    [StringLength(256)]
    public string? FtpHost { get; set; }

    public int FtpPort { get; set; } = StorageSettingDefaults.FtpPort;

    [StringLength(128)]
    public string? FtpUser { get; set; }

    [StringLength(256)]
    public string? FtpPassword { get; set; }

    public bool FtpPassive { get; set; } = true;

    [StringLength(256)]
    public string? FtpBasePath { get; set; }
}

public sealed class StorageConnectionTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

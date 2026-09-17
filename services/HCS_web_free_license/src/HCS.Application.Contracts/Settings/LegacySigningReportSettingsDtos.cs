using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace HCS.Settings;

public interface ILegacySigningReportSettingsAppService : IApplicationService
{
    Task<LegacySigningReportSettingsDto> GetAsync();
    Task UpdateAsync(UpdateLegacySigningReportSettingsDto input);
    Task<LegacySigningReportConnectionTestResultDto> TestAsync(UpdateLegacySigningReportSettingsDto input);
}

public sealed class LegacySigningReportSettingsDto
{
    public bool Enabled { get; set; } = true;
    public bool HasConnectionString { get; set; }
    public string? ConnectionString { get; set; }
}

public sealed class UpdateLegacySigningReportSettingsDto
{
    public bool Enabled { get; set; } = true;
    public string? ConnectionString { get; set; }
}

public sealed class LegacySigningReportConnectionTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

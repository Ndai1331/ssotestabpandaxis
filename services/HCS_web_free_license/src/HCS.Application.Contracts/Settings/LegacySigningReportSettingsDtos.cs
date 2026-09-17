using System.ComponentModel.DataAnnotations;
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
    public bool HasConnectionString { get; set; }
    public string? ConnectionString { get; set; }
}

public sealed class UpdateLegacySigningReportSettingsDto
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class LegacySigningReportConnectionTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

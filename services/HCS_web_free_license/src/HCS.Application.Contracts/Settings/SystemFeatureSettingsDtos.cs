using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace HCS.Settings;

public interface ISystemFeatureSettingsAppService : IApplicationService
{
    Task<SystemFeatureSettingsDto> GetAsync();
    Task UpdateGeneralAsync(UpdateGeneralSettingsDto input);
}

public sealed class SystemFeatureSettingsDto
{
    public bool AllowSigningFromDocuments { get; set; } = true;
    public bool EnableProposalStatistics { get; set; } = true;
    public int ChatAttachmentMaxMegabytes { get; set; } = HCSSettings.ChatAttachmentMaxMegabytesDefault;
}

public sealed class UpdateGeneralSettingsDto
{
    public bool AllowSigningFromDocuments { get; set; } = true;
    public int ChatAttachmentMaxMegabytes { get; set; } = HCSSettings.ChatAttachmentMaxMegabytesDefault;
}

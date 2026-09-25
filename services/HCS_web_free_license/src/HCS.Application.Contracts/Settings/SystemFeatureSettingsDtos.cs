using System.Threading.Tasks;
using HCS.Coding;
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
    public bool ShowDocumentUrgency { get; set; } = true;
    public bool ShowDocumentConfidentiality { get; set; } = true;
    public bool EnableProposalStatistics { get; set; } = true;
    public int ChatAttachmentMaxMegabytes { get; set; } = HCSSettings.ChatAttachmentMaxMegabytesDefault;
    public string ProjectCodePrefix { get; set; } = AutoCode.Defaults.Project;
    public string TaskCodePrefix { get; set; } = AutoCode.Defaults.Task;
    public string DocumentCodePrefix { get; set; } = AutoCode.Defaults.Document;
    public string PersonalDocumentCodePrefix { get; set; } = AutoCode.Defaults.PersonalDocument;
    public string ArchiveNumberPrefix { get; set; } = AutoCode.Defaults.Archive;
    public string PersonalArchiveNumberPrefix { get; set; } = AutoCode.Defaults.PersonalArchive;
    public string WorkflowCodePrefix { get; set; } = AutoCode.Defaults.Workflow;
    public string CatalogCodePrefix { get; set; } = AutoCode.Defaults.Catalog;
}

public sealed class UpdateGeneralSettingsDto
{
    public bool AllowSigningFromDocuments { get; set; } = true;
    public bool ShowDocumentUrgency { get; set; } = true;
    public bool ShowDocumentConfidentiality { get; set; } = true;
    public int ChatAttachmentMaxMegabytes { get; set; } = HCSSettings.ChatAttachmentMaxMegabytesDefault;
    public string? ProjectCodePrefix { get; set; }
    public string? TaskCodePrefix { get; set; }
    public string? DocumentCodePrefix { get; set; }
    public string? PersonalDocumentCodePrefix { get; set; }
    public string? ArchiveNumberPrefix { get; set; }
    public string? PersonalArchiveNumberPrefix { get; set; }
    public string? WorkflowCodePrefix { get; set; }
    public string? CatalogCodePrefix { get; set; }
}

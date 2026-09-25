using System.Threading.Tasks;
using HCS.Coding;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.SettingManagement;

namespace HCS.Settings;

[RemoteService(IsEnabled = false)]
[Authorize]
public class SystemFeatureSettingsAppService(
    ISettingManager settingManager) : HCSAppService, ISystemFeatureSettingsAppService
{
    public async Task<SystemFeatureSettingsDto> GetAsync()
    {
        var allowSigning = await settingManager.GetOrNullGlobalAsync(HCSSettings.AllowSigningFromDocuments);
        var showUrgency = await settingManager.GetOrNullGlobalAsync(HCSSettings.ShowDocumentUrgency);
        var showConfidentiality = await settingManager.GetOrNullGlobalAsync(HCSSettings.ShowDocumentConfidentiality);
        var enableReport = await settingManager.GetOrNullGlobalAsync(HCSSettings.LegacySigningReportEnabled);
        var chatMax = await settingManager.GetOrNullGlobalAsync(HCSSettings.ChatAttachmentMaxMegabytes);
        return new SystemFeatureSettingsDto
        {
            AllowSigningFromDocuments = HCSSettings.IsEnabledOrDefault(allowSigning),
            ShowDocumentUrgency = HCSSettings.IsEnabledOrDefault(showUrgency),
            ShowDocumentConfidentiality = HCSSettings.IsEnabledOrDefault(showConfidentiality),
            EnableProposalStatistics = HCSSettings.IsEnabledOrDefault(enableReport),
            ChatAttachmentMaxMegabytes = HCSSettings.ParseChatAttachmentMaxMegabytes(chatMax),
            ProjectCodePrefix = await ReadPrefixAsync(AutoCodeKind.Project),
            TaskCodePrefix = await ReadPrefixAsync(AutoCodeKind.Task),
            DocumentCodePrefix = await ReadPrefixAsync(AutoCodeKind.Document),
            PersonalDocumentCodePrefix = await ReadPrefixAsync(AutoCodeKind.PersonalDocument),
            ArchiveNumberPrefix = await ReadPrefixAsync(AutoCodeKind.Archive),
            PersonalArchiveNumberPrefix = await ReadPrefixAsync(AutoCodeKind.PersonalArchive),
            WorkflowCodePrefix = await ReadPrefixAsync(AutoCodeKind.Workflow),
            CatalogCodePrefix = await ReadPrefixAsync(AutoCodeKind.Catalog)
        };
    }

    [Authorize(HCSPermissions.SystemBranding.Update)]
    public async Task UpdateGeneralAsync(UpdateGeneralSettingsDto input)
    {
        await settingManager.SetGlobalAsync(
            HCSSettings.AllowSigningFromDocuments,
            input.AllowSigningFromDocuments ? "true" : "false");
        await settingManager.SetGlobalAsync(
            HCSSettings.ShowDocumentUrgency,
            input.ShowDocumentUrgency ? "true" : "false");
        await settingManager.SetGlobalAsync(
            HCSSettings.ShowDocumentConfidentiality,
            input.ShowDocumentConfidentiality ? "true" : "false");
        await settingManager.SetGlobalAsync(
            HCSSettings.ChatAttachmentMaxMegabytes,
            HCSSettings.ClampChatAttachmentMaxMegabytes(input.ChatAttachmentMaxMegabytes).ToString());
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodeProjectPrefix,
            HCSSettings.ParseAutoCodePrefix(input.ProjectCodePrefix, AutoCodeKind.Project));
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodeTaskPrefix,
            HCSSettings.ParseAutoCodePrefix(input.TaskCodePrefix, AutoCodeKind.Task));
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodeDocumentPrefix,
            HCSSettings.ParseAutoCodePrefix(input.DocumentCodePrefix, AutoCodeKind.Document));
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodePersonalDocumentPrefix,
            HCSSettings.ParseAutoCodePrefix(input.PersonalDocumentCodePrefix, AutoCodeKind.PersonalDocument));
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodeArchivePrefix,
            HCSSettings.ParseAutoCodePrefix(input.ArchiveNumberPrefix, AutoCodeKind.Archive));
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodePersonalArchivePrefix,
            HCSSettings.ParseAutoCodePrefix(input.PersonalArchiveNumberPrefix, AutoCodeKind.PersonalArchive));
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodeWorkflowPrefix,
            HCSSettings.ParseAutoCodePrefix(input.WorkflowCodePrefix, AutoCodeKind.Workflow));
        await settingManager.SetGlobalAsync(
            HCSSettings.AutoCodeCatalogPrefix,
            HCSSettings.ParseAutoCodePrefix(input.CatalogCodePrefix, AutoCodeKind.Catalog));
    }

    private async Task<string> ReadPrefixAsync(AutoCodeKind kind)
    {
        var stored = await settingManager.GetOrNullGlobalAsync(HCSSettings.SettingName(kind));
        return HCSSettings.ParseAutoCodePrefix(stored, kind);
    }
}

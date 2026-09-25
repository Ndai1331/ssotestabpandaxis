using System;
using System.Threading;
using System.Threading.Tasks;
using HCS.Coding;
using HCS.Settings;

namespace HCS.Blazor.Client.Settings;

public sealed class SystemFeatureState(SystemFeatureSettingsClient client) : IAsyncDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private Task? pollingTask;
    private bool loaded;

    public event EventHandler? Changed;

    public bool AllowSigningFromDocuments { get; private set; } = true;
    public bool ShowDocumentUrgency { get; private set; } = true;
    public bool ShowDocumentConfidentiality { get; private set; } = true;
    public bool EnableProposalStatistics { get; private set; } = true;
    public int ChatAttachmentMaxMegabytes { get; private set; } = HCSSettings.ChatAttachmentMaxMegabytesDefault;
    public long ChatAttachmentMaxBytes => HCSSettings.ChatAttachmentMaxBytes(ChatAttachmentMaxMegabytes);
    public string ProjectCodePrefix { get; private set; } = AutoCode.Defaults.Project;
    public string TaskCodePrefix { get; private set; } = AutoCode.Defaults.Task;
    public string DocumentCodePrefix { get; private set; } = AutoCode.Defaults.Document;
    public string PersonalDocumentCodePrefix { get; private set; } = AutoCode.Defaults.PersonalDocument;
    public string ArchiveNumberPrefix { get; private set; } = AutoCode.Defaults.Archive;
    public string PersonalArchiveNumberPrefix { get; private set; } = AutoCode.Defaults.PersonalArchive;
    public string WorkflowCodePrefix { get; private set; } = AutoCode.Defaults.Workflow;
    public string CatalogCodePrefix { get; private set; } = AutoCode.Defaults.Catalog;

    public string Prefix(AutoCodeKind kind) => kind switch
    {
        AutoCodeKind.Project => ProjectCodePrefix,
        AutoCodeKind.Task => TaskCodePrefix,
        AutoCodeKind.Document => DocumentCodePrefix,
        AutoCodeKind.PersonalDocument => PersonalDocumentCodePrefix,
        AutoCodeKind.Archive => ArchiveNumberPrefix,
        AutoCodeKind.PersonalArchive => PersonalArchiveNumberPrefix,
        AutoCodeKind.Workflow => WorkflowCodePrefix,
        AutoCodeKind.Catalog => CatalogCodePrefix,
        _ => AutoCode.DefaultPrefix(kind)
    };

    public string Preview(AutoCodeKind kind) => AutoCode.Preview(Prefix(kind));

    public SystemFeatureSettingsDto Snapshot(bool? proposalStatistics = null) => new()
    {
        AllowSigningFromDocuments = AllowSigningFromDocuments,
        ShowDocumentUrgency = ShowDocumentUrgency,
        ShowDocumentConfidentiality = ShowDocumentConfidentiality,
        EnableProposalStatistics = proposalStatistics ?? EnableProposalStatistics,
        ChatAttachmentMaxMegabytes = ChatAttachmentMaxMegabytes,
        ProjectCodePrefix = ProjectCodePrefix,
        TaskCodePrefix = TaskCodePrefix,
        DocumentCodePrefix = DocumentCodePrefix,
        PersonalDocumentCodePrefix = PersonalDocumentCodePrefix,
        ArchiveNumberPrefix = ArchiveNumberPrefix,
        PersonalArchiveNumberPrefix = PersonalArchiveNumberPrefix,
        WorkflowCodePrefix = WorkflowCodePrefix,
        CatalogCodePrefix = CatalogCodePrefix
    };

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (!loaded)
        {
            await RefreshAsync(cancellationToken);
            loaded = true;
        }

        pollingTask ??= PollAsync();
    }

    public void Apply(SystemFeatureSettingsDto snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        var chatMax = HCSSettings.ClampChatAttachmentMaxMegabytes(snapshot.ChatAttachmentMaxMegabytes);
        var project = HCSSettings.ParseAutoCodePrefix(snapshot.ProjectCodePrefix, AutoCodeKind.Project);
        var task = HCSSettings.ParseAutoCodePrefix(snapshot.TaskCodePrefix, AutoCodeKind.Task);
        var document = HCSSettings.ParseAutoCodePrefix(snapshot.DocumentCodePrefix, AutoCodeKind.Document);
        var personalDocument = HCSSettings.ParseAutoCodePrefix(snapshot.PersonalDocumentCodePrefix, AutoCodeKind.PersonalDocument);
        var archive = HCSSettings.ParseAutoCodePrefix(snapshot.ArchiveNumberPrefix, AutoCodeKind.Archive);
        var personalArchive = HCSSettings.ParseAutoCodePrefix(snapshot.PersonalArchiveNumberPrefix, AutoCodeKind.PersonalArchive);
        var workflow = HCSSettings.ParseAutoCodePrefix(snapshot.WorkflowCodePrefix, AutoCodeKind.Workflow);
        var catalog = HCSSettings.ParseAutoCodePrefix(snapshot.CatalogCodePrefix, AutoCodeKind.Catalog);
        if (snapshot.AllowSigningFromDocuments == AllowSigningFromDocuments &&
            snapshot.ShowDocumentUrgency == ShowDocumentUrgency &&
            snapshot.ShowDocumentConfidentiality == ShowDocumentConfidentiality &&
            snapshot.EnableProposalStatistics == EnableProposalStatistics &&
            chatMax == ChatAttachmentMaxMegabytes &&
            project == ProjectCodePrefix &&
            task == TaskCodePrefix &&
            document == DocumentCodePrefix &&
            personalDocument == PersonalDocumentCodePrefix &&
            archive == ArchiveNumberPrefix &&
            personalArchive == PersonalArchiveNumberPrefix &&
            workflow == WorkflowCodePrefix &&
            catalog == CatalogCodePrefix)
        {
            return;
        }

        AllowSigningFromDocuments = snapshot.AllowSigningFromDocuments;
        ShowDocumentUrgency = snapshot.ShowDocumentUrgency;
        ShowDocumentConfidentiality = snapshot.ShowDocumentConfidentiality;
        EnableProposalStatistics = snapshot.EnableProposalStatistics;
        ChatAttachmentMaxMegabytes = chatMax;
        ProjectCodePrefix = project;
        TaskCodePrefix = task;
        DocumentCodePrefix = document;
        PersonalDocumentCodePrefix = personalDocument;
        ArchiveNumberPrefix = archive;
        PersonalArchiveNumberPrefix = personalArchive;
        WorkflowCodePrefix = workflow;
        CatalogCodePrefix = catalog;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Apply(await client.GetAsync(cancellationToken));
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            // Keep the last known flags when the endpoint is unavailable.
        }
    }

    public ValueTask DisposeAsync()
    {
        lifetime.Cancel();
        lifetime.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task PollAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(45));
        try
        {
            while (await timer.WaitForNextTickAsync(lifetime.Token))
            {
                await RefreshAsync(lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
    }
}

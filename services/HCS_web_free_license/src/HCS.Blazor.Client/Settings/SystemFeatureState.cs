using System;
using System.Threading;
using System.Threading.Tasks;
using HCS.Settings;

namespace HCS.Blazor.Client.Settings;

public sealed class SystemFeatureState(SystemFeatureSettingsClient client) : IAsyncDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private Task? pollingTask;
    private bool loaded;

    public event EventHandler? Changed;

    public bool AllowSigningFromDocuments { get; private set; } = true;
    public bool EnableProposalStatistics { get; private set; } = true;
    public int ChatAttachmentMaxMegabytes { get; private set; } = HCSSettings.ChatAttachmentMaxMegabytesDefault;
    public long ChatAttachmentMaxBytes => HCSSettings.ChatAttachmentMaxBytes(ChatAttachmentMaxMegabytes);

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
        if (snapshot.AllowSigningFromDocuments == AllowSigningFromDocuments &&
            snapshot.EnableProposalStatistics == EnableProposalStatistics &&
            chatMax == ChatAttachmentMaxMegabytes)
        {
            return;
        }

        AllowSigningFromDocuments = snapshot.AllowSigningFromDocuments;
        EnableProposalStatistics = snapshot.EnableProposalStatistics;
        ChatAttachmentMaxMegabytes = chatMax;
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

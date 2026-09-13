using System;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Navigation;
using HCS.Branding;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;

namespace HCS.Blazor.Client.Branding;

public sealed class SystemBrandingState(
    SystemBrandingClient client,
    IConfiguration configuration,
    IJSRuntime js) : IAsyncDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private Task? pollingTask;
    private bool loaded;

    public event EventHandler? Changed;

    public SystemBrandingDto Current { get; private set; } = new();

    public string Title => string.IsNullOrWhiteSpace(Current.Title)
        ? SystemBrandingDefaults.Title
        : Current.Title;

    public string Description => string.IsNullOrWhiteSpace(Current.Description)
        ? SystemBrandingDefaults.Description
        : Current.Description;

    public string LogoUrl => ResolveAssetUrl(Current.Logo?.Url) ?? "/images/logo/logo.png";
    public string FaviconUrl => ResolveAssetUrl(Current.Favicon?.Url) ?? "/favicon.ico";
    public string? BackgroundUrl => ResolveAssetUrl(Current.Background?.Url);

    public string BackgroundStyle => BackgroundUrl is null
        ? string.Empty
        : $"--hcs-branding-background-image: url('{BackgroundUrl}')";

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (!loaded)
        {
            await RefreshAsync(cancellationToken);
            loaded = true;
        }

        pollingTask ??= PollAsync();
    }

    public async Task ApplyAsync(SystemBrandingDto snapshot)
    {
        if (snapshot is null || IsSame(snapshot, Current))
        {
            return;
        }

        Current = snapshot;
        await ApplyBrowserBrandingAsync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await client.GetPublicAsync(cancellationToken);
            if (!IsSame(snapshot, Current))
            {
                Current = snapshot;
                await ApplyBrowserBrandingAsync();
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            // Keep the built-in fallback branding when the public endpoint is unavailable.
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

    private string? ResolveAssetUrl(string? resourceUrl)
    {
        if (string.IsNullOrWhiteSpace(resourceUrl))
        {
            return null;
        }

        return Uri.TryCreate(resourceUrl, UriKind.Absolute, out _)
            ? resourceUrl
            : GatewayResourceUrlBuilder.Build(configuration, resourceUrl);
    }

    private async Task ApplyBrowserBrandingAsync()
    {
        try
        {
            await js.InvokeVoidAsync(
                "hcsApplySystemBranding",
                Title,
                Description,
                LogoUrl,
                FaviconUrl,
                BackgroundUrl);
        }
        catch (JSException)
        {
            // The state is also rendered by Blazor; JS is only an enhancement for document head/boot UI.
        }
        catch (InvalidOperationException)
        {
            // JS interop can be unavailable during prerendering.
        }
    }

    private static bool IsSame(SystemBrandingDto left, SystemBrandingDto right) =>
        left.Revision == right.Revision &&
        string.Equals(left.Title, right.Title, StringComparison.Ordinal) &&
        string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
        string.Equals(left.Logo?.Url, right.Logo?.Url, StringComparison.Ordinal) &&
        string.Equals(left.Favicon?.Url, right.Favicon?.Url, StringComparison.Ordinal) &&
        string.Equals(left.Background?.Url, right.Background?.Url, StringComparison.Ordinal);
}

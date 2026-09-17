using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Logging;
using HCS.Blazor.Client.Services;
using HCS.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HCS.Blazor.Client.Pages;

public partial class ServiceLogs : HCSComponentBase, IDisposable
{
    private readonly List<ServiceLogDto> rows = [];
    private readonly HashSet<string> selectedApplications = new(StringComparer.Ordinal);
    private readonly HashSet<string> selectedLevels = new(StringComparer.Ordinal)
    {
        HcsServiceLogCatalog.Levels.Error,
        HcsServiceLogCatalog.Levels.Warning
    };

    private CancellationTokenSource? loadCancellation;
    private CancellationTokenSource? refreshCancellation;
    private ElementReference detailModal;
    private ServiceLogDto? selected;
    private bool isLoading;
    private bool allServices = true;
    private bool autoRefresh = true;
    private bool focusDetail;
    private string filter = "";
    private string? errorMessage;
    private DateTimeOffset? lastUpdated;

    [Inject] private ServiceLogClient LogClient { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        refreshCancellation = new CancellationTokenSource();
        _ = RunRefreshLoopAsync(refreshCancellation.Token);
    }

    private async Task RunRefreshLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                if (autoRefresh && selected is null)
                {
                    await InvokeAsync(() => LoadAsync(silent: true));
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private async Task LoadAsync(bool silent = false)
    {
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        loadCancellation = new CancellationTokenSource();
        var token = loadCancellation.Token;
        if (!silent || rows.Count == 0)
        {
            isLoading = true;
        }

        if (!silent)
        {
            errorMessage = null;
        }

        try
        {
            var result = await LogClient.GetListAsync(BuildInput(), token);
            if (token.IsCancellationRequested) return;
            rows.Clear();
            rows.AddRange(result.Items);
            lastUpdated = DateTimeOffset.Now;
            errorMessage = null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (token.IsCancellationRequested) return;
            if (exception is BffApiException { StatusCode: HttpStatusCode.Unauthorized })
            {
                await NotifyErrorAsync(exception);
                return;
            }

            if (!silent || rows.Count == 0)
            {
                rows.Clear();
                errorMessage = MapBffError(exception);
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                isLoading = false;
                if (silent)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
    }

    private Task SearchAsync() => LoadAsync();

    private Task OnKeywordKeyDown(KeyboardEventArgs args) =>
        args.Key == "Enter" ? SearchAsync() : Task.CompletedTask;

    private async Task ResetAsync()
    {
        filter = "";
        allServices = true;
        selectedApplications.Clear();
        selectedLevels.Clear();
        selectedLevels.Add(HcsServiceLogCatalog.Levels.Error);
        selectedLevels.Add(HcsServiceLogCatalog.Levels.Warning);
        selected = null;
        await LoadAsync();
    }

    private async Task OnAllServicesChanged(ChangeEventArgs args)
    {
        allServices = args.Value is true;
        if (allServices)
        {
            selectedApplications.Clear();
        }

        await LoadAsync();
    }

    private async Task OnServiceChanged(string application, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            selectedApplications.Add(application);
        }
        else
        {
            selectedApplications.Remove(application);
        }

        allServices = selectedApplications.Count == 0;
        await LoadAsync();
    }

    private async Task ToggleLevelAsync(string level)
    {
        if (!selectedLevels.Add(level))
        {
            selectedLevels.Remove(level);
        }

        if (selectedLevels.Count == 0)
        {
            selectedLevels.Add(HcsServiceLogCatalog.Levels.Error);
            selectedLevels.Add(HcsServiceLogCatalog.Levels.Warning);
        }

        await LoadAsync();
    }

    private Task OnAutoRefreshChanged(ChangeEventArgs args)
    {
        autoRefresh = args.Value is true;
        return Task.CompletedTask;
    }

    private void OpenDetail(ServiceLogDto row)
    {
        selected = row;
        focusDetail = true;
    }

    private void CloseDetail()
    {
        selected = null;
        focusDetail = false;
    }

    private void HandleDialogKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape") CloseDetail();
    }

    private GetServiceLogsInput BuildInput() => new()
    {
        Filter = filter,
        Applications = allServices ? [] : selectedApplications.ToList(),
        Levels = selectedLevels.ToList(),
        MaxResultCount = 100
    };

    private string AppLabel(string application) => application switch
    {
        HcsServiceLogCatalog.Applications.AuthServer => L["ServiceLogs:AppAuthServer"].Value,
        HcsServiceLogCatalog.Applications.WebGateway => L["ServiceLogs:AppWebGateway"].Value,
        HcsServiceLogCatalog.Applications.Blazor => L["ServiceLogs:AppBlazor"].Value,
        HcsServiceLogCatalog.Applications.PlatformService => L["ServiceLogs:AppPlatform"].Value,
        HcsServiceLogCatalog.Applications.OrganizationService => L["ServiceLogs:AppOrganization"].Value,
        HcsServiceLogCatalog.Applications.DocumentService => L["ServiceLogs:AppDocument"].Value,
        HcsServiceLogCatalog.Applications.WorkManagementService => L["ServiceLogs:AppWork"].Value,
        HcsServiceLogCatalog.Applications.CollaborationService => L["ServiceLogs:AppCollaboration"].Value,
        _ => application
    };

    private string LevelLabel(string level) => level switch
    {
        HcsServiceLogCatalog.Levels.Error => L["ServiceLogs:LevelError"].Value,
        HcsServiceLogCatalog.Levels.Warning => L["ServiceLogs:LevelWarning"].Value,
        HcsServiceLogCatalog.Levels.Information => L["ServiceLogs:LevelInformation"].Value,
        _ => level
    };

    private static string FirstValue(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "—";

    private static string FormatDate(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        return local.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);
    }

    private static string IsoDate(DateTime value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string ShortService(string? value)
    {
        var text = FirstValue(value);
        var last = text.LastIndexOf('.');
        return last >= 0 && last < text.Length - 1 ? text[(last + 1)..] : text;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!focusDetail) return;
        focusDetail = false;
        await detailModal.FocusAsync();
    }

    public void Dispose()
    {
        refreshCancellation?.Cancel();
        refreshCancellation?.Dispose();
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
    }
}

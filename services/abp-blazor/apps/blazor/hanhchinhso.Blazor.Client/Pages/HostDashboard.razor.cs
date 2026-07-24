using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.Blazor.MudBlazor.Pages.Shared.AverageExecutionDurationPerDayWidget;
using Volo.Abp.AuditLogging.Blazor.MudBlazor.Pages.Shared.ErrorRateWidget;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Timing;

namespace hanhchinhso.Blazor.Client.Pages;

public partial class HostDashboard
{
    [Inject]
    public IPermissionChecker PermissionChecker { get; set; } = default!;

    protected List<BreadcrumbItem> BreadcrumbItems = new();

    protected AuditLoggingErrorRateWidgetComponent? ErrorRateWidgetComponent;

    protected AuditLoggingAverageExecutionDurationPerDayWidgetComponent? AverageExecutionDurationPerDayWidgetComponent;

    protected DateTime StartDate { get; set; }

    protected DateTime EndDate { get; set; }

    protected bool HasAuditLoggingPermission { get; set; }

    private DateRange _dateRange = new();

    protected async override Task OnInitializedAsync()
    {
        StartDate = Clock.Now.AddMonths(-1).Date;
        EndDate = Clock.Now.Date;
        _dateRange = new DateRange(StartDate, EndDate);
        HasAuditLoggingPermission = await PermissionChecker.IsGrantedAsync(AbpAuditLoggingPermissions.AuditLogs.Default);
        await SetBreadcrumbItemsAsync();
    }

    protected virtual async Task RefreshAsync()
    {
        if (HasAuditLoggingPermission)
        {
            if (ErrorRateWidgetComponent != null)
            {
                await ErrorRateWidgetComponent.RefreshAsync();
            }

            if (AverageExecutionDurationPerDayWidgetComponent != null)
            {
                await AverageExecutionDurationPerDayWidgetComponent.RefreshAsync();
            }
        }
    }

    protected virtual ValueTask SetBreadcrumbItemsAsync()
    {
        BreadcrumbItems.Add(new BreadcrumbItem(L["Dashboard"], href: null, disabled: true));
        return ValueTask.CompletedTask;
    }

    protected virtual Task OnDateRangeChangedAsync(DateRange range)
    {
        _dateRange = range;
        if (range.Start.HasValue) StartDate = range.Start.Value;
        if (range.End.HasValue) EndDate = range.End.Value;
        return Task.CompletedTask;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Blazorise;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Volo.Abp.AspNetCore.Components.Messages;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class OrganizationCatalog : System.IDisposable
{
    private static readonly int[] PageSizeOptions = [10, 20, 50, 100];

    [Inject] private OrganizationCatalogClient CatalogClient { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter, EditorRequired]
    public OrganizationCatalogKind Kind { get; set; }

    [Parameter]
    public string? MasterType { get; set; }

    private readonly List<OrganizationCatalogRow> rows = [];
    private readonly List<DepartmentCatalogDto> departmentOptions = [];
    private Modal? editModal;
    private Validations? validations;
    private OrganizationCatalogFormModel form = new();
    private readonly HashSet<Guid> isDeleting = [];
    private string? filterText;
    private string statusFilter = string.Empty;
    private string? errorMessage;
    private string errorTitleKey = "Catalog:LoadErrorTitle";
    private bool errorCanRetry;
    private bool showAdvancedFilters;
    private bool isLoading;
    private bool isSaving;
    private bool isAuthorized;
    private bool canCreate;
    private bool canUpdate;
    private bool canDelete;
    private Guid? editingId;
    private int totalCount;
    private int pageSize = 20;
    private int currentPage = 1;
    private string? loadedDefinitionKey;
    private CancellationTokenSource? activeLoadCancellation;
    private long loadVersion;

    private IReadOnlyList<int> pageSizes => PageSizeOptions;
    private CatalogPageDefinition Definition => ResolveDefinition(Kind, MasterType);
    private bool IsDepartmentLookupRequired => Kind is OrganizationCatalogKind.Department or OrganizationCatalogKind.Unit;
    private IEnumerable<DepartmentCatalogDto> ParentDepartmentOptions =>
        departmentOptions.Where(department => !editingId.HasValue || department.Id != editingId.Value);
    private string RelationColumnTitle => Kind == OrganizationCatalogKind.Unit
        ? L["Catalog:Department"].Value
        : L["Catalog:ParentDepartment"].Value;

    protected override async Task OnParametersSetAsync()
    {
        var definitionKey = $"{Kind}:{MasterType}";
        var definitionChanged = !string.Equals(loadedDefinitionKey, definitionKey, StringComparison.Ordinal);
        if (definitionChanged)
        {
            activeLoadCancellation?.Cancel();
            loadVersion++;
            loadedDefinitionKey = definitionKey;
            filterText = null;
            statusFilter = string.Empty;
            currentPage = 1;
            rows.Clear();
            totalCount = 0;
            errorMessage = null;
            errorTitleKey = "Catalog:LoadErrorTitle";
            errorCanRetry = false;
            form = NewForm();
        }

        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        await RefreshAuthorizationAsync(authenticationState.User);

        if (isAuthorized && IsDepartmentLookupRequired)
        {
            await LoadDepartmentOptionsAsync();
        }

        if (definitionChanged && isAuthorized)
        {
            await LoadPageAsync(currentPage, pageSize);
        }
    }

    private int PageCount => Math.Max(1, (int)Math.Ceiling(totalCount / (double)Math.Max(pageSize, 1)));
    private bool CanPrev => currentPage > 1;
    private bool CanNext => currentPage < PageCount;

    private Task GoFirst() { currentPage = 1; return LoadPageAsync(currentPage, pageSize); }
    private Task GoPrev() { if (!CanPrev) return Task.CompletedTask; currentPage--; return LoadPageAsync(currentPage, pageSize); }
    private Task GoNext() { if (!CanNext) return Task.CompletedTask; currentPage++; return LoadPageAsync(currentPage, pageSize); }
    private Task GoLast() { currentPage = PageCount; return LoadPageAsync(currentPage, pageSize); }

    private Task OnPageSizeChanged(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var size)) return Task.CompletedTask;
        pageSize = Math.Clamp(size, PageSizeOptions[0], PageSizeOptions[^1]);
        currentPage = 1;
        return LoadPageAsync(currentPage, pageSize);
    }

    private Task FilterAllAsync() => SetStatusFilterAsync(string.Empty);
    private Task FilterActiveAsync() => SetStatusFilterAsync("true");
    private Task FilterInactiveAsync() => SetStatusFilterAsync("false");

    private Task SetStatusFilterAsync(string value)
    {
        statusFilter = value;
        showAdvancedFilters = false;
        return SearchAsync();
    }

    public void Dispose()
    {
        activeLoadCancellation?.Cancel();
        activeLoadCancellation?.Dispose();
    }

    private void ClearError()
    {
        errorMessage = null;
        errorTitleKey = "Catalog:LoadErrorTitle";
        errorCanRetry = false;
    }

    private void SetError(string message, string titleKey, bool canRetry)
    {
        errorMessage = message;
        errorTitleKey = titleKey;
        errorCanRetry = canRetry;
    }

    private async Task SetErrorAsync(
        string message,
        string titleKey,
        bool canRetry,
        HttpStatusCode? statusCode = null)
    {
        SetError(message, titleKey, canRetry);
        await ShowErrorAsync(message, statusCode);
    }


    private void ToggleAdvancedFilters() => showAdvancedFilters = !showAdvancedFilters;

    private async Task RefreshAuthorizationAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        isAuthorized = await IsPageAuthorizedAsync(user);
        if (!isAuthorized)
        {
            canCreate = canUpdate = canDelete = false;
            return;
        }

        var manageAll = Kind == OrganizationCatalogKind.MasterData
            && (await IsGrantedAsync(user, HCSOrganizationPermissions.MasterData)
                || await IsGrantedAsync(user, HCSCatalogPermissions.MasterData));
        canCreate = manageAll || await IsGrantedAsync(user, HcsCrudPermissions.Create(Definition.Permission));
        canUpdate = manageAll || await IsGrantedAsync(user, HcsCrudPermissions.Update(Definition.Permission));
        canDelete = manageAll || await IsGrantedAsync(user, HcsCrudPermissions.Delete(Definition.Permission));
    }

    private async Task<bool> IsPageAuthorizedAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        if (await IsGrantedAsync(user, Definition.Permission))
        {
            return true;
        }

        return Kind == OrganizationCatalogKind.MasterData
            && (await IsGrantedAsync(user, HCSOrganizationPermissions.MasterData)
                || await IsGrantedAsync(user, HCSCatalogPermissions.MasterData));
    }

    private async Task<bool> IsGrantedAsync(System.Security.Claims.ClaimsPrincipal user, string permission) =>
        (await AuthorizationService.AuthorizeAsync(user, null, permission)).Succeeded;
}

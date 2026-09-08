using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.DataGrid;
using HCS.Blazor.Client.Services;
using HCS.Localization;
using HCS.Permissions;
using Microsoft.AspNetCore.Components;

namespace HCS.Blazor.Client.Pages;

public partial class AdministrationLanguageTexts
{
    [Parameter, SupplyParameterFromQuery(Name = "cultureName")]
    public string? CultureName { get; set; }

    private static readonly int[] PageSizeOptions = [10, 20, 50, 100];
    private readonly List<LanguageTextDto> rows = [];
    private DataGrid<LanguageTextDto>? dataGrid;
    private Modal? editModal;
    private LanguageTextFormModel form = new();
    private string selectedCultureName = string.Empty;
    private string? filterText;
    private string? errorMessage;
    private string? formErrorMessage;
    private string? keyError;
    private string? valueError;
    private bool isLoading;
    private bool isSaving;
    private bool isAuthorized;
    private bool canManageTexts;
    private Guid? editingId;
    private int totalCount;
    private int pageSize = 20;
    private int currentPage = 1;

    private IReadOnlyList<int> pageSizes => PageSizeOptions;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthorized = (await AuthorizationService.AuthorizeAsync(state.User, null, HCSPermissions.Languages.Default)).Succeeded;
        canManageTexts = isAuthorized && (await AuthorizationService.AuthorizeAsync(state.User, null, HCSPermissions.Languages.ManageTexts)).Succeeded;
        if (!isAuthorized) return;

        try
        {
            await LanguageState.EnsureLoadedAsync();
            SelectInitialCulture();
        }
        catch (Exception exception)
        {
            errorMessage = MapBffError(exception);
            await ShowErrorAsync(errorMessage, BffErrorMapper.GetStatusCode(exception));
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && isAuthorized && !string.IsNullOrWhiteSpace(selectedCultureName) && dataGrid is not null)
        {
            await dataGrid.Reload();
        }
    }

    private void SelectInitialCulture()
    {
        var requested = LanguageState.Languages.FirstOrDefault(x =>
            string.Equals(x.CultureName, CultureName, StringComparison.OrdinalIgnoreCase));
        selectedCultureName = requested?.CultureName ?? LanguageState.DefaultCultureName;
    }

    private async Task OnCultureChangedAsync(string value)
    {
        if (string.Equals(selectedCultureName, value, StringComparison.Ordinal)) return;
        selectedCultureName = value;
        currentPage = 1;
        rows.Clear();
        if (dataGrid is null) return;
        await dataGrid.Paginate("1");
        await dataGrid.Reload();
    }

    private async Task OnDataGridReadAsync(DataGridReadDataEventArgs<LanguageTextDto> args)
    {
        currentPage = Math.Max(1, args.Page);
        pageSize = Math.Clamp(args.PageSize, PageSizeOptions[0], PageSizeOptions[^1]);
        await LoadPageAsync(currentPage, pageSize, args.CancellationToken);
    }

    private async Task LoadPageAsync(int page, int requestedPageSize, CancellationToken cancellationToken = default)
    {
        if (!isAuthorized || string.IsNullOrWhiteSpace(selectedCultureName)) return;
        isLoading = true;
        errorMessage = null;
        try
        {
            var result = await LanguageClient.GetLanguageTextsAsync(selectedCultureName, filterText,
                (Math.Max(1, page) - 1) * requestedPageSize, requestedPageSize, cancellationToken);
            rows.Clear();
            rows.AddRange(result.Items);
            totalCount = (int)Math.Min(result.TotalCount, int.MaxValue);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            errorMessage = MapBffError(exception);
            await ShowErrorAsync(errorMessage, BffErrorMapper.GetStatusCode(exception));
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SearchAsync()
    {
        currentPage = 1;
        if (dataGrid is null) { await LoadPageAsync(1, pageSize); return; }
        await dataGrid.Paginate("1");
        await dataGrid.Reload();
    }

    private async Task ResetSearchAsync()
    {
        filterText = null;
        await SearchAsync();
    }

    private async Task RefreshAsync()
    {
        if (dataGrid is null) { await LoadPageAsync(currentPage, pageSize); return; }
        await dataGrid.Reload();
    }

    private async Task OpenCreateModalAsync()
    {
        if (!canManageTexts || string.IsNullOrWhiteSpace(selectedCultureName)) return;
        editingId = null;
        form = new LanguageTextFormModel();
        ClearFormErrors();
        if (editModal is not null) await editModal.Show();
    }

    private async Task OpenEditModalAsync(LanguageTextDto text)
    {
        if (!canManageTexts) return;
        editingId = text.Id;
        form = new LanguageTextFormModel { Name = text.Name, Value = text.Value };
        ClearFormErrors();
        if (editModal is not null) await editModal.Show();
    }

    private async Task CloseModalAsync()
    {
        if (editModal is not null) await editModal.Hide();
        editingId = null;
        form = new LanguageTextFormModel();
        ClearFormErrors();
    }

    private void ClearFormErrors()
    {
        formErrorMessage = null;
        keyError = null;
        valueError = null;
    }

    private bool ValidateForm()
    {
        var valid = true;
        if (!editingId.HasValue && string.IsNullOrWhiteSpace(form.Name))
        {
            keyError = L["LanguageTexts:KeyRequired"].Value;
            valid = false;
        }
        if (form.Value.Length > 4096)
        {
            valueError = L["LanguageTexts:ValueTooLong"].Value;
            valid = false;
        }
        return valid;
    }

    private async Task SaveAsync()
    {
        if (isSaving || !canManageTexts || string.IsNullOrWhiteSpace(selectedCultureName)) return;
        ClearFormErrors();
        if (!ValidateForm()) return;

        var wasEditing = editingId.HasValue;
        isSaving = true;
        try
        {
            if (wasEditing)
            {
                await LanguageClient.UpdateLanguageTextAsync(editingId!.Value, new UpdateLanguageTextDto { Value = form.Value });
            }
            else
            {
                await LanguageClient.CreateLanguageTextAsync(new CreateLanguageTextDto
                {
                    ResourceName = "HCS",
                    CultureName = selectedCultureName,
                    Name = form.Name.Trim(),
                    Value = form.Value
                });
            }

            await CloseModalAsync();
            await NotifySuccessAsync(wasEditing ? L["LanguageTexts:Updated"].Value : L["LanguageTexts:Created"].Value);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            formErrorMessage = MapBffError(exception, BffErrorKind.Save);
            await ShowErrorAsync(formErrorMessage, BffErrorMapper.GetStatusCode(exception));
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task DeleteAsync(LanguageTextDto text)
    {
        if (!canManageTexts) return;
        if (!await UiMessageService.Confirm(string.Format(L["LanguageTexts:DeleteConfirmation"].Value, text.Name))) return;

        try
        {
            await LanguageClient.DeleteLanguageTextAsync(text.Id);
            await NotifySuccessAsync(L["LanguageTexts:Deleted"].Value);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            errorMessage = MapBffError(exception, BffErrorKind.Delete);
            await ShowErrorAsync(errorMessage, BffErrorMapper.GetStatusCode(exception));
        }
    }

    private sealed class LanguageTextFormModel
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazorise;
using HCS.Blazor.Client.Services;
using HCS.Localization;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace HCS.Blazor.Client.Pages;

public partial class AdministrationLanguageTexts
{
    [Parameter, SupplyParameterFromQuery(Name = "cultureName")]
    public string? CultureName { get; set; }

    private static readonly int[] PageSizeOptions = [10, 20, 50, 100];
    private readonly List<LanguageTextDto> rows = [];
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

    private int PageCount => Math.Max(1, (int)Math.Ceiling(totalCount / (double)Math.Max(pageSize, 1)));
    private bool CanPrevPage => currentPage > 1;
    private bool CanNextPage => currentPage < PageCount;

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
            if (!string.IsNullOrWhiteSpace(selectedCultureName))
            {
                await LoadPageAsync(1, pageSize);
            }
        }
        catch (Exception exception)
        {
            errorMessage = MapBffError(exception);
            await ShowErrorAsync(errorMessage, BffErrorMapper.GetStatusCode(exception));
        }
    }

    private void SelectInitialCulture()
    {
        var requested = LanguageState.Languages.FirstOrDefault(x =>
            string.Equals(x.CultureName, CultureName, StringComparison.OrdinalIgnoreCase));
        selectedCultureName = requested?.CultureName ?? LanguageState.DefaultCultureName;
    }

    private async Task OnCultureChangedAsync(ChangeEventArgs args)
    {
        var value = args.Value?.ToString() ?? string.Empty;
        if (string.Equals(selectedCultureName, value, StringComparison.Ordinal)) return;
        selectedCultureName = value;
        currentPage = 1;
        rows.Clear();
        await LoadPageAsync(1, pageSize);
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
            currentPage = page;
            pageSize = requestedPageSize;
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

    private Task SearchAsync()
    {
        currentPage = 1;
        return LoadPageAsync(1, pageSize);
    }

    private Task ResetSearchAsync()
    {
        filterText = null;
        return SearchAsync();
    }

    private Task RefreshAsync() => LoadPageAsync(currentPage, pageSize);

    private Task GoFirstPage() => LoadPageAsync(1, pageSize);
    private Task GoPrevPage() => LoadPageAsync(Math.Max(1, currentPage - 1), pageSize);
    private Task GoNextPage() => LoadPageAsync(Math.Min(PageCount, currentPage + 1), pageSize);
    private Task GoLastPage() => LoadPageAsync(PageCount, pageSize);

    private Task OnPageSizeChanged(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var size)) return Task.CompletedTask;
        pageSize = Math.Clamp(size, 10, 100);
        currentPage = 1;
        return LoadPageAsync(1, pageSize);
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

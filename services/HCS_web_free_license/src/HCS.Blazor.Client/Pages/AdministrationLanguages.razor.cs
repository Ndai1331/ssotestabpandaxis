using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.DataGrid;
using HCS.Blazor.Client.Services;
using HCS.Localization;
using HCS.Permissions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace HCS.Blazor.Client.Pages;

public partial class AdministrationLanguages : IDisposable
{
    private static readonly int[] PageSizeOptions = [10, 20, 50, 100];

    private readonly List<LanguageDto> rows = [];
    private DataGrid<LanguageDto>? dataGrid;
    private Modal? editModal;
    private LanguageFormModel form = new();
    private string? filterText;
    private string statusFilter = string.Empty;
    private string? errorMessage;
    private string? formErrorMessage;
    private string? cultureError;
    private string? displayNameError;
    private bool isLoading;
    private bool isSaving;
    private bool isAuthorized;
    private bool canCreate;
    private bool canUpdate;
    private bool canDelete;
    private bool canManageTexts;
    private bool isEditingCurrentDefault;
    private Guid? editingId;
    private int totalCount;
    private int pageSize = 20;
    private int currentPage = 1;

    private IReadOnlyList<int> pageSizes => PageSizeOptions;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = state.User;
        isAuthorized = (await AuthorizationService.AuthorizeAsync(user, null, HCSPermissions.Languages.Default)).Succeeded;
        canCreate = isAuthorized && (await AuthorizationService.AuthorizeAsync(user, null, HCSPermissions.Languages.Create)).Succeeded;
        canUpdate = isAuthorized && (await AuthorizationService.AuthorizeAsync(user, null, HCSPermissions.Languages.Update)).Succeeded;
        canDelete = isAuthorized && (await AuthorizationService.AuthorizeAsync(user, null, HCSPermissions.Languages.Delete)).Succeeded;
        canManageTexts = isAuthorized && (await AuthorizationService.AuthorizeAsync(user, null, HCSPermissions.Languages.ManageTexts)).Succeeded;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && isAuthorized && dataGrid is not null)
        {
            await dataGrid.Reload();
        }
    }

    public void Dispose()
    {
    }

    private async Task OnDataGridReadAsync(DataGridReadDataEventArgs<LanguageDto> args)
    {
        currentPage = Math.Max(1, args.Page);
        pageSize = Math.Clamp(args.PageSize, PageSizeOptions[0], PageSizeOptions[^1]);
        await LoadPageAsync(currentPage, pageSize, args.CancellationToken);
    }

    private async Task LoadPageAsync(int page, int requestedPageSize, CancellationToken cancellationToken = default)
    {
        if (!isAuthorized) return;
        isLoading = true;
        errorMessage = null;
        try
        {
            bool? enabled = statusFilter switch
            {
                "true" => true,
                "false" => false,
                _ => null
            };
            var result = await LanguageClient.GetLanguagesAsync(filterText, enabled,
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
        statusFilter = string.Empty;
        await SearchAsync();
    }

    private async Task RefreshAsync()
    {
        if (dataGrid is null) { await LoadPageAsync(currentPage, pageSize); return; }
        await dataGrid.Reload();
    }

    private async Task OpenCreateModalAsync()
    {
        if (!canCreate) return;
        editingId = null;
        isEditingCurrentDefault = false;
        form = new LanguageFormModel();
        ClearFormErrors();
        if (editModal is not null) await editModal.Show();
    }

    private async Task OpenEditModalAsync(LanguageDto language)
    {
        if (!canUpdate) return;
        editingId = language.Id;
        isEditingCurrentDefault = language.IsDefault;
        form = new LanguageFormModel
        {
            CultureName = language.CultureName,
            DisplayName = language.DisplayName,
            IsEnabled = language.IsEnabled,
            IsDefault = language.IsDefault
        };
        ClearFormErrors();
        if (editModal is not null) await editModal.Show();
    }

    private async Task CloseModalAsync()
    {
        if (editModal is not null) await editModal.Hide();
        editingId = null;
        isEditingCurrentDefault = false;
        form = new LanguageFormModel();
        ClearFormErrors();
    }

    private void ClearFormErrors()
    {
        formErrorMessage = null;
        cultureError = null;
        displayNameError = null;
    }

    private Task OnEnabledChanged(bool value)
    {
        if (!form.IsDefault) form.IsEnabled = value;
        return Task.CompletedTask;
    }

    private Task OnDefaultChanged(bool value)
    {
        if (isEditingCurrentDefault) return Task.CompletedTask;
        form.IsDefault = value;
        if (value) form.IsEnabled = true;
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (isSaving || (editingId.HasValue ? !canUpdate : !canCreate)) return;
        ClearFormErrors();
        if (!ValidateForm()) return;

        var wasEditing = editingId.HasValue;
        isSaving = true;
        try
        {
            if (editingId.HasValue)
            {
                await LanguageClient.UpdateLanguageAsync(editingId.Value, new UpdateLanguageDto
                {
                    DisplayName = form.DisplayName.Trim(),
                    IsEnabled = form.IsEnabled,
                    IsDefault = form.IsDefault
                });
            }
            else
            {
                await LanguageClient.CreateLanguageAsync(new CreateLanguageDto
                {
                    CultureName = form.CultureName.Trim(),
                    DisplayName = form.DisplayName.Trim(),
                    IsEnabled = form.IsEnabled,
                    IsDefault = form.IsDefault
                });
            }

            await CloseModalAsync();
            await LanguageState.RefreshAsync();
            await NotifySuccessAsync(wasEditing ? L["Languages:Updated"].Value : L["Languages:Created"].Value);
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

    private bool ValidateForm()
    {
        var valid = true;
        if (!editingId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(form.CultureName))
            {
                cultureError = L["Languages:CultureRequired"].Value;
                valid = false;
            }
            else
            {
                try { _ = CultureInfo.GetCultureInfo(form.CultureName.Trim()); }
                catch (CultureNotFoundException) { cultureError = L["Languages:CultureInvalid"].Value; valid = false; }
            }
        }
        if (string.IsNullOrWhiteSpace(form.DisplayName))
        {
            displayNameError = L["Languages:DisplayNameRequired"].Value;
            valid = false;
        }
        if (form.IsDefault && !form.IsEnabled)
        {
            formErrorMessage = L["Languages:DefaultEnabledHint"].Value;
            valid = false;
        }
        return valid;
    }

    private async Task DeleteAsync(LanguageDto language)
    {
        if (!canDelete || language.IsDefault) return;
        var message = string.Format(L["Languages:DeleteConfirmation"].Value, language.DisplayName, language.CultureName);
        if (!await UiMessageService.Confirm(message)) return;

        try
        {
            await LanguageClient.DeleteLanguageAsync(language.Id);
            await LanguageState.RefreshAsync();
            await NotifySuccessAsync(L["Languages:Deleted"].Value);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            errorMessage = MapBffError(exception, BffErrorKind.Delete);
            await ShowErrorAsync(errorMessage, BffErrorMapper.GetStatusCode(exception));
        }
    }

    private void OpenTranslations(LanguageDto language)
    {
        if (!canManageTexts) return;
        Navigation.NavigateTo($"/administration/language-texts?cultureName={Uri.EscapeDataString(language.CultureName)}");
    }

    private sealed class LanguageFormModel
    {
        public string CultureName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool IsDefault { get; set; }
    }
}

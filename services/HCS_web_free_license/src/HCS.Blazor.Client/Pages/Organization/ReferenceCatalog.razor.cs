using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.DataGrid;
using HCS.Permissions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class ReferenceCatalog : IDisposable
{
    private static readonly int[] PageSizeOptions = [10, 20, 50, 100];

    [Inject] private ReferenceCatalogClient CatalogClient { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter, EditorRequired] public ReferenceCatalogKind Kind { get; set; }

    private readonly List<ReferenceCatalogRow> rows = [];
    private readonly List<CountryCatalogDto> countryOptions = [];
    private readonly List<ProvinceCatalogDto> provinceOptions = [];
    private readonly HashSet<Guid> isDeleting = [];
    private DataGrid<ReferenceCatalogRow>? dataGrid;
    private Modal? editModal;
    private ReferenceCatalogFormModel form = new();
    private string? filterText;
    private string? errorMessage;
    private string? validationMessage;
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
    private string ParentColumnTitle => Kind == ReferenceCatalogKind.Province
        ? L["Catalog:CountryCode"].Value
        : L["Catalog:ProvinceCode"].Value;

    protected override async Task OnParametersSetAsync()
    {
        var definitionKey = Kind.ToString();
        var definitionChanged = !string.Equals(loadedDefinitionKey, definitionKey, StringComparison.Ordinal);
        if (definitionChanged)
        {
            activeLoadCancellation?.Cancel();
            loadVersion++;
            loadedDefinitionKey = definitionKey;
            filterText = null;
            rows.Clear();
            totalCount = 0;
            errorMessage = null;
            validationMessage = null;
            form = NewForm();
        }

        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        await RefreshAuthorizationAsync(authenticationState.User);
        if (isAuthorized)
        {
            await LoadRequiredLookupsAsync();
            if (definitionChanged && dataGrid is not null) await dataGrid.Reload();
        }
    }

    public void Dispose()
    {
        activeLoadCancellation?.Cancel();
        activeLoadCancellation?.Dispose();
    }

    private ReferenceCatalogFormModel NewForm() => new();

    private async Task RefreshAuthorizationAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        isAuthorized = await AuthorizationService.AuthorizeAsync(user, null, Definition.Permission) is { Succeeded: true };
        canCreate = isAuthorized && await IsGrantedAsync(user, HcsCrudPermissions.Create(Definition.Permission));
        canUpdate = isAuthorized && await IsGrantedAsync(user, HcsCrudPermissions.Update(Definition.Permission));
        canDelete = isAuthorized && await IsGrantedAsync(user, HcsCrudPermissions.Delete(Definition.Permission));
    }

    private async Task<bool> IsGrantedAsync(System.Security.Claims.ClaimsPrincipal user, string permission) =>
        (await AuthorizationService.AuthorizeAsync(user, null, permission)).Succeeded;

    private async Task LoadRequiredLookupsAsync()
    {
        try
        {
            if (Kind == ReferenceCatalogKind.Province && countryOptions.Count == 0)
            {
                countryOptions.AddRange((await CatalogClient.GetCountryLookupAsync()).OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            }
            else if (Kind == ReferenceCatalogKind.Commune && provinceOptions.Count == 0)
            {
                provinceOptions.AddRange((await CatalogClient.GetProvinceLookupAsync()).OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
            }
        }
        catch (ReferenceCatalogApiException exception)
        {
            await ShowErrorAsync(GetFriendlyErrorMessage(exception.StatusCode), exception.StatusCode);
        }
        catch (Exception)
        {
            await ShowErrorAsync(L["Catalog:LookupError"].Value);
        }
    }

    private async Task OnDataGridReadAsync(DataGridReadDataEventArgs<ReferenceCatalogRow> args)
    {
        currentPage = Math.Max(1, args.Page);
        pageSize = Math.Clamp(args.PageSize, PageSizeOptions[0], PageSizeOptions[^1]);
        await LoadPageAsync(currentPage, pageSize, args.CancellationToken);
    }

    private async Task LoadPageAsync(int page, int requestedPageSize, CancellationToken cancellationToken = default)
    {
        if (!isAuthorized) return;

        activeLoadCancellation?.Cancel();
        activeLoadCancellation?.Dispose();
        activeLoadCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var requestToken = activeLoadCancellation.Token;
        var requestVersion = ++loadVersion;
        isLoading = true;
        errorMessage = null;
        try
        {
            var query = new ReferenceCatalogQuery(filterText,
                (Math.Max(1, page) - 1) * Math.Clamp(requestedPageSize, 1, 100),
                Math.Clamp(requestedPageSize, 1, 100));
            var loadedRows = new List<ReferenceCatalogRow>();
            long loadedTotalCount;
            switch (Kind)
            {
                case ReferenceCatalogKind.Icd10:
                    var icd = await CatalogClient.GetIcd10Async(query, requestToken);
                    loadedRows.AddRange(icd.Items.Select(Map)); loadedTotalCount = icd.TotalCount; break;
                case ReferenceCatalogKind.BloodPressure:
                    var pressure = await CatalogClient.GetBloodPressureAsync(query, requestToken);
                    loadedRows.AddRange(pressure.Items.Select(Map)); loadedTotalCount = pressure.TotalCount; break;
                case ReferenceCatalogKind.BloodGlucose:
                    var glucose = await CatalogClient.GetBloodGlucoseAsync(query, requestToken);
                    loadedRows.AddRange(glucose.Items.Select(Map)); loadedTotalCount = glucose.TotalCount; break;
                case ReferenceCatalogKind.Bmi:
                    var bmi = await CatalogClient.GetBmiAsync(query, requestToken);
                    loadedRows.AddRange(bmi.Items.Select(Map)); loadedTotalCount = bmi.TotalCount; break;
                case ReferenceCatalogKind.Country:
                    var countries = await CatalogClient.GetCountriesAsync(query, requestToken);
                    loadedRows.AddRange(countries.Items.Select(Map)); loadedTotalCount = countries.TotalCount; break;
                case ReferenceCatalogKind.Province:
                    var provinces = await CatalogClient.GetProvincesAsync(query, requestToken);
                    loadedRows.AddRange(provinces.Items.Select(Map)); loadedTotalCount = provinces.TotalCount; break;
                case ReferenceCatalogKind.Commune:
                    var communes = await CatalogClient.GetCommunesAsync(query, requestToken);
                    loadedRows.AddRange(communes.Items.Select(Map)); loadedTotalCount = communes.TotalCount; break;
                default: throw new ArgumentOutOfRangeException();
            }

            if (requestVersion == loadVersion)
            {
                rows.Clear(); rows.AddRange(loadedRows); totalCount = ToDataGridTotal(loadedTotalCount);
            }
        }
        catch (OperationCanceledException) when (requestToken.IsCancellationRequested) { }
        catch (ReferenceCatalogApiException exception)
        {
            await SetErrorAsync(GetFriendlyErrorMessage(exception.StatusCode), exception.StatusCode);
        }
        catch (Exception)
        {
            await SetErrorAsync(L["Catalog:NetworkError"].Value);
        }
        finally
        {
            if (requestVersion == loadVersion)
            {
                isLoading = false;
                activeLoadCancellation?.Dispose();
                activeLoadCancellation = null;
            }
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
        if (!canCreate) return;
        editingId = null; form = NewForm(); validationMessage = null;
        await LoadRequiredLookupsAsync();
        if (editModal is not null) await editModal.Show();
    }

    private async Task OpenEditModalAsync(ReferenceCatalogRow row)
    {
        if (!canUpdate) return;
        editingId = row.Id;
        form = new ReferenceCatalogFormModel
        {
            Code = row.Code, Name = row.Name, DiseaseGroup = row.DiseaseGroup, IsChronic = row.IsChronic,
            HATTMin = row.HATTMin, HATTMax = row.HATTMax, HATTrMin = row.HATTrMin, HATTrMax = row.HATTrMax,
            Title = row.Title, MinValue = row.MinValue, MaxValue = row.MaxValue, Description = row.Description,
            BeforeMeal = row.BeforeMeal, Gender = row.Gender, CountryCode = row.CountryCode,
            ParentId = row.ParentId?.ToString() ?? string.Empty, SortOrder = row.SortOrder
        };
        validationMessage = null;
        await LoadRequiredLookupsAsync();
        if (editModal is not null) await editModal.Show();
    }

    private async Task CloseModalAsync()
    {
        if (editModal is not null) await editModal.Hide();
        editingId = null; form = NewForm(); validationMessage = null;
    }

    private async Task SaveAsync()
    {
        if (isSaving || (editingId.HasValue ? !canUpdate : !canCreate)) return;
        validationMessage = ValidateForm();
        if (!string.IsNullOrWhiteSpace(validationMessage)) return;

        isSaving = true;
        try
        {
            var wasEditing = editingId.HasValue;
            switch (Kind)
            {
                case ReferenceCatalogKind.Icd10:
                    var icdRequest = new Icd10UpsertRequest(form.Code.Trim(), form.Name.Trim(), form.DiseaseGroup.Trim(), form.IsChronic, form.SortOrder);
                    if (editingId.HasValue) await CatalogClient.UpdateIcd10Async(editingId.Value, icdRequest); else await CatalogClient.CreateIcd10Async(icdRequest);
                    break;
                case ReferenceCatalogKind.BloodPressure:
                    var pressureRequest = new BloodPressureUpsertRequest(form.HATTMin, form.HATTMax, form.HATTrMin, form.HATTrMax, form.Title.Trim(), form.Description.Trim(), form.SortOrder);
                    if (editingId.HasValue) await CatalogClient.UpdateBloodPressureAsync(editingId.Value, pressureRequest); else await CatalogClient.CreateBloodPressureAsync(pressureRequest);
                    break;
                case ReferenceCatalogKind.BloodGlucose:
                    var glucoseRequest = new BloodGlucoseUpsertRequest(form.Title.Trim(), form.MinValue, form.MaxValue, form.Description.Trim(), form.BeforeMeal, form.SortOrder);
                    if (editingId.HasValue) await CatalogClient.UpdateBloodGlucoseAsync(editingId.Value, glucoseRequest); else await CatalogClient.CreateBloodGlucoseAsync(glucoseRequest);
                    break;
                case ReferenceCatalogKind.Bmi:
                    var bmiRequest = new BmiUpsertRequest(form.Title.Trim(), form.Gender.Trim(), form.MinValue, form.MaxValue, form.Description.Trim(), form.SortOrder);
                    if (editingId.HasValue) await CatalogClient.UpdateBmiAsync(editingId.Value, bmiRequest); else await CatalogClient.CreateBmiAsync(bmiRequest);
                    break;
                case ReferenceCatalogKind.Country:
                    var countryRequest = new CountryUpsertRequest(form.Code.Trim(), form.Name.Trim(), form.CountryCode.Trim(), form.SortOrder);
                    if (editingId.HasValue) await CatalogClient.UpdateCountryAsync(editingId.Value, countryRequest); else await CatalogClient.CreateCountryAsync(countryRequest);
                    break;
                case ReferenceCatalogKind.Province:
                    var provinceRequest = new ProvinceUpsertRequest(form.Code.Trim(), form.Name.Trim(), Guid.Parse(form.ParentId), form.SortOrder);
                    if (editingId.HasValue) await CatalogClient.UpdateProvinceAsync(editingId.Value, provinceRequest); else await CatalogClient.CreateProvinceAsync(provinceRequest);
                    break;
                case ReferenceCatalogKind.Commune:
                    var communeRequest = new CommuneUpsertRequest(form.Code.Trim(), form.Name.Trim(), Guid.Parse(form.ParentId), form.SortOrder);
                    if (editingId.HasValue) await CatalogClient.UpdateCommuneAsync(editingId.Value, communeRequest); else await CatalogClient.CreateCommuneAsync(communeRequest);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            await CloseModalAsync();
            await NotifySuccessAsync(wasEditing ? L["Catalog:Updated"].Value : L["Catalog:Created"].Value);
            await RefreshAsync();
        }
        catch (ReferenceCatalogApiException exception)
        {
            var message = GetFriendlyErrorMessage(exception.StatusCode);
            errorMessage = message;
            await ShowErrorAsync(message, exception.StatusCode);
        }
        catch (Exception)
        {
            errorMessage = L["Catalog:SaveError"].Value;
            await ShowErrorAsync(errorMessage);
        }
        finally { isSaving = false; }
    }

    private async Task DeleteAsync(ReferenceCatalogRow row)
    {
        if (!canDelete || isDeleting.Contains(row.Id)) return;
        var label = IsCodeCatalog ? row.Name : row.Title;
        if (!await UiMessageService.Confirm(string.Format(L["Catalog:DeleteConfirmation"].Value, label)) || !isDeleting.Add(row.Id)) return;
        try
        {
            await CatalogClient.DeleteAsync(Kind, row.Id);
            await NotifySuccessAsync(L["Catalog:Deleted"].Value);
            await RefreshAsync();
        }
        catch (ReferenceCatalogApiException exception)
        {
            var message = GetFriendlyErrorMessage(exception.StatusCode);
            errorMessage = message;
            await ShowErrorAsync(message, exception.StatusCode);
        }
        finally { isDeleting.Remove(row.Id); }
    }

    private string? ValidateForm()
    {
        if (IsCodeCatalog && (string.IsNullOrWhiteSpace(form.Code) || string.IsNullOrWhiteSpace(form.Name))) return L["Catalog:CodeNameRequired"].Value;
        if (Kind == ReferenceCatalogKind.Icd10 && string.IsNullOrWhiteSpace(form.DiseaseGroup)) return L["Catalog:DiseaseGroupRequired"].Value;
        if (Kind is ReferenceCatalogKind.BloodPressure or ReferenceCatalogKind.BloodGlucose or ReferenceCatalogKind.Bmi
            && string.IsNullOrWhiteSpace(form.Title)) return L["Catalog:TitleRequired"].Value;
        if (Kind == ReferenceCatalogKind.Bmi && string.IsNullOrWhiteSpace(form.Gender)) return L["Catalog:GenderRequired"].Value;
        if (Kind == ReferenceCatalogKind.BloodPressure && (form.HATTMin > form.HATTMax || form.HATTrMin > form.HATTrMax)) return L["Catalog:InvalidRange"].Value;
        if (Kind is ReferenceCatalogKind.BloodGlucose or ReferenceCatalogKind.Bmi && (form.MinValue > form.MaxValue || form.MinValue < 0)) return L["Catalog:InvalidRange"].Value;
        if (Kind is ReferenceCatalogKind.Province or ReferenceCatalogKind.Commune)
        {
            if (!Guid.TryParse(form.ParentId, out var parentId) || parentId == Guid.Empty)
                return L["Catalog:ParentRequired"].Value;
        }
        if (Kind == ReferenceCatalogKind.Country && string.IsNullOrWhiteSpace(form.CountryCode)) return L["Catalog:CountryCodeRequired"].Value;
        return null;
    }

    private async Task SetErrorAsync(string message, HttpStatusCode? statusCode = null)
    {
        errorMessage = message;
        await ShowErrorAsync(message, statusCode);
    }

    private string GetFriendlyErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => L["Catalog:Unauthorized"].Value,
        HttpStatusCode.Forbidden => L["Catalog:ForbiddenDescription"].Value,
        HttpStatusCode.BadRequest => L["Catalog:ValidationError"].Value,
        HttpStatusCode.NotFound => L["Catalog:NotFound"].Value,
        HttpStatusCode.Conflict => L["Catalog:Conflict"].Value,
        _ => L["Catalog:NetworkError"].Value
    };

    private async Task ExportCurrentPageAsync()
    {
        if (rows.Count == 0) return;
        var headers = Headers();
        var csv = new StringBuilder().AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows) csv.AppendLine(string.Join(",", Values(row).Select(EscapeCsv)));
        try
        {
            await JsRuntime.InvokeVoidAsync("hcsDownloadTextFile", $"{Definition.Title}-{DateTime.Now:yyyyMMdd-HHmm}.csv", csv.ToString(), "text/csv;charset=utf-8");
            await NotifySuccessAsync(L["Catalog:Exported"].Value);
        }
        catch (Exception) { await UiMessageService.Error(L["Catalog:ExportError"].Value); }
    }

    private List<string> Headers() => Kind switch
    {
        ReferenceCatalogKind.Icd10 => [L["Catalog:Code"].Value, L["Catalog:Name"].Value, L["Catalog:DiseaseGroup"].Value, L["Catalog:Chronic"].Value, L["Catalog:SortOrder"].Value],
        ReferenceCatalogKind.BloodPressure => [L["Catalog:HATT"].Value, L["Catalog:HATTr"].Value, L["Catalog:Title"].Value, L["Catalog:Description"].Value, L["Catalog:SortOrder"].Value],
        ReferenceCatalogKind.BloodGlucose => [L["Catalog:Title"].Value, L["Catalog:Minimum"].Value, L["Catalog:Maximum"].Value, L["Catalog:BeforeMeal"].Value, L["Catalog:Description"].Value, L["Catalog:SortOrder"].Value],
        ReferenceCatalogKind.Bmi => [L["Catalog:Title"].Value, L["Catalog:Gender"].Value, L["Catalog:Minimum"].Value, L["Catalog:Maximum"].Value, L["Catalog:Description"].Value, L["Catalog:SortOrder"].Value],
        ReferenceCatalogKind.Country => [L["Catalog:Code"].Value, L["Catalog:Name"].Value, L["Catalog:CountryCode"].Value, L["Catalog:SortOrder"].Value],
        ReferenceCatalogKind.Province => [L["Catalog:Code"].Value, L["Catalog:Name"].Value, L["Catalog:CountryCode"].Value, L["Catalog:SortOrder"].Value],
        ReferenceCatalogKind.Commune => [L["Catalog:Code"].Value, L["Catalog:Name"].Value, L["Catalog:ProvinceCode"].Value, L["Catalog:SortOrder"].Value],
        _ => []
    };

    private IEnumerable<string> Values(ReferenceCatalogRow row) => Kind switch
    {
        ReferenceCatalogKind.Icd10 => [row.Code, row.Name, row.DiseaseGroup, row.IsChronic ? L["Catalog:Yes"].Value : L["Catalog:No"].Value, row.SortOrder.ToString(CultureInfo.InvariantCulture)],
        ReferenceCatalogKind.BloodPressure => [FormatRange(row.HATTMin, row.HATTMax), FormatRange(row.HATTrMin, row.HATTrMax), row.Title, row.Description, row.SortOrder.ToString(CultureInfo.InvariantCulture)],
        ReferenceCatalogKind.BloodGlucose => [row.Title, FormatDecimal(row.MinValue), FormatDecimal(row.MaxValue), row.BeforeMeal ? L["Catalog:Yes"].Value : L["Catalog:No"].Value, row.Description, row.SortOrder.ToString(CultureInfo.InvariantCulture)],
        ReferenceCatalogKind.Bmi => [row.Title, row.Gender, FormatDecimal(row.MinValue), FormatDecimal(row.MaxValue), row.Description, row.SortOrder.ToString(CultureInfo.InvariantCulture)],
        ReferenceCatalogKind.Country => [row.Code, row.Name, row.CountryCode, row.SortOrder.ToString(CultureInfo.InvariantCulture)],
        ReferenceCatalogKind.Province => [row.Code, row.Name, row.ParentCode, row.SortOrder.ToString(CultureInfo.InvariantCulture)],
        ReferenceCatalogKind.Commune => [row.Code, row.Name, row.ParentCode, row.SortOrder.ToString(CultureInfo.InvariantCulture)],
        _ => []
    };

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string FormatRange(int min, int max) => $"{min} - {max}";
    private static string FormatDecimal(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static int ToDataGridTotal(long value) => value >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)value);

    private static ReferenceCatalogRow Map(Icd10CatalogDto x) => new(x.Id, x.Code, x.Name, x.DiseaseGroup, x.IsChronic, 0, 0, 0, 0, string.Empty, 0, 0, string.Empty, false, string.Empty, null, string.Empty, string.Empty, x.SortOrder);
    private static ReferenceCatalogRow Map(BloodPressureCatalogDto x) => new(x.Id, string.Empty, string.Empty, string.Empty, false, x.HATTMin, x.HATTMax, x.HATTrMin, x.HATTrMax, x.Title, 0, 0, x.Description, false, string.Empty, null, string.Empty, string.Empty, x.SortOrder);
    private static ReferenceCatalogRow Map(BloodGlucoseCatalogDto x) => new(x.Id, string.Empty, string.Empty, string.Empty, false, 0, 0, 0, 0, x.Title, x.MinValue, x.MaxValue, x.Description, x.BeforeMeal, string.Empty, null, string.Empty, string.Empty, x.SortOrder);
    private static ReferenceCatalogRow Map(BmiCatalogDto x) => new(x.Id, string.Empty, string.Empty, string.Empty, false, 0, 0, 0, 0, x.Title, x.MinValue, x.MaxValue, x.Description, false, x.Gender, null, string.Empty, string.Empty, x.SortOrder);
    private static ReferenceCatalogRow Map(CountryCatalogDto x) => new(x.Id, x.Code, x.Name, string.Empty, false, 0, 0, 0, 0, string.Empty, 0, 0, string.Empty, false, string.Empty, null, string.Empty, x.CountryCode, x.SortOrder);
    private static ReferenceCatalogRow Map(ProvinceCatalogDto x) => new(x.Id, x.Code, x.Name, string.Empty, false, 0, 0, 0, 0, string.Empty, 0, 0, string.Empty, false, string.Empty, x.CountryId, x.CountryCode, string.Empty, x.SortOrder);
    private static ReferenceCatalogRow Map(CommuneCatalogDto x) => new(x.Id, x.Code, x.Name, string.Empty, false, 0, 0, 0, 0, string.Empty, 0, 0, string.Empty, false, string.Empty, x.ProvinceId, x.ProvinceCode, string.Empty, x.SortOrder);
}

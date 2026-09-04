using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using Microsoft.Extensions.Http;

namespace HCS.Blazor.Client.Pages.Organization;

public sealed class ReferenceCatalogClient(IHttpClientFactory httpClientFactory)
{
    private const int MaxPageSize = 100;

    public Task<OrganizationPagedResponse<Icd10CatalogDto>> GetIcd10Async(ReferenceCatalogQuery query, CancellationToken ct = default) =>
        GetAsync<OrganizationPagedResponse<Icd10CatalogDto>>(BuildListUri(ReferenceCatalogKind.Icd10, query), ct);

    public Task<OrganizationPagedResponse<BloodPressureCatalogDto>> GetBloodPressureAsync(ReferenceCatalogQuery query, CancellationToken ct = default) =>
        GetAsync<OrganizationPagedResponse<BloodPressureCatalogDto>>(BuildListUri(ReferenceCatalogKind.BloodPressure, query), ct);

    public Task<OrganizationPagedResponse<BloodGlucoseCatalogDto>> GetBloodGlucoseAsync(ReferenceCatalogQuery query, CancellationToken ct = default) =>
        GetAsync<OrganizationPagedResponse<BloodGlucoseCatalogDto>>(BuildListUri(ReferenceCatalogKind.BloodGlucose, query), ct);

    public Task<OrganizationPagedResponse<BmiCatalogDto>> GetBmiAsync(ReferenceCatalogQuery query, CancellationToken ct = default) =>
        GetAsync<OrganizationPagedResponse<BmiCatalogDto>>(BuildListUri(ReferenceCatalogKind.Bmi, query), ct);

    public Task<OrganizationPagedResponse<CountryCatalogDto>> GetCountriesAsync(ReferenceCatalogQuery query, CancellationToken ct = default) =>
        GetAsync<OrganizationPagedResponse<CountryCatalogDto>>(BuildListUri(ReferenceCatalogKind.Country, query), ct);

    public Task<OrganizationPagedResponse<ProvinceCatalogDto>> GetProvincesAsync(ReferenceCatalogQuery query, CancellationToken ct = default) =>
        GetAsync<OrganizationPagedResponse<ProvinceCatalogDto>>(BuildListUri(ReferenceCatalogKind.Province, query), ct);

    public Task<OrganizationPagedResponse<CommuneCatalogDto>> GetCommunesAsync(ReferenceCatalogQuery query, CancellationToken ct = default) =>
        GetAsync<OrganizationPagedResponse<CommuneCatalogDto>>(BuildListUri(ReferenceCatalogKind.Commune, query), ct);

    public Task<IReadOnlyList<CountryCatalogDto>> GetCountryLookupAsync(CancellationToken ct = default) =>
        GetLookupAsync<CountryCatalogDto>(ReferenceCatalogKind.Country, ct);

    public Task<IReadOnlyList<ProvinceCatalogDto>> GetProvinceLookupAsync(CancellationToken ct = default) =>
        GetLookupAsync<ProvinceCatalogDto>(ReferenceCatalogKind.Province, ct);

    public Task<Icd10CatalogDto> CreateIcd10Async(Icd10UpsertRequest request, CancellationToken ct = default) =>
        SendAsync<Icd10CatalogDto>(HttpMethod.Post, Endpoint(ReferenceCatalogKind.Icd10), request, ct);

    public Task<Icd10CatalogDto> UpdateIcd10Async(Guid id, Icd10UpsertRequest request, CancellationToken ct = default) =>
        SendAsync<Icd10CatalogDto>(HttpMethod.Put, ItemEndpoint(ReferenceCatalogKind.Icd10, id), request, ct);

    public Task<BloodPressureCatalogDto> CreateBloodPressureAsync(BloodPressureUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<BloodPressureCatalogDto>(HttpMethod.Post, Endpoint(ReferenceCatalogKind.BloodPressure), request, ct);

    public Task<BloodPressureCatalogDto> UpdateBloodPressureAsync(Guid id, BloodPressureUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<BloodPressureCatalogDto>(HttpMethod.Put, ItemEndpoint(ReferenceCatalogKind.BloodPressure, id), request, ct);

    public Task<BloodGlucoseCatalogDto> CreateBloodGlucoseAsync(BloodGlucoseUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<BloodGlucoseCatalogDto>(HttpMethod.Post, Endpoint(ReferenceCatalogKind.BloodGlucose), request, ct);

    public Task<BloodGlucoseCatalogDto> UpdateBloodGlucoseAsync(Guid id, BloodGlucoseUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<BloodGlucoseCatalogDto>(HttpMethod.Put, ItemEndpoint(ReferenceCatalogKind.BloodGlucose, id), request, ct);

    public Task<BmiCatalogDto> CreateBmiAsync(BmiUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<BmiCatalogDto>(HttpMethod.Post, Endpoint(ReferenceCatalogKind.Bmi), request, ct);

    public Task<BmiCatalogDto> UpdateBmiAsync(Guid id, BmiUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<BmiCatalogDto>(HttpMethod.Put, ItemEndpoint(ReferenceCatalogKind.Bmi, id), request, ct);

    public Task<CountryCatalogDto> CreateCountryAsync(CountryUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<CountryCatalogDto>(HttpMethod.Post, Endpoint(ReferenceCatalogKind.Country), request, ct);

    public Task<CountryCatalogDto> UpdateCountryAsync(Guid id, CountryUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<CountryCatalogDto>(HttpMethod.Put, ItemEndpoint(ReferenceCatalogKind.Country, id), request, ct);

    public Task<ProvinceCatalogDto> CreateProvinceAsync(ProvinceUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<ProvinceCatalogDto>(HttpMethod.Post, Endpoint(ReferenceCatalogKind.Province), request, ct);

    public Task<ProvinceCatalogDto> UpdateProvinceAsync(Guid id, ProvinceUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<ProvinceCatalogDto>(HttpMethod.Put, ItemEndpoint(ReferenceCatalogKind.Province, id), request, ct);

    public Task<CommuneCatalogDto> CreateCommuneAsync(CommuneUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<CommuneCatalogDto>(HttpMethod.Post, Endpoint(ReferenceCatalogKind.Commune), request, ct);

    public Task<CommuneCatalogDto> UpdateCommuneAsync(Guid id, CommuneUpsertRequest request, CancellationToken ct = default) =>
        SendAsync<CommuneCatalogDto>(HttpMethod.Put, ItemEndpoint(ReferenceCatalogKind.Commune, id), request, ct);

    public async Task DeleteAsync(ReferenceCatalogKind kind, Guid id, CancellationToken ct = default)
    {
        using var response = await CreateClient().DeleteAsync(ItemEndpoint(kind, id), ct);
        await EnsureSuccessAsync(response, ct);
    }

    internal static string BuildListUri(ReferenceCatalogKind kind, ReferenceCatalogQuery query)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Filter))
            parameters.Add($"filter={Uri.EscapeDataString(SearchText.Normalize(query.Filter))}");
        parameters.Add($"skipCount={Math.Max(0, query.SkipCount)}");
        parameters.Add($"maxResultCount={Math.Clamp(query.MaxResultCount, 1, MaxPageSize)}");
        return $"{Endpoint(kind)}?{string.Join('&', parameters)}";
    }

    internal static string Endpoint(ReferenceCatalogKind kind) => kind switch
    {
        ReferenceCatalogKind.Icd10 => "/api/organization/icd10",
        ReferenceCatalogKind.BloodPressure => "/api/organization/blood-pressure",
        ReferenceCatalogKind.BloodGlucose => "/api/organization/blood-glucose",
        ReferenceCatalogKind.Bmi => "/api/organization/bmi",
        ReferenceCatalogKind.Country => "/api/organization/countries",
        ReferenceCatalogKind.Province => "/api/organization/provinces",
        ReferenceCatalogKind.Commune => "/api/organization/communes",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    internal static string ItemEndpoint(ReferenceCatalogKind kind, Guid id) => $"{Endpoint(kind)}/{id:D}";

    private async Task<IReadOnlyList<T>> GetLookupAsync<T>(ReferenceCatalogKind kind, CancellationToken ct)
    {
        var items = new List<T>();
        var skipCount = 0;
        long totalCount = long.MaxValue;
        while (items.Count < totalCount)
        {
            var result = await GetAsync<OrganizationPagedResponse<T>>(
                BuildListUri(kind, new ReferenceCatalogQuery(null, skipCount, MaxPageSize)), ct);
            items.AddRange(result.Items);
            totalCount = result.TotalCount;
            if (result.Items.Count == 0 || result.Items.Count < MaxPageSize) break;
            skipCount += result.Items.Count;
        }

        return items;
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken ct)
    {
        using var response = await CreateClient().GetAsync(uri, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new ReferenceCatalogApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object payload, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        using var response = await CreateClient().SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new ReferenceCatalogApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new ReferenceCatalogApiException(response.StatusCode, body);
    }

    private HttpClient CreateClient() => httpClientFactory.CreateClient("HCS.Bff");
}

internal sealed class ReferenceCatalogApiException(HttpStatusCode statusCode, string? responseBody)
    : Exception($"Reference catalog request failed with HTTP {(int)statusCode}.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
}

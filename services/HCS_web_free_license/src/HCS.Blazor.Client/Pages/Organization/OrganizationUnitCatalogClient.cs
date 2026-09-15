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
using HCS.OrganizationUnits;
using Volo.Abp.Application.Dtos;

namespace HCS.Blazor.Client.Pages.Organization;

public sealed class OrganizationUnitCatalogClient(IHttpClientFactory httpClientFactory)
{
    private const int MaxPageSize = 100;

    public Task<List<OrganizationUnitDto>> GetTreeAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<OrganizationUnitDto>>(Endpoint, cancellationToken);

    public Task<List<OrganizationUnitDto>> GetLookupAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<OrganizationUnitDto>>(LookupEndpoint, cancellationToken);

    public async Task<IReadOnlyList<DepartmentCatalogDto>> GetDepartmentCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var units = await GetLookupAsync(cancellationToken);
        return units.Select(OrganizationUnitCatalogMapper.ToDepartment).ToList();
    }

    public async Task<OrganizationPagedResponse<DepartmentCatalogDto>> SearchDepartmentsAsync(
        string? filter,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        var items = await GetDepartmentCatalogAsync(cancellationToken);
        return OrganizationUnitCatalogMapper.Search(items, filter, skipCount, maxResultCount);
    }

    public async Task<IReadOnlyList<UserDepartmentLookupDto>> GetUserDepartmentsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().Take(200).ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var rows = await GetAsync<List<UserOrganizationUnitLookupDto>>(
            BuildUserLookupUri(ids), cancellationToken);
        return rows.Select(OrganizationUnitCatalogMapper.ToUserDepartment).ToList();
    }

    public async Task<IReadOnlyList<UserDepartmentLookupDto>> GetMembersLookupAsync(
        Guid organizationUnitId,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetAsync<List<UserOrganizationUnitLookupDto>>(
            BuildMembersLookupUri(organizationUnitId), cancellationToken);
        return rows.Select(OrganizationUnitCatalogMapper.ToUserDepartment).ToList();
    }

    public Task SetUserOrganizationUnitsAsync(
        Guid userId,
        IEnumerable<Guid> organizationUnitIds,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Put,
            BuildSetUserUri(userId),
            new { organizationUnitIds = organizationUnitIds.Where(id => id != Guid.Empty).Distinct().ToArray() },
            cancellationToken);

    public Task<PagedResultDto<OrganizationUnitMemberDto>> GetMembersAsync(
        Guid id,
        string? filter,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultDto<OrganizationUnitMemberDto>>(
            BuildMembersUri(id, filter, skipCount, maxResultCount), cancellationToken);

    public Task<PagedResultDto<OrganizationUnitMemberDto>> GetAvailableMembersAsync(
        Guid id,
        string? filter,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultDto<OrganizationUnitMemberDto>>(
            BuildAvailableMembersUri(id, filter, skipCount, maxResultCount), cancellationToken);

    public Task<OrganizationUnitDto> CreateAsync(
        string displayName,
        Guid? parentId,
        CancellationToken cancellationToken = default) =>
        SendAsync<OrganizationUnitDto>(HttpMethod.Post, Endpoint, new { displayName, parentId }, cancellationToken);

    public Task<OrganizationUnitDto> UpdateAsync(
        Guid id,
        string displayName,
        CancellationToken cancellationToken = default) =>
        SendAsync<OrganizationUnitDto>(HttpMethod.Put, ItemEndpoint(id), new { displayName }, cancellationToken);

    public Task<OrganizationUnitDto> MoveAsync(
        Guid id,
        Guid? parentId,
        CancellationToken cancellationToken = default) =>
        SendAsync<OrganizationUnitDto>(HttpMethod.Post, $"{ItemEndpoint(id)}/move", new { parentId }, cancellationToken);

    public Task MoveAllMembersAsync(
        Guid id,
        Guid targetOrganizationUnitId,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"{ItemEndpoint(id)}/move-all-members", new { targetOrganizationUnitId }, cancellationToken);

    public Task AddMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"{ItemEndpoint(id)}/members", new { userId }, cancellationToken);

    public Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"{ItemEndpoint(id)}/members/{userId:D}", cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync(ItemEndpoint(id), cancellationToken);

    internal static string BuildMembersUri(Guid id, string? filter, int skipCount, int maxResultCount) =>
        BuildPagedUri($"{ItemEndpoint(id)}/members", filter, skipCount, maxResultCount);

    internal static string BuildAvailableMembersUri(Guid id, string? filter, int skipCount, int maxResultCount) =>
        BuildPagedUri($"{ItemEndpoint(id)}/available-members", filter, skipCount, maxResultCount);

    internal const string Endpoint = "/api/identity/organization-units";
    internal const string LookupEndpoint = "/api/identity/organization-unit-lookup";

    internal static string ItemEndpoint(Guid id) => $"{Endpoint}/{id:D}";

    internal static string BuildUserLookupUri(IEnumerable<Guid> userIds) =>
        $"{LookupEndpoint}/users?{string.Join("&", userIds.Select(id => $"userIds={id:D}"))}";

    internal static string BuildMembersLookupUri(Guid organizationUnitId) =>
        $"{LookupEndpoint}/{organizationUnitId:D}/members";

    internal static string BuildSetUserUri(Guid userId) =>
        $"{LookupEndpoint}/users/{userId:D}";

    private static string BuildPagedUri(string endpoint, string? filter, int skipCount, int maxResultCount)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            parameters.Add($"filter={Uri.EscapeDataString(SearchText.Normalize(filter))}");
        }

        parameters.Add($"skipCount={Math.Max(0, skipCount)}");
        parameters.Add($"maxResultCount={Math.Clamp(maxResultCount, 1, MaxPageSize)}");
        return new StringBuilder(endpoint).Append('?').Append(string.Join('&', parameters)).ToString();
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await CreateClient().GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new OrganizationUnitApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        using var response = await CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new OrganizationUnitApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private async Task SendAsync(HttpMethod method, string uri, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        using var response = await CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task DeleteAsync(string uri, CancellationToken cancellationToken)
    {
        using var response = await CreateClient().DeleteAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new OrganizationUnitApiException(response.StatusCode, body);
    }

    private HttpClient CreateClient() => httpClientFactory.CreateClient("HCS.Bff");
}

public sealed class OrganizationUnitApiException(HttpStatusCode statusCode, string responseBody)
    : Exception(responseBody)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}

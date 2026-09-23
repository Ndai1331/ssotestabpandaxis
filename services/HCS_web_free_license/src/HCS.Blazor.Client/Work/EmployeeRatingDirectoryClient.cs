using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;

namespace HCS.Blazor.Client.Work;

public sealed record EmployeeDirectoryUserDto(Guid UserId, string UserName, string DisplayName, string? AvatarUrl);
public sealed record EmployeeDirectoryPageResponse(List<EmployeeDirectoryUserDto> Items, long TotalCount, bool HasMore);

public sealed class EmployeeRatingDirectoryClient(IHttpClientFactory httpClientFactory)
{
    public Task<EmployeeDirectoryPageResponse> GetPageAsync(
        string? filter = null,
        int skipCount = 0,
        int maxResultCount = 20,
        CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(0, skipCount);
        var take = Math.Clamp(maxResultCount, 1, 100);
        var uri = $"/api/identity/employee-directory?skipCount={skip}&maxResultCount={take}";
        if (!string.IsNullOrWhiteSpace(filter))
            uri += $"&filter={Uri.EscapeDataString(SearchText.Normalize(filter))}";
        return GetAsync<EmployeeDirectoryPageResponse>(uri, cancellationToken);
    }

    public Task<EmployeeDirectoryUserDto> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        GetAsync<EmployeeDirectoryUserDto>($"/api/identity/employee-directory/{userId:D}", cancellationToken);

    public Task<EmployeeDirectoryPageResponse> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().Take(200).ToArray();
        if (ids.Length == 0)
        {
            return Task.FromResult(new EmployeeDirectoryPageResponse([], 0, false));
        }

        var query = string.Join("&", ids.Select(id => $"userIds={id:D}"));
        return GetAsync<EmployeeDirectoryPageResponse>($"/api/identity/employee-directory?{query}", cancellationToken);
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("HCS.Bff").GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BffApiException(response.StatusCode, body);
        }
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }
}

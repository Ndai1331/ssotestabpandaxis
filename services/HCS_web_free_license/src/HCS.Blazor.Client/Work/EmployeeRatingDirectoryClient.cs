using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;

namespace HCS.Blazor.Client.Work;

public sealed record EmployeeDirectoryUserDto(Guid UserId, string UserName, string DisplayName, string? AvatarUrl);
public sealed record EmployeeDirectoryPageResponse(List<EmployeeDirectoryUserDto> Items, bool HasMore);

public sealed class EmployeeRatingDirectoryClient(IHttpClientFactory httpClientFactory)
{
    public async Task<List<EmployeeDirectoryUserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<EmployeeDirectoryUserDto>();
        var skip = 0;
        while (true)
        {
            var page = await GetAsync<EmployeeDirectoryPageResponse>(
                $"/api/identity/employee-directory?skipCount={skip}&maxResultCount=100", cancellationToken);
            all.AddRange(page.Items);
            skip += page.Items.Count;
            if (!page.HasMore || page.Items.Count == 0) break;
        }
        return all;
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

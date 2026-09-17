using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Logging;
using Volo.Abp.Application.Dtos;

namespace HCS.Blazor.Client.Logging;

public sealed class ServiceLogClient(IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ListResultDto<ServiceLogDto>> GetListAsync(
        GetServiceLogsInput input,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClientFactory.CreateClient("HCS.Bff")
            .GetAsync(BuildListUri(input), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BffApiException(response.StatusCode, body);
        }

        return await response.Content.ReadFromJsonAsync<ListResultDto<ServiceLogDto>>(JsonOptions, cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private static string BuildListUri(GetServiceLogsInput input)
    {
        var query = $"api/service-logs?maxResultCount={Math.Clamp(input.MaxResultCount <= 0 ? 100 : input.MaxResultCount, 1, 200)}";
        Add(ref query, "filter", input.Filter);
        Add(ref query, "afterId", input.AfterId);
        if (input.Applications is not null)
        {
            foreach (var application in input.Applications)
            {
                Add(ref query, "applications", application);
            }
        }

        if (input.Levels is not null)
        {
            foreach (var level in input.Levels)
            {
                Add(ref query, "levels", level);
            }
        }

        return query;
    }

    private static void Add(ref string query, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        query += $"&{name}={Uri.EscapeDataString(value)}";
    }
}

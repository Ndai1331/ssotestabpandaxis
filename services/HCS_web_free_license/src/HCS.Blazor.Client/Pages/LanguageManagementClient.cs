using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using Volo.Abp.Application.Dtos;

namespace HCS.Blazor.Client.Pages;

public sealed class LanguageManagementClient(IHttpClientFactory httpClientFactory)
{
    private const int MaxPageSize = 100;
    private const string LanguagesEndpoint = "/api/language-management/languages";
    private const string LanguageTextsEndpoint = "/api/language-management/language-texts";

    public Task<PagedResultDto<HCS.Localization.LanguageDto>> GetLanguagesAsync(
        string? filter, bool? isEnabled, int skipCount, int maxResultCount, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultDto<HCS.Localization.LanguageDto>>(
            BuildLanguagesUri(filter, isEnabled, skipCount, maxResultCount), cancellationToken);

    public Task<List<HCS.Localization.LanguageOptionDto>> GetEnabledLanguagesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<HCS.Localization.LanguageOptionDto>>($"{LanguagesEndpoint}/enabled", cancellationToken);

    public Task<HCS.Localization.LanguageDto> CreateLanguageAsync(
        HCS.Localization.CreateLanguageDto input, CancellationToken cancellationToken = default) =>
        SendAsync<HCS.Localization.LanguageDto>(HttpMethod.Post, LanguagesEndpoint, input, cancellationToken);

    public Task<HCS.Localization.LanguageDto> UpdateLanguageAsync(
        Guid id, HCS.Localization.UpdateLanguageDto input, CancellationToken cancellationToken = default) =>
        SendAsync<HCS.Localization.LanguageDto>(HttpMethod.Put, $"{LanguagesEndpoint}/{id:D}", input, cancellationToken);

    public Task DeleteLanguageAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"{LanguagesEndpoint}/{id:D}", cancellationToken);

    public Task<PagedResultDto<HCS.Localization.LanguageTextDto>> GetLanguageTextsAsync(
        string cultureName, string? filter, int skipCount, int maxResultCount, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultDto<HCS.Localization.LanguageTextDto>>(
            BuildLanguageTextsUri(cultureName, filter, skipCount, maxResultCount), cancellationToken);

    public Task<HCS.Localization.LanguageTextDto> CreateLanguageTextAsync(
        HCS.Localization.CreateLanguageTextDto input, CancellationToken cancellationToken = default) =>
        SendAsync<HCS.Localization.LanguageTextDto>(HttpMethod.Post, LanguageTextsEndpoint, input, cancellationToken);

    public Task<HCS.Localization.LanguageTextDto> UpdateLanguageTextAsync(
        Guid id, HCS.Localization.UpdateLanguageTextDto input, CancellationToken cancellationToken = default) =>
        SendAsync<HCS.Localization.LanguageTextDto>(HttpMethod.Put, $"{LanguageTextsEndpoint}/{id:D}", input, cancellationToken);

    public Task DeleteLanguageTextAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"{LanguageTextsEndpoint}/{id:D}", cancellationToken);

    internal static string BuildLanguagesUri(string? filter, bool? isEnabled, int skipCount, int maxResultCount)
    {
        var query = new List<string>();
        AddFilter(query, "filter", filter);
        if (isEnabled.HasValue) query.Add($"isEnabled={isEnabled.Value.ToString().ToLowerInvariant()}");
        query.Add($"skipCount={Math.Max(0, skipCount)}");
        query.Add($"maxResultCount={Math.Clamp(maxResultCount, 1, MaxPageSize)}");
        return $"{LanguagesEndpoint}?{string.Join('&', query)}";
    }

    internal static string BuildLanguageTextsUri(string cultureName, string? filter, int skipCount, int maxResultCount)
    {
        var query = new List<string> { "resourceName=HCS", $"cultureName={Uri.EscapeDataString(cultureName)}" };
        AddFilter(query, "filter", filter);
        query.Add($"skipCount={Math.Max(0, skipCount)}");
        query.Add($"maxResultCount={Math.Clamp(maxResultCount, 1, MaxPageSize)}");
        return $"{LanguageTextsEndpoint}?{string.Join('&', query)}";
    }

    private static void AddFilter(ICollection<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await CreateClient().GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(System.Net.HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        using var response = await CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(System.Net.HttpStatusCode.NoContent, "Gateway returned an empty response.");
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
        throw new BffApiException(response.StatusCode, body);
    }

    private HttpClient CreateClient() => httpClientFactory.CreateClient("HCS.Bff");
}

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Settings;

namespace HCS.Blazor.Client.Settings;

public sealed class StorageSettingsClient(IHttpClientFactory httpClientFactory)
{
    private const string Endpoint = "api/hcs/storage-settings";

    public async Task<StorageSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync(Endpoint, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<StorageSettingsDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    public async Task UpdateAsync(UpdateStorageSettingsDto input, CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().PutAsJsonAsync(Endpoint, input, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<StorageConnectionTestResultDto> TestAsync(
        UpdateStorageSettingsDto input,
        CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().PostAsJsonAsync($"{Endpoint}/test", input, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<StorageConnectionTestResultDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new BffApiException(response.StatusCode, body);
    }

    private HttpClient CreateClient() => httpClientFactory.CreateClient("HCS.Bff");
}

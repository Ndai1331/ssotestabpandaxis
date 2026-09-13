using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Branding;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace HCS.Blazor.Client.Branding;

public sealed class SystemBrandingClient(IHttpClientFactory httpClientFactory)
{
    private const string Endpoint = "api/hcs/system-branding";

    public async Task<SystemBrandingDto> GetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync(Endpoint, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadAsync(response, cancellationToken);
    }

    public async Task<SystemBrandingDto> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        using var response = await CreateClient().GetAsync($"{Endpoint}/public", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadAsync(response, cancellationToken);
    }

    public async Task<SystemBrandingDto> UpdateAsync(
        string title,
        string description,
        bool removeLogo,
        bool removeFavicon,
        bool removeBackground,
        IBrowserFile? logo,
        IBrowserFile? favicon,
        IBrowserFile? background,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent(description ?? string.Empty), "Description");
        content.Add(new StringContent(removeLogo ? "true" : "false"), "RemoveLogo");
        content.Add(new StringContent(removeFavicon ? "true" : "false"), "RemoveFavicon");
        content.Add(new StringContent(removeBackground ? "true" : "false"), "RemoveBackground");

        AddFile(content, logo, "Logo", SystemBrandingLimits.MaxLogoBytes, cancellationToken);
        AddFile(content, favicon, "Favicon", SystemBrandingLimits.MaxFaviconBytes, cancellationToken);
        AddFile(content, background, "Background", SystemBrandingLimits.MaxBackgroundBytes, cancellationToken);

        using var response = await CreateClient().PutAsync(Endpoint, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadAsync(response, cancellationToken);
    }

    private static void AddFile(
        MultipartFormDataContent content,
        IBrowserFile? file,
        string fieldName,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return;
        }

        var stream = file.OpenReadStream(maximumBytes, cancellationToken);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, fieldName, file.Name);
    }

    private static async Task<SystemBrandingDto> ReadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        return await response.Content.ReadFromJsonAsync<SystemBrandingDto>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty branding response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
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

public static class SystemBrandingLimits
{
    public const long MaxLogoBytes = 2 * 1024 * 1024;
    public const long MaxFaviconBytes = 512 * 1024;
    public const long MaxBackgroundBytes = 5 * 1024 * 1024;
}

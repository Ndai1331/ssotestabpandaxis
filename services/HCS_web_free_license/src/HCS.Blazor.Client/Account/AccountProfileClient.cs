using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace HCS.Blazor.Client.Account;

public sealed class AccountProfileClient(IHttpClientFactory httpClientFactory)
{
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private readonly SemaphoreSlim mineLock = new(1, 1);
    private bool mineLoaded;
    private AccountFileContent? mineContent;
    private string? mineEtag;

    public event EventHandler? AvatarChanged;

    public Task<AccountProfileDto> GetProfileAsync(CancellationToken cancellationToken = default) =>
        SendAsync<AccountProfileDto>(HttpMethod.Get, "/api/account/my-profile", null, cancellationToken);

    public Task<AccountProfileDto> UpdateProfileAsync(AccountProfileUpdateDto input, CancellationToken cancellationToken = default) =>
        SendAsync<AccountProfileDto>(HttpMethod.Put, "/api/account/my-profile", input, cancellationToken);

    public Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Post, "/api/account/my-profile/change-password", new
        {
            currentPassword,
            newPassword
        }, cancellationToken);

    public async Task UploadAvatarAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream(MaxAvatarBytes, cancellationToken);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType);
        content.Add(fileContent, "file", file.Name);
        using var response = await CreateClient().PostAsync("/api/identity/profile/avatar", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await InvalidateMineAsync(cancellationToken);
        AvatarChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteAvatarAsync(CancellationToken cancellationToken = default)
    {
        await SendNoContentAsync(HttpMethod.Delete, "/api/identity/profile/avatar", null, cancellationToken);
        await mineLock.WaitAsync(cancellationToken);
        try
        {
            mineLoaded = true;
            mineContent = null;
            mineEtag = null;
        }
        finally
        {
            mineLock.Release();
        }

        AvatarChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<AccountFileContent?> TryGetAvatarAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (userId is { } id)
        {
            var fetched = await FetchAvatarAsync($"/api/identity/users/{id:D}/avatar", ifNoneMatch: null, cancellationToken);
            return fetched.StatusCode == HttpStatusCode.NotFound ? null : fetched.Content;
        }

        await mineLock.WaitAsync(cancellationToken);
        try
        {
            if (mineLoaded)
            {
                return mineContent;
            }

            var fetched = await FetchAvatarAsync("/api/identity/profile/avatar", mineEtag, cancellationToken);
            if (fetched.StatusCode == HttpStatusCode.NotModified)
            {
                mineLoaded = true;
                return mineContent;
            }

            mineLoaded = true;
            mineEtag = fetched.Etag;
            mineContent = fetched.StatusCode == HttpStatusCode.NotFound ? null : fetched.Content;
            return mineContent;
        }
        finally
        {
            mineLock.Release();
        }
    }

    private async Task InvalidateMineAsync(CancellationToken cancellationToken)
    {
        await mineLock.WaitAsync(cancellationToken);
        try
        {
            mineLoaded = false;
            mineContent = null;
            mineEtag = null;
        }
        finally
        {
            mineLock.Release();
        }
    }

    private async Task<AvatarFetchResult> FetchAvatarAsync(string uri, string? ifNoneMatch, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrWhiteSpace(ifNoneMatch)
            && EntityTagHeaderValue.TryParse(ifNoneMatch, out var etag))
        {
            request.Headers.IfNoneMatch.Add(etag);
        }

        using var response = await CreateClient().SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NotModified)
        {
            return new AvatarFetchResult(response.StatusCode, null, ReadEtag(response));
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
        return new AvatarFetchResult(response.StatusCode, new AccountFileContent(bytes, contentType), ReadEtag(response));
    }

    private static string? ReadEtag(HttpResponseMessage response) =>
        response.Headers.ETag?.ToString();

    private sealed record AvatarFetchResult(HttpStatusCode StatusCode, AccountFileContent? Content, string? Etag);

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? payload, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(method, uri, payload);
        using var response = await CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private async Task SendNoContentAsync(HttpMethod method, string uri, object? payload, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(method, uri, payload);
        using var response = await CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string uri, object? payload) =>
        new(method, uri) { Content = payload is null ? null : JsonContent.Create(payload) };

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

public sealed class AccountProfileDto
{
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsExternal { get; set; }
    public bool HasPassword { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}

public sealed class AccountProfileUpdateDto
{
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}

public sealed record AccountFileContent(byte[] Bytes, string ContentType)
{
    public string ToDataUrl() => $"data:{ContentType};base64,{Convert.ToBase64String(Bytes)}";
}

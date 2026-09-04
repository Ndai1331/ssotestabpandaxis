using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HCS.CollaborationService.Contracts;
using HCS.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace HCS.Blazor.Client.Collaboration;

internal sealed class SocialClient(IHttpClientFactory httpClientFactory)
{
    private const long MaxMediaSize = 25 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<PagedSocialPostsDto> GetFeedAsync(int skip = 0, int take = 20, CancellationToken ct = default) =>
        GetAsync<PagedSocialPostsDto>($"api/social/feed?skip={Math.Max(skip, 0)}&take={Math.Clamp(take, 1, 50)}", ct);

    public Task<PagedSocialPostsDto> GetProfilePostsAsync(int skip = 0, int take = 20,
        SocialPostVisibility? visibility = null, CancellationToken ct = default)
    {
        var uri = $"api/social/profile/posts?skip={Math.Max(skip, 0)}&take={Math.Clamp(take, 1, 50)}";
        if (visibility.HasValue)
            uri += $"&visibility={visibility.Value.ToString().ToLowerInvariant()}";

        return GetAsync<PagedSocialPostsDto>(uri, ct);
    }

    public Task<IReadOnlyList<SocialCommentDto>> GetCommentsAsync(Guid postId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SocialCommentDto>>($"api/social/posts/{postId:D}/comments", ct);

    public Task<SocialPostDto> CreatePostAsync(CreateSocialPostInput input, CancellationToken ct = default) =>
        SendAsync<CreateSocialPostInput, SocialPostDto>(HttpMethod.Post, "api/social/posts", input, ct);

    public Task<SocialCommentDto> CreateCommentAsync(Guid postId, CreateSocialCommentInput input, CancellationToken ct = default) =>
        SendAsync<CreateSocialCommentInput, SocialCommentDto>(HttpMethod.Post, $"api/social/posts/{postId:D}/comments", input, ct);

    public Task DeleteUnattachedMediaAsync(Guid mediaId, CancellationToken ct = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"api/social/media/{mediaId:D}", ct);

    public async Task<UploadSocialMediaResult> UploadMediaAsync(IBrowserFile file, CancellationToken ct = default)
    {
        if (file.Size > MaxMediaSize)
            throw new CollaborationApiException(HttpStatusCode.RequestEntityTooLarge, "Social media exceeds the 25 MB limit.");

        using var content = new MultipartFormDataContent();
        await using var source = file.OpenReadStream(MaxMediaSize, ct);
        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        using var fileContent = new StreamContent(buffer);
        fileContent.Headers.ContentLength = buffer.Length;
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);

        using var response = await CreateClient().PostAsync("api/social/uploads", content, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<UploadSocialMediaResult>(JsonOptions, ct)
            ?? throw new CollaborationApiException(HttpStatusCode.NoContent, "The gateway returned no media response.");
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken ct)
    {
        using var response = await CreateClient().GetAsync(uri, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
            ?? throw new CollaborationApiException(HttpStatusCode.NoContent, "The gateway returned no response.");
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(HttpMethod method, string uri,
        TRequest payload, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        using var response = await CreateClient().SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
            ?? throw new CollaborationApiException(HttpStatusCode.NoContent, "The gateway returned no response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        throw new CollaborationApiException(response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    private async Task SendNoContentAsync(HttpMethod method, string uri, CancellationToken ct)
    {
        using var response = await CreateClient().SendAsync(new HttpRequestMessage(method, uri), ct);
        await EnsureSuccessAsync(response, ct);
    }

    private HttpClient CreateClient() => httpClientFactory.CreateClient("HCS.Bff");
}

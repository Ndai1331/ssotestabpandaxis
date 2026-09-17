using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Blazor.Client.Work;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HCS.Blazor.EventCheckIn;

public sealed class EventCheckInGatewayClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
{
    public const string HttpClientName = "HCS.EventCheckIn";

    public Task<PublicEventDto> GetAsync(string code, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicEventDto>(HttpMethod.Get, PublicPath(code, token), payload: null, cancellationToken);

    public Task<PublicEventConfirmResultDto> ConfirmAsync(string code, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicEventConfirmResultDto>(HttpMethod.Post, PublicPath(code, token, "confirm"), new PublicEventCheckInRequest(), cancellationToken);

    public Task<PublicEventConfirmResultDto> DeclineAsync(string code, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicEventConfirmResultDto>(HttpMethod.Post, PublicPath(code, token, "decline"), new PublicEventCheckInRequest(), cancellationToken);

    public Task<PublicEventCheckInResultDto> CheckInAsync(string code, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicEventCheckInResultDto>(HttpMethod.Post, PublicPath(code, token, "check-in"), new PublicEventCheckInRequest(), cancellationToken);

    public string BuildPublicAttachmentUrl(string code, string token, Guid fileId)
    {
        var origin = EventCheckInOrigins.ResolveBrowser(configuration);
        return new Uri(origin, $"api/events/public/{Uri.EscapeDataString(code)}/attachments/{fileId:D}?token={Uri.EscapeDataString(token)}").AbsoluteUri;
    }

    private static string PublicPath(string code, string token, string? action = null)
    {
        var suffix = string.IsNullOrWhiteSpace(action) ? "" : "/" + action;
        return $"/api/events/public/{Uri.EscapeDataString(code)}{suffix}?token={Uri.EscapeDataString(token)}";
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        ForwardBrowserCookies(request);
        using var response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BffApiException(response.StatusCode, body);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new BffApiException(HttpStatusCode.NoContent, "Gateway returned an empty response.");
    }

    private void ForwardBrowserCookies(HttpRequestMessage request)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Event check-in requires the current HTTP context.");
        if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookies) &&
            !string.IsNullOrWhiteSpace(cookies.ToString()))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookies.ToString());
        }
    }
}

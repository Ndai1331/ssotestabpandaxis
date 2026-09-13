using System.Net;
using System.Net.Http.Json;
using HCS.Blazor.Client.Documents;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class SigningCredentialUploadTests
{
    [Fact]
    public async Task Layout_stream_remains_readable_during_send_and_is_disposed_afterwards()
    {
        var file = new LayoutFile();
        using var handler = new UploadHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var client = new DocumentClient(new ClientFactory(http));

        var result = await client.ConfigureCredentialWithLayoutAsync(
            new(2, "https://signing.example", "test-secret", "TEST"), file);

        Assert.True(handler.UploadRead);
        Assert.True(result.HasLayoutImage);
        Assert.False(file.Stream.CanRead);
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class LayoutFile : IBrowserFile
    {
        public MemoryStream Stream { get; } = new([1, 2, 3, 4]);
        public string Name => "layout.jpg";
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size => 4;
        public string ContentType => "image/jpeg";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => Stream;
    }

    private sealed class UploadHandler : HttpMessageHandler
    {
        public bool UploadRead { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/api/signing/credentials/current/upload", request.RequestUri!.AbsolutePath);
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var file = Assert.Single(multipart.Where(part =>
                part.Headers.ContentDisposition?.Name?.Trim('"') == "layoutImage"));
            Assert.Equal("image/jpeg", file.Headers.ContentType!.MediaType);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, await file.ReadAsByteArrayAsync(cancellationToken));
            UploadRead = true;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SigningCredentialDto(Guid.NewGuid(), 2, "TEST",
                    "https://signing.example", "***", 30, 150, 70, true, true, false, DateTime.UtcNow, true))
            };
        }
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using hanhchinhso.DocumentService.Signing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Xunit;
using PdfSharp.Pdf;

namespace hanhchinhso.DocumentService.Tests.Signing;

public class RemoteCaSigningProviderTests
{
    [Fact]
    public void Should_Match_Canonical_Hmac_Vector()
    {
        var endpoint = new Uri(
            "https://sign.example.test/api/v2/pdf/sign/originaldata");
        var canonical = RemoteCaHmac.CreateCanonicalText(
            HttpMethod.Post,
            endpoint,
            "api-key",
            "nonce-123",
            "Wed, 01 Jan 2025 00:00:00 GMT",
            """{"a":1}""");

        canonical.ShouldBe(
            "POST\nhttps\nsign.example.test:443\n" +
            "/api/v2/pdf/sign/originaldata\n" +
            "application/json; charset=utf-8\n" +
            "api-key\nnonce-123\n" +
            "Wed, 01 Jan 2025 00:00:00 GMT\n" +
            "{\"a\":1}\n");
        RemoteCaHmac.ComputeDigest(
                "MDEyMzQ1Njc4OWFiY2RlZg==",
                canonical)
            .ShouldBe(
                "VSJb3n1vwA0Syts612ujRAJAPpx6/Y1fMUjGiqPlEKk=");
    }

    [Fact]
    public void Should_Bracket_Ipv6_In_Canonical_Host()
    {
        var canonical = RemoteCaHmac.CreateCanonicalText(
            HttpMethod.Post,
            new Uri("https://[2001:db8::1]/sign"),
            "key",
            "nonce",
            "Wed, 01 Jan 2025 00:00:00 GMT",
            "{}");

        canonical.ShouldContain(
            "\n[2001:db8::1]:443\n/sign\n");
    }

    [Fact]
    public async Task Should_Send_Canonical_Authenticated_Request()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var signedPdf = CreateValidPdf();
        var handler = new DelegateHandler(async request =>
        {
            captured = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return JsonResponse(new
            {
                status = 0,
                obj = Convert.ToBase64String(signedPdf)
            });
        });
        var policy = Substitute.For<ISigningEndpointPolicy>();
        var provider = CreateProvider(handler, policy);

        var result = await provider.SignAsync(
            Command(), CancellationToken.None);

        result.ShouldBe(signedPdf);
        captured.ShouldNotBeNull();
        captured.RequestUri.ShouldBe(
            new Uri(
                "https://sign.example.test/api/v2/pdf/sign/originaldata"));
        captured.Headers.GetValues("Date").Single()
            .ShouldBe("Wed, 01 Jan 2025 00:00:00 GMT");
        captured.Headers.GetValues("Authorization").Single()
            .ShouldStartWith(
                "HmacSHA256 api-key:nonce-123:");
        captured.Content!.Headers.ContentType!.ToString()
            .ShouldBe(RemoteCaHmac.ContentType);
        using var json = JsonDocument.Parse(capturedBody!);
        json.RootElement.GetProperty("base64pdf").GetString()
            .ShouldBe(Convert.ToBase64String("%PDF-1.4\nsource"u8));
        json.RootElement.GetProperty("TextLocationIdentifier")
            .GetString().ShouldBe("<<Sign02>>");
        policy.Received(1).Validate(
            "https://sign.example.test/");
    }

    [Fact]
    public async Task Should_Reject_Redirect_Without_Leaking_Request()
    {
        var handler = new DelegateHandler(_ =>
            Task.FromResult(new HttpResponseMessage(
                HttpStatusCode.Redirect)
            {
                Headers =
                {
                    Location = new Uri("https://attacker.example/")
                }
            }));
        var provider = CreateProvider(
            handler,
            Substitute.For<ISigningEndpointPolicy>());

        var exception = await Should.ThrowAsync<BusinessException>(
            () => provider.SignAsync(
                Command(), CancellationToken.None));

        exception.Code.ShouldBe(
            "DocumentService:RemoteCaHttpFailure");
        handler.CallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("not-base64", "DocumentService:RemoteCaSecretMustBeBase64")]
    [InlineData("c2hvcnQ=", "DocumentService:RemoteCaSecretTooShort")]
    public async Task Should_Reject_Invalid_Hmac_Secret(
        string secret,
        string code)
    {
        var provider = CreateProvider(
            new DelegateHandler(_ =>
                throw new InvalidOperationException()),
            Substitute.For<ISigningEndpointPolicy>());

        var exception = await Should.ThrowAsync<BusinessException>(
            () => provider.SignAsync(
                Command(secret), CancellationToken.None));

        exception.Code.ShouldBe(code);
    }

    [Fact]
    public async Task Should_Reject_Invalid_Provider_Payload()
    {
        var provider = CreateProvider(
            new DelegateHandler(_ => Task.FromResult(
                JsonResponse(new { status = 0, obj = "not-base64" }))),
            Substitute.For<ISigningEndpointPolicy>());

        var exception = await Should.ThrowAsync<BusinessException>(
            () => provider.SignAsync(
                Command(), CancellationToken.None));

        exception.Code.ShouldBe(
            "DocumentService:RemoteCaInvalidResponse");
    }

    [Fact]
    public async Task Should_Reject_Missing_Status()
    {
        var provider = CreateProvider(
            new DelegateHandler(_ => Task.FromResult(
                JsonResponse(new
                {
                    obj = Convert.ToBase64String(CreateValidPdf())
                }))),
            Substitute.For<ISigningEndpointPolicy>());

        var exception = await Should.ThrowAsync<BusinessException>(
            () => provider.SignAsync(
                Command(), CancellationToken.None));

        exception.Code.ShouldBe(
            "DocumentService:RemoteCaRejected");
    }

    [Fact]
    public async Task Should_Reject_Truncated_Pdf()
    {
        var provider = CreateProvider(
            new DelegateHandler(_ => Task.FromResult(
                JsonResponse(new
                {
                    status = 0,
                    obj = Convert.ToBase64String(
                        "%PDF-1.4\ntruncated"u8)
                }))),
            Substitute.For<ISigningEndpointPolicy>());

        var exception = await Should.ThrowAsync<BusinessException>(
            () => provider.SignAsync(
                Command(), CancellationToken.None));

        exception.Code.ShouldBe(
            "DocumentService:RemoteCaInvalidSignedPdf");
    }

    [Fact]
    public async Task Should_Enforce_Per_Request_Timeout()
    {
        var handler = new DelegateHandler(async (
            _,
            cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            throw new InvalidOperationException();
        });
        var provider = CreateProvider(
            handler,
            Substitute.For<ISigningEndpointPolicy>());

        var exception = await Should.ThrowAsync<BusinessException>(
            () => provider.SignAsync(
                Command() with { TimeoutSeconds = 1 },
                CancellationToken.None));

        exception.Code.ShouldBe("DocumentService:RemoteCaTimeout");
    }

    [Fact]
    public async Task Should_Apply_Endpoint_Policy_Before_Network()
    {
        var handler = new DelegateHandler(_ =>
            throw new InvalidOperationException());
        var policy = Substitute.For<ISigningEndpointPolicy>();
        policy.When(x => x.Validate(Arg.Any<string>()))
            .Do(_ => throw new BusinessException(
                "DocumentService:SigningEndpointHostNotAllowed"));
        var provider = CreateProvider(handler, policy);

        var exception = await Should.ThrowAsync<BusinessException>(
            () => provider.SignAsync(
                Command(), CancellationToken.None));

        exception.Code.ShouldBe(
            "DocumentService:SigningEndpointHostNotAllowed");
        handler.CallCount.ShouldBe(0);
    }

    private static RemoteCaSigningProvider CreateProvider(
        HttpMessageHandler handler,
        ISigningEndpointPolicy policy) =>
        new(
            new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            },
            policy,
            new FixedRequestValues(),
            NullLogger<RemoteCaSigningProvider>.Instance);

    private static RemoteCaSigningCommand Command(
        string secret = "MDEyMzQ1Njc4OWFiY2RlZg==") =>
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "TAG",
            new Uri("https://sign.example.test"),
            30,
            "api-key",
            secret,
            "%PDF-1.4\nsource"u8.ToArray(),
            "image"u8.ToArray(),
            "<<Sign02>>",
            1,
            10,
            20,
            150,
            70,
            "Signer");

    private static HttpResponseMessage JsonResponse(object value) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value),
                Encoding.UTF8,
                "application/json")
        };

    private static byte[] CreateValidPdf()
    {
        using var document = new PdfDocument();
        document.AddPage();
        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private sealed class FixedRequestValues : IRemoteCaRequestValues
    {
        public DateTimeOffset UtcNow =>
            new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public string CreateNonce() => "nonce-123";
    }

    private sealed class DelegateHandler :
        HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> _send;

        public DelegateHandler(
            Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
            : this((request, _) => send(request))
        {
        }

        public DelegateHandler(
            Func<HttpRequestMessage, CancellationToken,
                Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return _send(request, cancellationToken);
        }
    }
}

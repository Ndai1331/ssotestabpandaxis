using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Signing;

public sealed record RemoteCaSigningCommand(
    Guid AttemptId,
    string ProviderCode,
    Uri Endpoint,
    int TimeoutSeconds,
    string ApiKey,
    string Base64Secret,
    byte[] PdfBytes,
    byte[] SignatureImageBytes,
    string Placeholder,
    int Page,
    int X,
    int Y,
    int Width,
    int Height,
    string SignerText);

public interface IRemoteCaSigningProvider
{
    Task<byte[]> SignAsync(
        RemoteCaSigningCommand command,
        CancellationToken cancellationToken);
}

public sealed record BnnSigningCommand(
    Guid AttemptId,
    string ProviderCode,
    Uri Endpoint,
    int TimeoutSeconds,
    string TokenReference,
    string Secret,
    byte[] PdfBytes,
    byte[] SignatureImageBytes,
    byte[]? SealImageBytes,
    byte[]? LayoutImageBytes,
    string Placeholder,
    int Width,
    int Height);

public interface IBnnSigningProvider
{
    Task<byte[]> SignAsync(
        BnnSigningCommand command,
        CancellationToken cancellationToken);
}

public sealed class UnavailableBnnSigningProvider :
    IBnnSigningProvider,
    ITransientDependency
{
    public Task<byte[]> SignAsync(
        BnnSigningCommand command,
        CancellationToken cancellationToken) =>
        throw new BusinessException(
            "DocumentService:BnnSigningProviderUnavailable");
}

public interface IRemoteCaRequestValues
{
    DateTimeOffset UtcNow { get; }
    string CreateNonce();
}

public sealed class RemoteCaRequestValues :
    IRemoteCaRequestValues,
    ITransientDependency
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public string CreateNonce() => Guid.NewGuid().ToString("N");
}

public static class RemoteCaHmac
{
    public const string ContentType = "application/json; charset=utf-8";

    public static string CreateCanonicalText(
        HttpMethod method,
        Uri endpoint,
        string apiKey,
        string nonce,
        string dateHeader,
        string bodyJson)
    {
        var defaultPort = endpoint.Scheme == Uri.UriSchemeHttps ? 443 : 80;
        var host = endpoint.HostNameType == UriHostNameType.IPv6
            ? $"[{endpoint.IdnHost}]"
            : endpoint.IdnHost;
        var hostPort =
            $"{host}:{(endpoint.IsDefaultPort ? defaultPort : endpoint.Port)}";
        return string.Join(
            '\n',
            method.Method.ToUpperInvariant(),
            endpoint.Scheme.ToLowerInvariant(),
            hostPort,
            endpoint.PathAndQuery,
            ContentType,
            apiKey,
            nonce,
            dateHeader,
            bodyJson) + "\n";
    }

    public static string ComputeDigest(
        string base64Secret,
        string canonicalText)
    {
        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Secret);
        }
        catch (FormatException exception)
        {
            throw new BusinessException(
                "DocumentService:RemoteCaSecretMustBeBase64",
                innerException: exception);
        }
        if (key.Length < 16)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new BusinessException(
                "DocumentService:RemoteCaSecretTooShort");
        }
        try
        {
            return Convert.ToBase64String(
                HMACSHA256.HashData(
                    key,
                    Encoding.UTF8.GetBytes(canonicalText)));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}

public sealed class RemoteCaSigningProvider : IRemoteCaSigningProvider
{
    private const string SignPath = "/api/v2/pdf/sign/originaldata";
    private const int MaxResponseBytes = 150 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ISigningEndpointPolicy _endpointPolicy;
    private readonly IRemoteCaRequestValues _requestValues;
    private readonly ILogger<RemoteCaSigningProvider> _logger;

    public RemoteCaSigningProvider(
        HttpClient http,
        ISigningEndpointPolicy endpointPolicy,
        IRemoteCaRequestValues requestValues,
        ILogger<RemoteCaSigningProvider> logger)
    {
        _http = http;
        _endpointPolicy = endpointPolicy;
        _requestValues = requestValues;
        _logger = logger;
    }

    public async Task<byte[]> SignAsync(
        RemoteCaSigningCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);
        _endpointPolicy.Validate(command.Endpoint.AbsoluteUri);
        var endpoint = new Uri(
            command.Endpoint.GetLeftPart(UriPartial.Authority) + SignPath);
        var body = JsonSerializer.Serialize(
            new RemoteCaPdfRequest
            {
                Base64Pdf = Convert.ToBase64String(command.PdfBytes),
                Base64Image =
                    Convert.ToBase64String(command.SignatureImageBytes),
                Base64SignImage =
                    Convert.ToBase64String(command.SignatureImageBytes),
                SignatureName = command.Placeholder,
                TextLocationIdentifier = command.Placeholder,
                PageSign = command.Page,
                XPoint = command.X,
                YPoint = command.Y,
                Width = command.Width,
                Height = command.Height,
                TextOut = command.SignerText
            },
            JsonOptions);
        var now = _requestValues.UtcNow;
        var date = now.ToString("r", CultureInfo.InvariantCulture);
        var nonce = _requestValues.CreateNonce();
        var canonical = RemoteCaHmac.CreateCanonicalText(
            HttpMethod.Post,
            endpoint,
            command.ApiKey,
            nonce,
            date,
            body);
        var digest = RemoteCaHmac.ComputeDigest(
            command.Base64Secret, canonical);
        var authorization =
            $"HmacSHA256 {command.ApiKey}:{nonce}:{digest}:" +
            now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("Date", date);
        request.Headers.TryAddWithoutValidation(
            "Authorization", authorization);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(body, Encoding.UTF8);
        request.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(RemoteCaHmac.ContentType);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(command.TimeoutSeconds));
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new BusinessException(
                    "DocumentService:RemoteCaHttpFailure")
                    .WithData("StatusCode", (int)response.StatusCode);
            }
            var responseBytes = await ReadBoundedAsync(
                response.Content, timeout.Token);
            timeout.Token.ThrowIfCancellationRequested();
            var (status, signedPdf) = ParseResponse(responseBytes);
            if (status != 0)
            {
                throw new BusinessException(
                    "DocumentService:RemoteCaRejected");
            }
            if (signedPdf.Length < 8 ||
                !signedPdf.AsSpan(0, 4).SequenceEqual("%PDF"u8))
            {
                throw new BusinessException(
                    "DocumentService:RemoteCaInvalidSignedPdf");
            }
            try
            {
                timeout.Token.ThrowIfCancellationRequested();
                using var parsed =
                    UglyToad.PdfPig.PdfDocument.Open(signedPdf);
                if (parsed.NumberOfPages < 1)
                {
                    throw new BusinessException(
                        "DocumentService:RemoteCaInvalidSignedPdf");
                }
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BusinessException(
                    "DocumentService:RemoteCaInvalidSignedPdf",
                    innerException: exception);
            }
            return signedPdf;
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new BusinessException(
                "DocumentService:RemoteCaTimeout",
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new BusinessException(
                "DocumentService:RemoteCaConnectionFailed",
                innerException: exception);
        }
        finally
        {
            _logger.LogInformation(
                "Signing provider completed AttemptId={AttemptId} " +
                "ProviderCode={ProviderCode} DurationMs={DurationMs}",
                command.AttemptId,
                command.ProviderCode,
                watch.ElapsedMilliseconds);
        }
    }

    private static void Validate(RemoteCaSigningCommand command)
    {
        if (command.AttemptId == Guid.Empty ||
            (command.Endpoint.Scheme != Uri.UriSchemeHttp &&
             command.Endpoint.Scheme != Uri.UriSchemeHttps) ||
            command.TimeoutSeconds is < 1 or > 600 ||
            command.ApiKey.IsNullOrWhiteSpace() ||
            command.ApiKey.IndexOfAny([':', '\r', '\n']) >= 0 ||
            command.ApiKey.Length > 500 ||
            command.Base64Secret.Length > 8192 ||
            command.PdfBytes.Length == 0 ||
            command.PdfBytes.Length > 104_857_600 ||
            command.SignatureImageBytes.Length == 0 ||
            command.SignatureImageBytes.Length > 10_485_760 ||
            command.Page < 1 ||
            command.Width < 1 ||
            command.Height < 1)
        {
            throw new BusinessException(
                "DocumentService:InvalidRemoteCaSigningCommand");
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > MaxResponseBytes)
        {
            throw new BusinessException(
                "DocumentService:RemoteCaResponseTooLarge");
        }
        await using var source =
            await content.ReadAsStreamAsync(cancellationToken);
        using var target = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(81_920);
        try
        {
            while (true)
            {
                var read = await source.ReadAsync(
                    buffer.AsMemory(), cancellationToken);
                if (read == 0)
                {
                    break;
                }
                if (target.Length + read > MaxResponseBytes)
                {
                    throw new BusinessException(
                        "DocumentService:RemoteCaResponseTooLarge");
                }
                await target.WriteAsync(
                    buffer.AsMemory(0, read), cancellationToken);
            }
            return target.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static (int? Status, byte[] SignedPdf) ParseResponse(
        byte[] responseBytes)
    {
        int? status = null;
        byte[]? signedPdf = null;
        try
        {
            var reader = new Utf8JsonReader(responseBytes);
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }
                if (reader.ValueTextEquals("status"u8))
                {
                    if (!reader.Read() ||
                        reader.TokenType != JsonTokenType.Number ||
                        !reader.TryGetInt32(out var value))
                    {
                        throw new JsonException();
                    }
                    status = value;
                }
                else if (reader.ValueTextEquals("obj"u8))
                {
                    if (!reader.Read() ||
                        reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException();
                    }
                    signedPdf = reader.GetBytesFromBase64();
                    if (signedPdf.Length > 104_857_600)
                    {
                        throw new BusinessException(
                            "DocumentService:RemoteCaResponseTooLarge");
                    }
                }
                else
                {
                    reader.Skip();
                }
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new BusinessException(
                "DocumentService:RemoteCaInvalidResponse",
                innerException: exception);
        }
        catch (FormatException exception)
        {
            throw new BusinessException(
                "DocumentService:RemoteCaInvalidResponse",
                innerException: exception);
        }
        if (status is null || signedPdf is null)
        {
            return (status, []);
        }
        return (status, signedPdf);
    }

    private sealed class RemoteCaPdfRequest
    {
        [JsonPropertyName("base64image")]
        public string Base64Image { get; init; } = string.Empty;
        [JsonPropertyName("base64pdf")]
        public string Base64Pdf { get; init; } = string.Empty;
        [JsonPropertyName("hashalg")]
        public string HashAlgorithm { get; init; } = "SHA256";
        [JsonPropertyName("height")]
        public int Height { get; init; }
        [JsonPropertyName("pagesign")]
        public int PageSign { get; init; }
        [JsonPropertyName("signaturename")]
        public string SignatureName { get; init; } = string.Empty;
        [JsonPropertyName("textout")]
        public string TextOut { get; init; } = string.Empty;
        [JsonPropertyName("typesignature")]
        public int SignatureType { get; init; } = 3;
        [JsonPropertyName("width")]
        public int Width { get; init; }
        [JsonPropertyName("xpoint")]
        public int XPoint { get; init; }
        [JsonPropertyName("ypoint")]
        public int YPoint { get; init; }
        [JsonPropertyName("textoutcolor")]
        public string TextColor { get; init; } = "0,0,0";
        public string TextLocationIdentifier { get; init; } = string.Empty;
        public bool AppendDateSign { get; init; }
        public string DateFormatString { get; init; } =
            "dd/MM/yyyy HH:mm:ss";
        public float FontSize { get; init; } = 9f;
        [JsonPropertyName("base64SignImage")]
        public string Base64SignImage { get; init; } = string.Empty;
        [JsonPropertyName("yPointOffset")]
        public string YPointOffset { get; init; } = "center";
    }

}

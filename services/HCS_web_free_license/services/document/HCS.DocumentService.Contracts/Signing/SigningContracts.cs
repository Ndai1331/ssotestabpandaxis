using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace HCS.DocumentService.Signing;

public enum SigningKind { Electronic, RemoteCa, Hsm }
public enum SigningStatus { Pending, Completed, Failed }

public sealed class ConfigureSigningCredentialRequest
{
    public SigningKind Kind { get; init; }
    [Required, StringLength(1024)]
    public string Endpoint { get; init; } = string.Empty;
    [JsonPropertyName("secret")]
    public string Secret { private get; init; } = string.Empty;
    public string ConsumeSecret() => Secret;
}

public sealed record SigningCredentialDto(Guid Id, SigningKind Kind, string Endpoint, string MaskedSecret, DateTime UpdatedAt);
public sealed record UserSignatureDto(Guid Id, string FileName, string ContentType, long Size, bool IsDefault, DateTime CreationTime);
public sealed record SignDocumentRequest(Guid DocumentId, Guid FileId, SigningKind Kind,
    [property: Required, StringLength(128, MinimumLength = 1)] string IdempotencyKey);
public sealed record SigningAttemptDto(Guid Id, Guid DocumentId, Guid FileId, SigningKind Kind, SigningStatus Status,
    string InputSha256, string? OutputSha256, string? Error, DateTime CreationTime, DateTime? CompletedAt);
public sealed record SigningReportDto(Guid DocumentId, int Completed, int Failed, IReadOnlyList<SigningAttemptDto> Attempts);

public interface ISigningAppService
{
    Task<IReadOnlyList<SigningCredentialDto>> GetCredentialsAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<SigningCredentialDto> ConfigureCredentialAsync(ConfigureSigningCredentialRequest input, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<SigningAttemptDto> SignAsync(SignDocumentRequest input, CancellationToken cancellationToken = default);
    Task<SigningReportDto> GetReportAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSignatureDto>> GetSignaturesAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<UserSignatureDto> UploadSignatureAsync(string fileName, string contentType, Stream content, long size, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<UserSignatureDto> UpdateSignatureAsync(Guid id, string? fileName, string? contentType, Stream? content, long? size, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<UserSignatureDto> SetDefaultSignatureAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task DeleteSignatureAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType, string FileName)> OpenSignatureContentAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
}

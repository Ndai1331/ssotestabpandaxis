using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.DocumentService.Signing;

[JsonConverter(typeof(StrictSignatureTypeJsonConverter))]
public enum SignatureType
{
    Electronic = 0,
    Digital = 1
}

[JsonConverter(typeof(StrictSignatureProviderTypeJsonConverter))]
public enum SignatureProviderType
{
    Hsm = 0,
    RemoteCa = 1,
    UsbToken = 2
}

[JsonConverter(typeof(StrictSigningAssetKindJsonConverter))]
public enum SigningAssetKind
{
    SignatureImage = 0,
    SealImage = 1,
    LayoutImage = 2
}

[JsonConverter(typeof(StrictSigningAttemptStatusJsonConverter))]
public enum SigningAttemptStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}

public sealed class StrictSignatureTypeJsonConverter() :
    JsonStringEnumConverter<SignatureType>(JsonNamingPolicy.CamelCase, false);

public sealed class StrictSignatureProviderTypeJsonConverter() :
    JsonStringEnumConverter<SignatureProviderType>(JsonNamingPolicy.CamelCase, false);

public sealed class StrictSigningAssetKindJsonConverter() :
    JsonStringEnumConverter<SigningAssetKind>(JsonNamingPolicy.CamelCase, false);
public sealed class StrictSigningAttemptStatusJsonConverter() :
    JsonStringEnumConverter<SigningAttemptStatus>(
        JsonNamingPolicy.CamelCase, false);

public static class SigningConsts
{
    public const int ProviderCodeMaxLength = 50;
    public const int EndpointMaxLength = 500;
    public const int BlobNameMaxLength = 500;
    public const int TokenReferenceMaxLength = 500;
    public const int SignedFileSuffixMaxLength = 50;
}

public class SigningListInput : PagedAndSortedResultRequestDto
{
    public string? FilterText { get; set; }
    public bool? IsActive { get; set; }
}

public class SignatureSettingDto : AuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public string ProviderCode { get; set; } = string.Empty;
    public SignatureProviderType ProviderType { get; set; }
    public string ApiEndpoint { get; set; } = string.Empty;
    public Guid? LayoutAssetId { get; set; }
    public int ApiTimeoutSeconds { get; set; }
    public SignatureType DefaultSignatureType { get; set; }
    public bool AllowElectronicSign { get; set; }
    public bool AllowDigitalSign { get; set; }
    public bool RequireOtp { get; set; }
    public int SignWidth { get; set; }
    public int SignHeight { get; set; }
    public string SignedFileSuffix { get; set; } = string.Empty;
    public bool KeepOriginalFile { get; set; }
    public bool OverwriteSignedFile { get; set; }
    public bool EnableSignLog { get; set; }
    public bool IsActive { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdateSignatureSettingDto : IHasConcurrencyStamp
{
    [Required, MaxLength(SigningConsts.ProviderCodeMaxLength)]
    public string ProviderCode { get; set; } = string.Empty;
    public SignatureProviderType ProviderType { get; set; }
    [Required, MaxLength(SigningConsts.EndpointMaxLength)]
    public string ApiEndpoint { get; set; } = string.Empty;
    public Guid? LayoutAssetId { get; set; }
    [Range(1, 600)]
    public int ApiTimeoutSeconds { get; set; } = 30;
    public SignatureType DefaultSignatureType { get; set; }
    public bool AllowElectronicSign { get; set; }
    public bool AllowDigitalSign { get; set; }
    public bool RequireOtp { get; set; }
    [Range(1, 2000)]
    public int SignWidth { get; set; } = 150;
    [Range(1, 2000)]
    public int SignHeight { get; set; } = 70;
    [Required, MaxLength(SigningConsts.SignedFileSuffixMaxLength)]
    public string SignedFileSuffix { get; set; } = "-signed";
    public bool KeepOriginalFile { get; set; } = true;
    public bool OverwriteSignedFile { get; set; }
    public bool EnableSignLog { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class UserSignatureDto : AuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public Guid SignatureSettingId { get; set; }
    public Guid IdentityUserId { get; set; }
    public SignatureType SignatureType { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string? TokenReference { get; set; }
    public bool HasSecret { get; set; }
    public Guid? SealAssetId { get; set; }
    public Guid SignatureAssetId { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public bool IsActive { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUserSignatureDto
{
    public Guid? IdentityUserId { get; set; }
    public SignatureType SignatureType { get; set; }
    [Required, MaxLength(SigningConsts.ProviderCodeMaxLength)]
    public string ProviderCode { get; set; } = string.Empty;
    [MaxLength(SigningConsts.TokenReferenceMaxLength)]
    public string? TokenReference { get; set; }
    [DisableAuditing]
    public string? Secret { get; set; }
    public Guid? SealAssetId { get; set; }
    public Guid SignatureAssetId { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserSignatureDto : CreateUserSignatureDto, IHasConcurrencyStamp
{
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class SigningAssetDto : AuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public SigningAssetKind Kind { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class ElectronicSignInput
{
    public Guid SourceFileId { get; set; }
    public Guid UserSignatureId { get; set; }
    [Required]
    public string AssignmentConcurrencyStamp { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? Comment { get; set; }
}

public class DigitalSignInput : ElectronicSignInput
{
}

public class SigningAttemptDto : AuditedEntityDto<Guid>
{
    public Guid WorkflowInstanceId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid SourceFileId { get; set; }
    public Guid UserSignatureId { get; set; }
    public Guid SignerUserId { get; set; }
    public SignatureType SignatureType { get; set; }
    public SigningAttemptStatus Status { get; set; }
    public Guid? ResultFileId { get; set; }
    public string SourceSha256 { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int AttemptCount { get; set; }
}

public interface ISignatureSettingAppService : IApplicationService
{
    Task<SignatureSettingDto> GetAsync(Guid id);
    Task<PagedResultDto<SignatureSettingDto>> GetListAsync(SigningListInput input);
    Task<SignatureSettingDto> CreateAsync(CreateUpdateSignatureSettingDto input);
    Task<SignatureSettingDto> UpdateAsync(Guid id, CreateUpdateSignatureSettingDto input);
    Task DeleteAsync(Guid id, string concurrencyStamp);
}

public interface IUserSignatureAppService : IApplicationService
{
    Task<UserSignatureDto> GetAsync(Guid id);
    Task<PagedResultDto<UserSignatureDto>> GetListAsync(SigningListInput input);
    Task<UserSignatureDto> CreateAsync(CreateUserSignatureDto input);
    Task<UserSignatureDto> UpdateAsync(Guid id, UpdateUserSignatureDto input);
    Task<UserSignatureDto> RevokeCredentialAsync(
        Guid id,
        string concurrencyStamp);
    Task DeleteAsync(Guid id, string concurrencyStamp);
}

public interface ISigningExecutionAppService : IApplicationService
{
    Task<SigningAttemptDto> GetAsync(Guid id);
    Task<SigningAttemptDto> ExecuteElectronicAsync(
        Guid assignmentId,
        ElectronicSignInput input);
    Task<SigningAttemptDto> ExecuteDigitalAsync(
        Guid assignmentId,
        DigitalSignInput input);
}

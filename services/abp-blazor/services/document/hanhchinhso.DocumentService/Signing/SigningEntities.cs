using hanhchinhso.DocumentService.Signing;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Signing;

public class SignatureSetting : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public SignatureProviderType ProviderType { get; private set; }
    public string ApiEndpoint { get; private set; } = string.Empty;
    public Guid? LayoutAssetId { get; private set; }
    public int ApiTimeoutSeconds { get; private set; }
    public SignatureType DefaultSignatureType { get; private set; }
    public bool AllowElectronicSign { get; private set; }
    public bool AllowDigitalSign { get; private set; }
    public bool RequireOtp { get; private set; }
    public int SignWidth { get; private set; }
    public int SignHeight { get; private set; }
    public string SignedFileSuffix { get; private set; } = string.Empty;
    public bool KeepOriginalFile { get; private set; }
    public bool OverwriteSignedFile { get; private set; }
    public bool EnableSignLog { get; private set; }
    public bool IsActive { get; private set; }

    protected SignatureSetting() { }

    public SignatureSetting(Guid id, Guid? tenantId, CreateUpdateSignatureSettingDto input) : base(id)
    {
        TenantId = tenantId;
        Update(input);
    }

    public void Update(CreateUpdateSignatureSettingDto input)
    {
        ProviderCode = NormalizeProviderCode(input.ProviderCode);
        if (!Enum.IsDefined(input.ProviderType) ||
            !Enum.IsDefined(input.DefaultSignatureType))
        {
            throw new BusinessException("DocumentService:InvalidSigningEnum");
        }
        if (!Uri.TryCreate(input.ApiEndpoint?.Trim(), UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttp &&
             endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new BusinessException("DocumentService:InvalidSigningEndpoint");
        }
        if (input.ApiTimeoutSeconds is < 1 or > 600 ||
            input.SignWidth is < 1 or > 2000 ||
            input.SignHeight is < 1 or > 2000)
        {
            throw new BusinessException("DocumentService:InvalidSigningDimensions");
        }
        ProviderType = input.ProviderType;
        ApiEndpoint = Check.Length(endpoint.AbsoluteUri, nameof(input.ApiEndpoint),
            SigningConsts.EndpointMaxLength);
        LayoutAssetId = input.LayoutAssetId;
        ApiTimeoutSeconds = input.ApiTimeoutSeconds;
        DefaultSignatureType = input.DefaultSignatureType;
        AllowElectronicSign = input.AllowElectronicSign;
        AllowDigitalSign = input.AllowDigitalSign;
        RequireOtp = input.RequireOtp;
        SignWidth = input.SignWidth;
        SignHeight = input.SignHeight;
        SignedFileSuffix = Check.NotNullOrWhiteSpace(
            input.SignedFileSuffix, nameof(input.SignedFileSuffix),
            SigningConsts.SignedFileSuffixMaxLength).Trim();
        if (SignedFileSuffix.IndexOfAny(
                ['/', '\\', '\r', '\n', '\0']) >= 0)
        {
            throw new BusinessException(
                "DocumentService:InvalidSignedFileSuffix");
        }
        KeepOriginalFile = input.KeepOriginalFile;
        OverwriteSignedFile = input.OverwriteSignedFile;
        EnableSignLog = input.EnableSignLog;
        IsActive = input.IsActive;
    }

    internal static string NormalizeProviderCode(string value) =>
        Check.NotNullOrWhiteSpace(
            value, nameof(value), SigningConsts.ProviderCodeMaxLength)
            .Trim().ToUpperInvariant();

    internal static string? NormalizeOptional(string? value, int maxLength) =>
        value.IsNullOrWhiteSpace()
            ? null
            : Check.Length(value.Trim(), nameof(value), maxLength);
}

public class UserSignature : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid SignatureSettingId { get; private set; }
    public Guid IdentityUserId { get; private set; }
    public SignatureType SignatureType { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public string? TokenReference { get; private set; }
    public string? ProtectedSecret { get; private set; }
    public Guid? SealAssetId { get; private set; }
    public Guid SignatureAssetId { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidToUtc { get; private set; }
    public bool IsActive { get; private set; }
    public bool HasSecret => !ProtectedSecret.IsNullOrWhiteSpace();

    protected UserSignature() { }

    public UserSignature(
        Guid id,
        Guid? tenantId,
        Guid signatureSettingId,
        Guid identityUserId,
        CreateUserSignatureDto input,
        string? protectedSecret) : base(id)
    {
        TenantId = tenantId;
        Update(signatureSettingId, identityUserId, input, protectedSecret);
    }

    public void Update(
        Guid signatureSettingId,
        Guid identityUserId,
        CreateUserSignatureDto input,
        string? protectedSecret)
    {
        if (signatureSettingId == Guid.Empty || identityUserId == Guid.Empty ||
            !Enum.IsDefined(input.SignatureType))
        {
            throw new BusinessException("DocumentService:InvalidUserSignature");
        }
        if (input.ValidFromUtc.HasValue && input.ValidToUtc.HasValue &&
            input.ValidToUtc.Value < input.ValidFromUtc.Value)
        {
            throw new BusinessException("DocumentService:InvalidSignatureValidity");
        }
        SignatureSettingId = signatureSettingId;
        IdentityUserId = identityUserId;
        SignatureType = input.SignatureType;
        ProviderCode = SignatureSetting.NormalizeProviderCode(input.ProviderCode);
        TokenReference = SignatureSetting.NormalizeOptional(
            input.TokenReference, SigningConsts.TokenReferenceMaxLength);
        if (input.SignatureAssetId == Guid.Empty)
        {
            throw new BusinessException(
                "DocumentService:SignatureAssetRequired");
        }
        SealAssetId = input.SealAssetId;
        SignatureAssetId = input.SignatureAssetId;
        ValidFromUtc = input.ValidFromUtc;
        ValidToUtc = input.ValidToUtc;
        IsActive = input.IsActive;
        if (!protectedSecret.IsNullOrWhiteSpace())
        {
            ProtectedSecret = protectedSecret;
        }
        if (SignatureType == SignatureType.Electronic &&
            (!TokenReference.IsNullOrWhiteSpace() || HasSecret))
        {
            throw new BusinessException(
                "DocumentService:ElectronicSignatureCredentialForbidden");
        }
        if (SignatureType == SignatureType.Digital &&
            IsActive &&
            (TokenReference.IsNullOrWhiteSpace() || !HasSecret))
        {
            throw new BusinessException(
                "DocumentService:DigitalSignatureCredentialRequired");
        }
    }

    public void RevokeCredential()
    {
        IsActive = false;
        TokenReference = null;
        ProtectedSecret = null;
    }
}

public class SigningAsset : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public SigningAssetKind Kind { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public bool BlobDeletionPending { get; private set; }

    protected SigningAsset() { }

    public SigningAsset(
        Guid id,
        Guid? tenantId,
        SigningAssetKind kind,
        Guid? ownerUserId,
        string displayName,
        string blobName,
        string mimeType,
        long size,
        string sha256) : base(id)
    {
        if (!Enum.IsDefined(kind) || size <= 0)
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningAsset");
        }
        if (kind != SigningAssetKind.LayoutImage &&
            !ownerUserId.HasValue)
        {
            throw new BusinessException(
                "DocumentService:SigningAssetOwnerRequired");
        }
        TenantId = tenantId;
        Kind = kind;
        OwnerUserId = ownerUserId;
        DisplayName = Check.NotNullOrWhiteSpace(
            displayName, nameof(displayName), 255);
        BlobName = Check.NotNullOrWhiteSpace(
            blobName, nameof(blobName), SigningConsts.BlobNameMaxLength);
        MimeType = Check.NotNullOrWhiteSpace(
            mimeType, nameof(mimeType), 127);
        Size = size;
        Sha256 = Check.NotNullOrWhiteSpace(
            sha256, nameof(sha256), 64);
    }

    public void MarkBlobDeletionPending() =>
        BlobDeletionPending = true;
}

public class SigningBlobCleanup : BasicAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string BlobName { get; private set; } = string.Empty;
    public DateTime CreationTime { get; private set; }

    protected SigningBlobCleanup() { }

    public SigningBlobCleanup(
        Guid id,
        Guid? tenantId,
        string blobName) : base(id)
    {
        TenantId = tenantId;
        BlobName = Check.NotNullOrWhiteSpace(
            blobName, nameof(blobName), SigningConsts.BlobNameMaxLength);
        CreationTime = DateTime.UtcNow;
    }
}

using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Signing;

public class SigningAttempt :
    FullAuditedAggregateRoot<Guid>,
    IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid WorkflowInstanceId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid SourceFileId { get; private set; }
    public Guid UserSignatureId { get; private set; }
    public Guid SignerUserId { get; private set; }
    public SignatureType SignatureType { get; private set; }
    public SigningAttemptStatus Status { get; private set; }
    public Guid? ResultFileId { get; private set; }
    public Guid? PendingResultFileId { get; private set; }
    public string? PendingResultBlobName { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string SourceSha256 { get; private set; } = string.Empty;
    public string UserSignatureConcurrencyStamp { get; private set; } =
        string.Empty;
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public string? FailureCode { get; private set; }
    public int AttemptCount { get; private set; }

    protected SigningAttempt() { }

    public SigningAttempt(
        Guid id,
        Guid? tenantId,
        Guid workflowInstanceId,
        Guid assignmentId,
        Guid sourceFileId,
        Guid userSignatureId,
        Guid signerUserId,
        SignatureType signatureType,
        string idempotencyKey,
        string sourceSha256,
        string userSignatureConcurrencyStamp) : base(id)
    {
        if (workflowInstanceId == Guid.Empty ||
            assignmentId == Guid.Empty ||
            sourceFileId == Guid.Empty ||
            userSignatureId == Guid.Empty ||
            signerUserId == Guid.Empty ||
            !Enum.IsDefined(signatureType))
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningAttempt");
        }
        TenantId = tenantId;
        WorkflowInstanceId = workflowInstanceId;
        AssignmentId = assignmentId;
        SourceFileId = sourceFileId;
        UserSignatureId = userSignatureId;
        SignerUserId = signerUserId;
        SignatureType = signatureType;
        IdempotencyKey = RequireHash(
            idempotencyKey, nameof(idempotencyKey));
        SourceSha256 = RequireHash(
            sourceSha256, nameof(sourceSha256));
        UserSignatureConcurrencyStamp = Check.NotNullOrWhiteSpace(
            userSignatureConcurrencyStamp,
            nameof(userSignatureConcurrencyStamp),
            40);
        Status = SigningAttemptStatus.Pending;
    }

    public void Start(DateTime startedAtUtc)
    {
        if (Status is SigningAttemptStatus.Succeeded or
            SigningAttemptStatus.Cancelled ||
            PendingResultFileId.HasValue ||
            !PendingResultBlobName.IsNullOrWhiteSpace())
        {
            throw new BusinessException(
                "DocumentService:SigningAttemptTerminal");
        }
        Status = SigningAttemptStatus.Processing;
        StartedAtUtc = ToUtc(startedAtUtc);
        FinishedAtUtc = null;
        FailureCode = null;
        AttemptCount++;
    }

    public void Succeed(Guid resultFileId, DateTime finishedAtUtc)
    {
        if (Status != SigningAttemptStatus.Processing ||
            resultFileId == Guid.Empty)
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningAttemptTransition");
        }
        ResultFileId = resultFileId;
        PendingResultFileId = null;
        PendingResultBlobName = null;
        Status = SigningAttemptStatus.Succeeded;
        FinishedAtUtc = ToUtc(finishedAtUtc);
        FailureCode = null;
    }

    public void ReserveResult(Guid fileId, string blobName)
    {
        if (Status != SigningAttemptStatus.Processing ||
            fileId == Guid.Empty)
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningAttemptTransition");
        }
        PendingResultFileId = fileId;
        PendingResultBlobName = Check.NotNullOrWhiteSpace(
            blobName, nameof(blobName), 500);
    }

    public void ClearPendingResult()
    {
        PendingResultFileId = null;
        PendingResultBlobName = null;
    }

    public void Fail(string failureCode, DateTime finishedAtUtc)
    {
        if (Status != SigningAttemptStatus.Processing)
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningAttemptTransition");
        }
        Status = SigningAttemptStatus.Failed;
        FinishedAtUtc = ToUtc(finishedAtUtc);
        FailureCode = Check.NotNullOrWhiteSpace(
            failureCode, nameof(failureCode), 100);
    }

    private static string RequireHash(string value, string name)
    {
        var normalized = Check.NotNullOrWhiteSpace(
            value, name, 64).Trim().ToLowerInvariant();
        if (normalized.Length != 64 ||
            normalized.Any(x => !Uri.IsHexDigit(x)))
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningHash");
        }
        return normalized;
    }

    private static DateTime ToUtc(DateTime value) =>
        DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
}

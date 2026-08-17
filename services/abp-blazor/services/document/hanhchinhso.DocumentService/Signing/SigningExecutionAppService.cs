using System.Security.Cryptography;
using System.Text;
using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Permissions;
using hanhchinhso.DocumentService.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Uow;

namespace hanhchinhso.DocumentService.Signing;

[Authorize(DocumentServicePermissions.SigningExecution.Default)]
public class SigningExecutionAppService :
    ApplicationService,
    ISigningExecutionAppService
{
    private readonly DocumentServiceDbContext _db;
    private readonly IBlobContainer<DocumentBlobContainer> _documentBlobs;
    private readonly IBlobContainer<SigningBlobContainer> _signingBlobs;
    private readonly DocumentFileManager _fileManager;
    private readonly IElectronicPdfSigner _pdfSigner;
    private readonly IPdfSigningPlaceholderLocator _placeholderLocator;
    private readonly IRemoteCaSigningProvider _remoteCa;
    private readonly IBnnSigningProvider _bnn;
    private readonly IUserSignatureSecretProtector _secretProtector;
    private readonly WorkflowSignCompletionService _completion;
    private readonly IAbpDistributedLock _locks;
    private readonly IUnitOfWorkManager _uows;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SigningExecutionAppService> _logger;

    public SigningExecutionAppService(
        DocumentServiceDbContext db,
        IBlobContainer<DocumentBlobContainer> documentBlobs,
        IBlobContainer<SigningBlobContainer> signingBlobs,
        DocumentFileManager fileManager,
        IElectronicPdfSigner pdfSigner,
        IPdfSigningPlaceholderLocator placeholderLocator,
        IRemoteCaSigningProvider remoteCa,
        IBnnSigningProvider bnn,
        IUserSignatureSecretProtector secretProtector,
        WorkflowSignCompletionService completion,
        IAbpDistributedLock locks,
        IUnitOfWorkManager uows,
        IConfiguration configuration,
        ILogger<SigningExecutionAppService> logger)
    {
        _db = db;
        _documentBlobs = documentBlobs;
        _signingBlobs = signingBlobs;
        _fileManager = fileManager;
        _pdfSigner = pdfSigner;
        _placeholderLocator = placeholderLocator;
        _remoteCa = remoteCa;
        _bnn = bnn;
        _secretProtector = secretProtector;
        _completion = completion;
        _locks = locks;
        _uows = uows;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SigningAttemptDto> GetAsync(Guid id)
    {
        var attempt = await _db.SigningAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(
                typeof(SigningAttempt), id);
        EnsureOwner(attempt.SignerUserId);
        return Map(attempt);
    }

    [Authorize(DocumentServicePermissions.SigningExecution.Execute)]
    [UnitOfWork(IsDisabled = true)]
    public Task<SigningAttemptDto> ExecuteElectronicAsync(
        Guid assignmentId,
        ElectronicSignInput input) =>
        ExecuteAsync(assignmentId, input, SignatureType.Electronic);

    [Authorize(DocumentServicePermissions.SigningExecution.Execute)]
    [UnitOfWork(IsDisabled = true)]
    public Task<SigningAttemptDto> ExecuteDigitalAsync(
        Guid assignmentId,
        DigitalSignInput input) =>
        ExecuteAsync(assignmentId, input, SignatureType.Digital);

    private async Task<SigningAttemptDto> ExecuteAsync(
        Guid assignmentId,
        ElectronicSignInput input,
        SignatureType signatureType)
    {
        var replay = await _db.SigningAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.AssignmentId == assignmentId &&
                x.SourceFileId == input.SourceFileId &&
                x.UserSignatureId == input.UserSignatureId &&
                x.SignatureType == signatureType &&
                x.Status == SigningAttemptStatus.Succeeded);
        if (replay is not null)
        {
            EnsureOwner(replay.SignerUserId);
            return Map(replay);
        }
        var state = await LoadAndValidateAsync(
            assignmentId, input, signatureType);
        var sourceBytes = await ReadBlobAsync(
            _documentBlobs,
            state.Source.BlobName,
            MaxPdfBytes,
            CancellationToken.None);
        var sourceHash = Hash(sourceBytes);
        if (!state.Source.Hash.IsNullOrWhiteSpace() &&
            !string.Equals(
                state.Source.Hash,
                sourceHash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "DocumentService:SourceFileHashMismatch");
        }
        var idempotencyKey = Hash(
            Encoding.UTF8.GetBytes(
                $"{CurrentTenant.Id:N}|{state.Assignment.InstanceId:N}|" +
                $"{assignmentId:N}|{sourceHash}|{state.Signature.Id:N}"));
        var tenantKey = CurrentTenant.Id?.ToString("N") ?? "host";
        await using var handle = await _locks.TryAcquireAsync(
            $"document-signing-attempt:{tenantKey}:{idempotencyKey}",
            TimeSpan.FromSeconds(30));
        if (handle is null)
        {
            throw new UserFriendlyException(
                "The signing request is already processing.");
        }

        var attempt = await StartAttemptAsync(
            state, idempotencyKey, sourceHash);
        if (attempt.Status == SigningAttemptStatus.Succeeded)
        {
            return Map(attempt);
        }

        DocumentFile? resultFile = null;
        var resultSaved = false;
        try
        {
            var imageBytes = await ReadBlobAsync(
                _signingBlobs,
                state.Asset.BlobName,
                10_485_760,
                CancellationToken.None);
            if (!string.Equals(
                    Hash(imageBytes),
                    state.Asset.Sha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "DocumentService:SigningAssetHashMismatch");
            }
            var signedBytes = signatureType == SignatureType.Electronic
                ? _pdfSigner.Sign(
                    sourceBytes,
                    imageBytes,
                    state.StepOrder,
                    state.Setting.SignWidth,
                    state.Setting.SignHeight)
                : await SignDigitalAsync(
                    attempt,
                    state,
                    sourceBytes,
                    imageBytes,
                    CancellationToken.None);
            if (signedBytes.Length == 0 ||
                signedBytes.Length > MaxPdfBytes)
            {
                throw new BusinessException(
                    "DocumentService:InvalidSignedPdf");
            }
            ValidateSignedPdf(signedBytes);
            if (signatureType == SignatureType.Digital &&
                string.Equals(
                    Hash(signedBytes),
                    sourceHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "DocumentService:UnchangedProviderSigningOutput");
            }
            resultFile = CreateResultFile(state, signedBytes);
            await ReserveResultAsync(attempt.Id, resultFile);
            await using var content =
                new MemoryStream(signedBytes, writable: false);
            await _fileManager.SaveAsync(
                resultFile, content, CancellationToken.None);
            resultSaved = true;
            await _completion.CompleteAsync(
                attempt.Id,
                resultFile.Id,
                state.Setting.ConcurrencyStamp,
                input.Comment,
                CancellationToken.None);
            return Map(await _db.SigningAttempts
                .AsNoTracking()
                .SingleAsync(x => x.Id == attempt.Id));
        }
        catch
        {
            var clearPendingResult = resultFile is null;
            if (resultFile is not null)
            {
                try
                {
                    if (resultSaved ||
                        await ResultFileExistsAsync(resultFile.Id))
                    {
                        await _fileManager.RequestDeleteAsync(
                            resultFile.Id,
                            resultFile.ConcurrencyStamp,
                            CancellationToken.None);
                    }
                    clearPendingResult = true;
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(
                        cleanupException,
                        "Failed to schedule signed artifact {FileId} cleanup",
                        resultFile.Id);
                }
            }
            await MarkFailedAsync(attempt.Id, clearPendingResult);
            throw;
        }
    }

    private async Task<bool> ResultFileExistsAsync(Guid resultFileId)
    {
        using var uow = _uows.Begin(
            requiresNew: true, isTransactional: false);
        var exists = await _db.DocumentFiles
            .AsNoTracking()
            .AnyAsync(x => x.Id == resultFileId);
        await uow.CompleteAsync();
        return exists;
    }

    private long MaxPdfBytes =>
        _configuration.GetValue<long?>(
            "Signing:MaxSignedPdfBytes") ?? 104_857_600;

    private async Task<ExecutionState> LoadAndValidateAsync(
        Guid assignmentId,
        ElectronicSignInput input,
        SignatureType signatureType)
    {
        var userId = CurrentUser.Id ?? throw new AbpAuthorizationException();
        var assignment = await _db.DocumentAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == assignmentId)
            ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(
                typeof(DocumentAssignment), assignmentId);
        if (assignment.ReceiverUserId != userId ||
            assignment.Action != DocumentAssignmentAction.Sign ||
            assignment.Status != DocumentAssignmentStatus.Pending ||
            !assignment.IsCurrent ||
            assignment.ConcurrencyStamp !=
                input.AssignmentConcurrencyStamp)
        {
            throw new BusinessException(
                "DocumentService:SigningAssignmentNotActionable");
        }
        var instance = await _db.DocumentWorkflowInstances
            .AsNoTracking()
            .SingleAsync(x => x.Id == assignment.InstanceId);
        if (instance.Status is not (
                DocumentWorkflowStatus.InProgress or
                DocumentWorkflowStatus.Overdue) ||
            !await _db.DocumentWorkflowInstanceLogs.AnyAsync(x =>
                x.AssignmentId == assignmentId &&
                x.Action == WorkflowRuntimeAction.RequestSign))
        {
            throw new BusinessException(
                "DocumentService:SigningIntentRequired");
        }
        var source = await _db.DocumentFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == input.SourceFileId &&
                x.DocumentId == assignment.DocumentId &&
                !x.BlobDeletionPending)
            ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(
                typeof(DocumentFile), input.SourceFileId);
        var canonicalSourceFileId =
            instance.CurrentSignedFileId ?? instance.SourceFileId;
        if (source.Id != canonicalSourceFileId)
        {
            throw new BusinessException(
                "DocumentService:SigningCanonicalSourceChanged");
        }
        if (!string.Equals(
                source.MimeType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "DocumentService:SigningSourceMustBePdf");
        }
        var signature = await _db.UserSignatures
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == input.UserSignatureId &&
                x.IdentityUserId == userId &&
                x.SignatureType == signatureType &&
                x.IsActive)
            ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(
                typeof(UserSignature), input.UserSignatureId);
        var now = Clock.Now.ToUniversalTime();
        if (signature.ValidFromUtc > now || signature.ValidToUtc < now)
        {
            throw new BusinessException(
                "DocumentService:SignatureOutsideValidityWindow");
        }
        var setting = await _db.SignatureSettings
            .AsNoTracking()
            .SingleAsync(x =>
                x.Id == signature.SignatureSettingId &&
                x.ProviderCode == signature.ProviderCode &&
                x.IsActive &&
                (signatureType == SignatureType.Electronic
                    ? x.AllowElectronicSign
                    : x.AllowDigitalSign));
        var asset = await _db.SigningAssets
            .AsNoTracking()
            .SingleAsync(x =>
                x.Id == signature.SignatureAssetId &&
                x.Kind == SigningAssetKind.SignatureImage &&
                x.OwnerUserId == userId &&
                !x.BlobDeletionPending);
        var stepOrder = await _db.DocumentWorkflowCommittedSteps
            .AsNoTracking()
            .Where(x => x.Id == assignment.CommittedStepId)
            .Select(x => x.Order)
            .SingleAsync();
        return new(
            assignment, source, signature, setting, asset, stepOrder);
    }

    private async Task<SigningAttempt> StartAttemptAsync(
        ExecutionState state,
        string idempotencyKey,
        string sourceHash)
    {
        var tenantKey = CurrentTenant.Id?.ToString("N") ?? "host";
        await using var documentHandle = await _locks.TryAcquireAsync(
            $"document-workflow-document:{tenantKey}:" +
            $"{state.Assignment.DocumentId:N}",
            TimeSpan.FromSeconds(30));
        if (documentHandle is null)
        {
            throw new UserFriendlyException(
                "The document workflow is busy. Please retry.");
        }
        using var uow = _uows.Begin(
            requiresNew: true, isTransactional: true);
        var liveAssignment = await _db.DocumentAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == state.Assignment.Id);
        var liveInstance = await _db.DocumentWorkflowInstances
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == state.Assignment.InstanceId);
        var now = Clock.Now.ToUniversalTime();
        var snapshotIsCurrent =
            liveAssignment is not null &&
            liveAssignment.ReceiverUserId ==
                state.Assignment.ReceiverUserId &&
            liveAssignment.Action == DocumentAssignmentAction.Sign &&
            liveAssignment.ConcurrencyStamp ==
                state.Assignment.ConcurrencyStamp &&
            liveAssignment.Status == DocumentAssignmentStatus.Pending &&
            liveAssignment.IsCurrent &&
            liveInstance is not null &&
            liveInstance.Status is (
                DocumentWorkflowStatus.InProgress or
                DocumentWorkflowStatus.Overdue) &&
            (liveInstance.CurrentSignedFileId ??
             liveInstance.SourceFileId) == state.Source.Id &&
            await _db.DocumentWorkflowInstanceLogs.AnyAsync(x =>
                x.AssignmentId == liveAssignment.Id &&
                x.Action == WorkflowRuntimeAction.RequestSign) &&
            await _db.DocumentFiles.AnyAsync(x =>
                x.Id == state.Source.Id &&
                x.DocumentId == liveAssignment.DocumentId &&
                !x.BlobDeletionPending) &&
            await _db.UserSignatures.AnyAsync(x =>
                x.Id == state.Signature.Id &&
                x.IdentityUserId == liveAssignment.ReceiverUserId &&
                x.SignatureType == state.Signature.SignatureType &&
                x.IsActive &&
                x.ConcurrencyStamp ==
                    state.Signature.ConcurrencyStamp &&
                (!x.ValidFromUtc.HasValue ||
                 x.ValidFromUtc <= now) &&
                (!x.ValidToUtc.HasValue ||
                 x.ValidToUtc >= now)) &&
            await _db.SignatureSettings.AnyAsync(x =>
                x.Id == state.Setting.Id &&
                x.ConcurrencyStamp ==
                    state.Setting.ConcurrencyStamp &&
                x.IsActive &&
                (state.Signature.SignatureType ==
                    SignatureType.Electronic
                    ? x.AllowElectronicSign
                    : x.AllowDigitalSign)) &&
            await _db.SigningAssets.AnyAsync(x =>
                x.Id == state.Asset.Id &&
                x.OwnerUserId == liveAssignment.ReceiverUserId &&
                x.Kind == SigningAssetKind.SignatureImage &&
                !x.BlobDeletionPending &&
                x.Sha256 == state.Asset.Sha256);
        if (!snapshotIsCurrent)
        {
            throw new BusinessException(
                "DocumentService:SigningStateChanged");
        }
        var attempt = await _db.SigningAttempts
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
        if (attempt?.Status == SigningAttemptStatus.Succeeded)
        {
            await uow.CompleteAsync();
            return attempt;
        }
        if (attempt is null)
        {
            attempt = new SigningAttempt(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                state.Assignment.InstanceId,
                state.Assignment.Id,
                state.Source.Id,
                state.Signature.Id,
                state.Signature.IdentityUserId,
                state.Signature.SignatureType,
                idempotencyKey,
                sourceHash,
                state.Signature.ConcurrencyStamp);
            _db.SigningAttempts.Add(attempt);
        }
        else if (attempt.UserSignatureConcurrencyStamp !=
                 state.Signature.ConcurrencyStamp)
        {
            throw new BusinessException(
                "DocumentService:SigningCredentialChanged");
        }
        attempt.Start(Clock.Now.ToUniversalTime());
        await _db.SaveChangesAsync();
        await uow.CompleteAsync();
        return attempt;
    }

    private async Task MarkFailedAsync(
        Guid attemptId,
        bool clearPendingResult)
    {
        using var uow = _uows.Begin(
            requiresNew: true, isTransactional: true);
        var attempt = await _db.SigningAttempts
            .SingleOrDefaultAsync(x => x.Id == attemptId);
        if (attempt?.Status == SigningAttemptStatus.Processing)
        {
            if (clearPendingResult)
            {
                attempt.ClearPendingResult();
            }
            attempt.Fail(
                attempt.SignatureType == SignatureType.Digital
                    ? "DigitalSigningFailed"
                    : "ElectronicSigningFailed",
                Clock.Now.ToUniversalTime());
            await _db.SaveChangesAsync();
        }
        await uow.CompleteAsync();
    }

    private async Task ReserveResultAsync(
        Guid attemptId,
        DocumentFile resultFile)
    {
        using var uow = _uows.Begin(
            requiresNew: true, isTransactional: true);
        var attempt = await _db.SigningAttempts
            .SingleAsync(x => x.Id == attemptId);
        attempt.ReserveResult(resultFile.Id, resultFile.BlobName);
        await _db.SaveChangesAsync();
        await uow.CompleteAsync();
    }

    private async Task<byte[]> SignDigitalAsync(
        SigningAttempt attempt,
        ExecutionState state,
        byte[] sourceBytes,
        byte[] signatureImageBytes,
        CancellationToken cancellationToken)
    {
        if (state.Signature.TokenReference.IsNullOrWhiteSpace() ||
            state.Signature.ProtectedSecret.IsNullOrWhiteSpace())
        {
            throw new BusinessException(
                "DocumentService:DigitalSignatureCredentialRequired");
        }
        var secret = _secretProtector.Unprotect(
            state.Signature.TenantId,
            state.Signature.Id,
            state.Signature.ProviderCode,
            state.Signature.ProtectedSecret);
        var placeholder = $"<<Sign{state.StepOrder:D2}>>";
        var position = _placeholderLocator.Locate(
            sourceBytes, placeholder);
        var endpoint = new Uri(
            state.Setting.ApiEndpoint, UriKind.Absolute);
        if (state.Setting.ProviderType ==
            SignatureProviderType.RemoteCa)
        {
            var signerName =
                CurrentUser.Name ??
                CurrentUser.UserName ??
                state.Signature.IdentityUserId.ToString();
            return await _remoteCa.SignAsync(
                new RemoteCaSigningCommand(
                    attempt.Id,
                    state.Setting.ProviderCode,
                    endpoint,
                    state.Setting.ApiTimeoutSeconds,
                    state.Signature.TokenReference,
                    secret,
                    sourceBytes,
                    signatureImageBytes,
                    placeholder,
                    position.Page,
                    position.X,
                    position.Y,
                    state.Setting.SignWidth,
                    state.Setting.SignHeight,
                    $"{signerName}\r\nNgày ký:" +
                    Clock.Now.ToLocalTime().ToString(
                        "dd/MM/yyyy HH:mm:ss")),
                cancellationToken);
        }
        if (state.Setting.ProviderType ==
            SignatureProviderType.Hsm)
        {
            if (!state.Signature.SealAssetId.HasValue ||
                !state.Setting.LayoutAssetId.HasValue)
            {
                throw new BusinessException(
                    "DocumentService:BnnSigningAssetsRequired");
            }
            var seal = await ReadVerifiedSigningAssetAsync(
                state.Signature.SealAssetId.Value,
                SigningAssetKind.SealImage,
                state.Signature.IdentityUserId,
                cancellationToken);
            var layout = await ReadVerifiedSigningAssetAsync(
                state.Setting.LayoutAssetId.Value,
                SigningAssetKind.LayoutImage,
                ownerUserId: null,
                cancellationToken);
            return await _bnn.SignAsync(
                new BnnSigningCommand(
                    attempt.Id,
                    state.Setting.ProviderCode,
                    endpoint,
                    state.Setting.ApiTimeoutSeconds,
                    state.Signature.TokenReference,
                    secret,
                    sourceBytes,
                    signatureImageBytes,
                    seal,
                    layout,
                    placeholder,
                    state.Setting.SignWidth,
                    state.Setting.SignHeight),
                cancellationToken);
        }
        throw new BusinessException(
            "DocumentService:UnsupportedDigitalSigningProvider");
    }

    private async Task<byte[]> ReadVerifiedSigningAssetAsync(
        Guid id,
        SigningAssetKind kind,
        Guid? ownerUserId,
        CancellationToken cancellationToken)
    {
        var asset = await _db.SigningAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == id &&
                x.Kind == kind &&
                x.OwnerUserId == ownerUserId &&
                !x.BlobDeletionPending,
                cancellationToken)
            ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(
                typeof(SigningAsset), id);
        var bytes = await ReadBlobAsync(
            _signingBlobs,
            asset.BlobName,
            10_485_760,
            cancellationToken);
        if (!string.Equals(
                Hash(bytes),
                asset.Sha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "DocumentService:SigningAssetHashMismatch");
        }
        return bytes;
    }

    private DocumentFile CreateResultFile(
        ExecutionState state,
        byte[] bytes)
    {
        var id = GuidGenerator.Create();
        var tenant = CurrentTenant.Id?.ToString("N") ?? "host";
        var baseName = Path.GetFileNameWithoutExtension(
            state.Source.DisplayName);
        var displayName = $"{baseName}" +
            $"{state.Setting.SignedFileSuffix}.pdf";
        var file = new DocumentFile(
            id,
            CurrentTenant.Id,
            state.Assignment.DocumentId,
            displayName,
            $"{tenant}/{state.Assignment.DocumentId:N}/signed/{id:N}.pdf",
            "application/pdf",
            bytes.LongLength,
            Hash(bytes));
        file.MarkSigned(state.Source.Id);
        return file;
    }

    private static async Task<byte[]> ReadBlobAsync<TContainer>(
        IBlobContainer<TContainer> container,
        string blobName,
        long maxBytes,
        CancellationToken cancellationToken)
        where TContainer : class
    {
        await using var source = await container.GetAsync(
            blobName, cancellationToken);
        if (source.CanSeek && source.Length > maxBytes)
        {
            throw new BusinessException(
                "DocumentService:SigningBlobTooLarge");
        }
        using var target = new MemoryStream();
        var buffer = new byte[81_920];
        int read;
        while ((read = await source.ReadAsync(
                   buffer, cancellationToken)) > 0)
        {
            if (target.Length + read > maxBytes)
            {
                throw new BusinessException(
                    "DocumentService:SigningBlobTooLarge");
            }
            await target.WriteAsync(
                buffer.AsMemory(0, read), cancellationToken);
        }
        return target.ToArray();
    }

    private void EnsureOwner(Guid userId)
    {
        if (CurrentUser.Id != userId)
        {
            throw new AbpAuthorizationException();
        }
    }

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void ValidateSignedPdf(byte[] bytes)
    {
        try
        {
            using var document =
                UglyToad.PdfPig.PdfDocument.Open(bytes);
            if (document.NumberOfPages < 1)
            {
                throw new BusinessException(
                    "DocumentService:InvalidSignedPdf");
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BusinessException(
                "DocumentService:InvalidSignedPdf",
                innerException: exception);
        }
    }

    private static SigningAttemptDto Map(SigningAttempt attempt) => new()
    {
        Id = attempt.Id,
        WorkflowInstanceId = attempt.WorkflowInstanceId,
        AssignmentId = attempt.AssignmentId,
        SourceFileId = attempt.SourceFileId,
        UserSignatureId = attempt.UserSignatureId,
        SignerUserId = attempt.SignerUserId,
        SignatureType = attempt.SignatureType,
        Status = attempt.Status,
        ResultFileId = attempt.ResultFileId,
        SourceSha256 = attempt.SourceSha256,
        StartedAtUtc = attempt.StartedAtUtc,
        FinishedAtUtc = attempt.FinishedAtUtc,
        AttemptCount = attempt.AttemptCount,
        CreationTime = attempt.CreationTime,
        LastModificationTime = attempt.LastModificationTime
    };

    private sealed record ExecutionState(
        DocumentAssignment Assignment,
        DocumentFile Source,
        UserSignature Signature,
        SignatureSetting Setting,
        SigningAsset Asset,
        int StepOrder);
}

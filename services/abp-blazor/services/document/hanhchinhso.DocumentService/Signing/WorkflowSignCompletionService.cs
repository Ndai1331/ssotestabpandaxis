using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Workflows;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace hanhchinhso.DocumentService.Signing;

public sealed class WorkflowSignCompletionService : ITransientDependency
{
    private readonly DocumentServiceDbContext _db;
    private readonly IAbpDistributedLock _locks;
    private readonly IUnitOfWorkManager _uows;
    private readonly ICurrentUser _currentUser;

    public WorkflowSignCompletionService(
        DocumentServiceDbContext db,
        IAbpDistributedLock locks,
        IUnitOfWorkManager uows,
        ICurrentUser currentUser)
    {
        _db = db;
        _locks = locks;
        _uows = uows;
        _currentUser = currentUser;
    }

    public async Task CompleteAsync(
        Guid attemptId,
        Guid resultFileId,
        string signatureSettingConcurrencyStamp,
        string? comment,
        CancellationToken cancellationToken)
    {
        var attemptSnapshot = await _db.SigningAttempts
            .AsNoTracking()
            .SingleAsync(x => x.Id == attemptId, cancellationToken);
        var documentId = await _db.DocumentAssignments
            .AsNoTracking()
            .Where(x => x.Id == attemptSnapshot.AssignmentId)
            .Select(x => x.DocumentId)
            .SingleAsync(cancellationToken);
        var tenantKey = attemptSnapshot.TenantId?.ToString("N") ?? "host";
        await using var handle = await _locks.TryAcquireAsync(
            $"document-workflow-document:{tenantKey}:" +
            $"{documentId:N}",
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (handle is null)
        {
            throw new UserFriendlyException(
                "The document workflow is busy. Please retry.");
        }
        await using var metadataHandle = await _locks.TryAcquireAsync(
            SigningMutationLock.GetName(attemptSnapshot.TenantId),
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (metadataHandle is null)
        {
            throw new UserFriendlyException(
                "The signing metadata is busy. Please retry.");
        }

        using var uow = _uows.Begin(
            requiresNew: true, isTransactional: true);
        var attempt = await _db.SigningAttempts
            .SingleAsync(x => x.Id == attemptId, cancellationToken);
        var assignment = await _db.DocumentAssignments
            .SingleAsync(x => x.Id == attempt.AssignmentId, cancellationToken);
        var instance = await _db.DocumentWorkflowInstances
            .Include(x => x.Steps)
                .ThenInclude(x => x.Receivers)
            .SingleAsync(x => x.Id == assignment.InstanceId, cancellationToken);
        var now = DateTime.UtcNow;
        await ValidateAsync(
            attempt,
            assignment,
            instance,
            resultFileId,
            signatureSettingConcurrencyStamp,
            now,
            cancellationToken);

        assignment.Complete(now, resultFileId);
        attempt.Succeed(resultFileId, now);
        Append(
            instance,
            assignment,
            WorkflowRuntimeAction.ConfirmSign,
            now,
            comment);
        await AdvanceOrCompleteAsync(
            instance, assignment, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await uow.CompleteAsync(cancellationToken);
    }

    private async Task ValidateAsync(
        SigningAttempt attempt,
        DocumentAssignment assignment,
        DocumentWorkflowInstance instance,
        Guid resultFileId,
        string signatureSettingConcurrencyStamp,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ??
            throw new AbpAuthorizationException();
        if (attempt.Status != SigningAttemptStatus.Processing ||
            assignment.ReceiverUserId != userId ||
            assignment.Action != DocumentAssignmentAction.Sign ||
            assignment.Status != DocumentAssignmentStatus.Pending ||
            !assignment.IsCurrent ||
            instance.Status is not (
                DocumentWorkflowStatus.InProgress or
                DocumentWorkflowStatus.Overdue))
        {
            throw new BusinessException(
                "DocumentService:SigningStateChanged");
        }
        if ((instance.CurrentSignedFileId ?? instance.SourceFileId) !=
            attempt.SourceFileId)
        {
            throw new BusinessException(
                "DocumentService:SigningCanonicalSourceChanged");
        }
        var signature = await _db.UserSignatures
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == attempt.UserSignatureId,
                cancellationToken);
        if (!signature.IsActive ||
            signature.IdentityUserId != userId ||
            signature.SignatureType != attempt.SignatureType ||
            signature.ConcurrencyStamp !=
                attempt.UserSignatureConcurrencyStamp ||
            signature.ValidFromUtc > now ||
            signature.ValidToUtc < now)
        {
            throw new BusinessException(
                "DocumentService:SigningCredentialChanged");
        }
        var setting = await _db.SignatureSettings
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == signature.SignatureSettingId,
                cancellationToken);
        if (!setting.IsActive ||
            setting.ProviderCode != signature.ProviderCode ||
            setting.ConcurrencyStamp !=
                signatureSettingConcurrencyStamp ||
            (attempt.SignatureType == SignatureType.Electronic
                ? !setting.AllowElectronicSign
                : !setting.AllowDigitalSign))
        {
            throw new BusinessException(
                "DocumentService:SigningProviderChanged");
        }
        var result = await _db.DocumentFiles
            .AsNoTracking()
            .SingleAsync(x => x.Id == resultFileId, cancellationToken);
        if (!result.IsSigned ||
            result.SourceFileId != attempt.SourceFileId ||
            result.DocumentId != assignment.DocumentId ||
            result.BlobDeletionPending ||
            result.Hash.IsNullOrWhiteSpace())
        {
            throw new BusinessException(
                "DocumentService:InvalidSignedArtifact");
        }
        instance.SetCurrentSignedFile(resultFileId);
    }

    private async Task AdvanceOrCompleteAsync(
        DocumentWorkflowInstance instance,
        DocumentAssignment completed,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (instance.SignMode == WorkflowSignMode.Parallel &&
            await _db.DocumentAssignments.AnyAsync(
                x => x.InstanceId == instance.Id &&
                     x.Id != completed.Id &&
                     x.Status == DocumentAssignmentStatus.Pending,
                cancellationToken))
        {
            return;
        }
        var order = instance.Steps
            .Single(x => x.Id == completed.CommittedStepId).Order;
        var next = instance.SignMode == WorkflowSignMode.Sequential
            ? instance.Steps
                .Where(x => x.Order > order &&
                    x.Type != WorkflowStepType.View)
                .OrderBy(x => x.Order)
                .FirstOrDefault()
            : null;
        if (next is null)
        {
            var fromStatus = instance.Status;
            instance.Complete(now);
            var document = await _db.Documents
                .SingleAsync(
                    x => x.Id == instance.DocumentId,
                    cancellationToken);
            document.SetWorkflowStatus("WORKFLOW_COMPLETED", now);
            Append(
                instance,
                null,
                WorkflowRuntimeAction.Complete,
                now,
                null,
                fromStatus,
                DocumentWorkflowStatus.Completed);
            return;
        }
        var receiver = next.Receivers.Single(x => x.IsSelected);
        var nextAssignment = new DocumentAssignment(
            Guid.NewGuid(),
            instance.TenantId,
            instance.Id,
            instance.DocumentId,
            next.Id,
            receiver.UserId,
            next.Type == WorkflowStepType.Sign
                ? DocumentAssignmentAction.Sign
                : DocumentAssignmentAction.Process,
            now,
            isCurrent: true);
        _db.DocumentAssignments.Add(nextAssignment);
        instance.SetCurrentStep(next.Id);
        _db.DocumentHistories.Add(new DocumentHistory(
            Guid.NewGuid(),
            instance.TenantId,
            instance.DocumentId,
            instance.Id,
            WorkflowRuntimeAction.AssignUser,
            completed.ReceiverUserId,
            receiver.UserId,
            now));
    }

    private void Append(
        DocumentWorkflowInstance instance,
        DocumentAssignment? assignment,
        WorkflowRuntimeAction action,
        DateTime now,
        string? comment,
        DocumentWorkflowStatus? fromStatus = null,
        DocumentWorkflowStatus? toStatus = null)
    {
        _db.DocumentWorkflowInstanceLogs.Add(
            new DocumentWorkflowInstanceLog(
                Guid.NewGuid(),
                instance.TenantId,
                instance.Id,
                action,
                _currentUser.Id,
                fromStatus ?? instance.Status,
                toStatus ?? instance.Status,
                now,
                comment,
                assignment?.Id));
        _db.DocumentHistories.Add(new DocumentHistory(
            Guid.NewGuid(),
            instance.TenantId,
            instance.DocumentId,
            instance.Id,
            action,
            _currentUser.Id,
            assignment?.ReceiverUserId,
            now,
            comment));
    }
}

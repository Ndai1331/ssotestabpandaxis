using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace hanhchinhso.DocumentService.Workflows;

[Authorize(DocumentServicePermissions.WorkflowRuntime.Act)]
public class WorkflowActionAppService :
    ApplicationService,
    IWorkflowActionAppService
{
    private readonly DocumentServiceDbContext _dbContext;
    private readonly IWorkflowIdentityReferenceValidator _identityValidator;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowActionAppService(
        DocumentServiceDbContext dbContext,
        IWorkflowIdentityReferenceValidator identityValidator,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _dbContext = dbContext;
        _identityValidator = identityValidator;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public Task<DocumentWorkflowInstanceDto> ApproveAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input) =>
        ActOnAssignmentAsync(
            assignmentId,
            input,
            WorkflowRuntimeAction.Approve);

    public Task<DocumentWorkflowInstanceDto> RequestSignAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input) =>
        ActOnAssignmentAsync(
            assignmentId,
            input,
            WorkflowRuntimeAction.RequestSign);

    public Task<DocumentWorkflowInstanceDto> ReturnAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input) =>
        ActOnAssignmentAsync(
            assignmentId,
            input,
            WorkflowRuntimeAction.Return);

    public Task<DocumentWorkflowInstanceDto> RejectAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input) =>
        ActOnAssignmentAsync(
            assignmentId,
            input,
            WorkflowRuntimeAction.Reject);

    public async Task<DocumentWorkflowInstanceDto> CancelAsync(
        Guid instanceId,
        WorkflowCancelInput input)
    {
        var initial = await _dbContext.DocumentWorkflowInstances
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == instanceId)
            ?? throw new EntityNotFoundException(
                typeof(DocumentWorkflowInstance), instanceId);
        await using var handle = await AcquireDocumentLockAsync(initial.DocumentId);
        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true,
            isTransactional: true);
        var instance = await LoadInstanceAsync(instanceId);
        if (instance.InitiatorUserId != CurrentUser.Id)
        {
            throw new AbpAuthorizationException(
                "Only the workflow initiator can cancel it.");
        }
        if (instance.Status == DocumentWorkflowStatus.Cancelled)
        {
            return Map(instance);
        }
        EnsureConcurrency(
            instance.ConcurrencyStamp,
            input.InstanceConcurrencyStamp);
        if (await _dbContext.DocumentAssignments.AnyAsync(x =>
                x.InstanceId == instance.Id &&
                x.Action == DocumentAssignmentAction.Sign &&
                x.Status == DocumentAssignmentStatus.Done))
        {
            throw new UserFriendlyException(
                "A workflow with a completed signature cannot be cancelled.");
        }

        var now = Clock.Now.ToUniversalTime();
        var pending = await _dbContext.DocumentAssignments
            .Where(x =>
                x.InstanceId == instance.Id &&
                x.Status == DocumentAssignmentStatus.Pending)
            .ToListAsync();
        foreach (var assignment in pending)
        {
            assignment.Revoke(now);
        }
        var fromStatus = instance.Status;
        instance.Cancel(now);
        await FinishAsync(
            instance,
            WorkflowRuntimeAction.Cancel,
            fromStatus,
            now,
            input.Comment);
        await uow.CompleteAsync();
        return Map(instance);
    }

    public async Task<DocumentWorkflowInstanceDto> ReplaceSignerAsync(
        Guid assignmentId,
        WorkflowSignerReplacementInput input)
    {
        var initial = await _dbContext.DocumentAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == assignmentId)
            ?? throw new EntityNotFoundException(
                typeof(DocumentAssignment), assignmentId);
        await using var handle = await AcquireDocumentLockAsync(initial.DocumentId);
        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true,
            isTransactional: true);
        var assignment = await _dbContext.DocumentAssignments
            .SingleAsync(x => x.Id == assignmentId);
        var instance = await LoadInstanceAsync(assignment.InstanceId);
        if (instance.InitiatorUserId != CurrentUser.Id)
        {
            throw new AbpAuthorizationException(
                "Only the workflow initiator can replace a signer.");
        }
        EnsureActiveAndCurrent(instance, assignment);
        EnsureConcurrency(
            assignment.ConcurrencyStamp,
            input.AssignmentConcurrencyStamp);
        if (assignment.Action != DocumentAssignmentAction.Sign)
        {
            throw new UserFriendlyException(
                "Only a current SIGN assignment can replace its signer.");
        }
        var step = instance.Steps.Single(x => x.Id == assignment.CommittedStepId);
        if (step.Receivers.All(x => x.UserId != input.NewSignerUserId))
        {
            throw new UserFriendlyException(
                "The replacement signer is outside the committed candidate set.");
        }
        await _identityValidator.ValidateAsync(
            [input.NewSignerUserId],
            [],
            null);

        var previousSigner = assignment.ReceiverUserId;
        if (previousSigner == input.NewSignerUserId)
        {
            return Map(instance);
        }
        assignment.ReplaceReceiver(input.NewSignerUserId);
        var now = Clock.Now.ToUniversalTime();
        _dbContext.DocumentWorkflowInstanceLogs.Add(
            new DocumentWorkflowInstanceLog(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                instance.Id,
                WorkflowRuntimeAction.UpdateSigner,
                CurrentUser.Id,
                instance.Status,
                instance.Status,
                now,
                input.Comment,
                assignment.Id));
        _dbContext.DocumentHistories.Add(new DocumentHistory(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            instance.DocumentId,
            instance.Id,
            WorkflowRuntimeAction.UpdateSigner,
            previousSigner,
            input.NewSignerUserId,
            now,
            input.Comment));
        await _dbContext.SaveChangesAsync();
        await uow.CompleteAsync();
        return Map(instance);
    }

    [Authorize(DocumentServicePermissions.WorkflowRuntime.MarkOverdue)]
    public async Task<DocumentWorkflowInstanceDto> MarkOverdueAsync(
        Guid instanceId,
        WorkflowMarkOverdueInput input)
    {
        var initial = await _dbContext.DocumentWorkflowInstances
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == instanceId)
            ?? throw new EntityNotFoundException(
                typeof(DocumentWorkflowInstance), instanceId);
        await using var handle = await AcquireDocumentLockAsync(initial.DocumentId);
        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true,
            isTransactional: true);
        var instance = await LoadInstanceAsync(instanceId);
        if (instance.Status == DocumentWorkflowStatus.Overdue)
        {
            return Map(instance);
        }
        EnsureConcurrency(
            instance.ConcurrencyStamp,
            input.InstanceConcurrencyStamp);
        var now = Clock.Now.ToUniversalTime();
        instance.MarkOverdue(now);
        var document = await _dbContext.Documents
            .SingleAsync(x => x.Id == instance.DocumentId);
        document.SetWorkflowStatus("WORKFLOW_OVERDUE");
        AppendRecords(
            instance,
            null,
            WorkflowRuntimeAction.MarkOverdue,
            DocumentWorkflowStatus.InProgress,
            DocumentWorkflowStatus.Overdue,
            now,
            null);
        await _dbContext.SaveChangesAsync();
        await uow.CompleteAsync();
        return Map(instance);
    }

    public async Task<DocumentWorkflowInstanceDto> ExtendAsync(
        Guid instanceId,
        WorkflowExtensionInput input)
    {
        var initial = await _dbContext.DocumentWorkflowInstances
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == instanceId)
            ?? throw new EntityNotFoundException(
                typeof(DocumentWorkflowInstance), instanceId);
        await using var handle = await AcquireDocumentLockAsync(initial.DocumentId);
        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true,
            isTransactional: true);
        var instance = await LoadInstanceAsync(instanceId);
        if (instance.InitiatorUserId != CurrentUser.Id)
        {
            throw new AbpAuthorizationException(
                "Only the workflow initiator can extend it.");
        }
        EnsureConcurrency(
            instance.ConcurrencyStamp,
            input.InstanceConcurrencyStamp);
        if (input.Reason.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(
                "An extension reason is required.");
        }
        var now = Clock.Now.ToUniversalTime();
        var baseDate = instance.DeadlineAtUtc.HasValue &&
            instance.DeadlineAtUtc.Value > now
                ? instance.DeadlineAtUtc.Value
                : now;
        instance.Extend(
            input.BusinessDays,
            WorkflowBusinessDayCalculator.Add(
                baseDate,
                input.BusinessDays));
        var document = await _dbContext.Documents
            .SingleAsync(x => x.Id == instance.DocumentId);
        document.SetWorkflowStatus("WORKFLOW_IN_PROGRESS");
        AppendRecords(
            instance,
            null,
            WorkflowRuntimeAction.Extend,
            DocumentWorkflowStatus.Overdue,
            DocumentWorkflowStatus.InProgress,
            now,
            input.Reason);
        await _dbContext.SaveChangesAsync();
        await uow.CompleteAsync();
        return Map(instance);
    }

    private async Task<DocumentWorkflowInstanceDto> ActOnAssignmentAsync(
        Guid assignmentId,
        WorkflowAssignmentActionInput input,
        WorkflowRuntimeAction action)
    {
        var initial = await _dbContext.DocumentAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == assignmentId)
            ?? throw new EntityNotFoundException(
                typeof(DocumentAssignment), assignmentId);
        await using var handle = await AcquireDocumentLockAsync(initial.DocumentId);
        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true,
            isTransactional: true);
        var assignment = await _dbContext.DocumentAssignments
            .SingleAsync(x => x.Id == assignmentId);
        var instance = await LoadInstanceAsync(assignment.InstanceId);
        var step = instance.Steps.Single(x => x.Id == assignment.CommittedStepId);
        EnsureOwner(assignment);
        if (IsReplay(assignment, action))
        {
            return Map(instance);
        }
        EnsureActiveAndCurrent(instance, assignment);
        EnsureConcurrency(
            assignment.ConcurrencyStamp,
            input.AssignmentConcurrencyStamp);

        var now = Clock.Now.ToUniversalTime();
        if (action == WorkflowRuntimeAction.RequestSign)
        {
            if (assignment.Action != DocumentAssignmentAction.Sign)
            {
                throw new UserFriendlyException(
                    "Only a SIGN assignment can request signing.");
            }
            if (!await _dbContext.DocumentWorkflowInstanceLogs.AnyAsync(x =>
                    x.AssignmentId == assignment.Id &&
                    x.Action == WorkflowRuntimeAction.RequestSign))
            {
                AppendRecords(
                    instance,
                    assignment,
                    WorkflowRuntimeAction.RequestSign,
                    instance.Status,
                    instance.Status,
                    now,
                    input.Comment);
                await _dbContext.SaveChangesAsync();
                await uow.CompleteAsync();
            }
            return Map(instance);
        }

        if (action == WorkflowRuntimeAction.Approve)
        {
            if (assignment.Action == DocumentAssignmentAction.Sign)
            {
                throw new UserFriendlyException(
                    "SIGN assignments remain pending until verified signing completes.");
            }
            assignment.Complete(now);
            await AdvanceOrCompleteAsync(instance, assignment, now, input.Comment);
        }
        else if (action == WorkflowRuntimeAction.Return)
        {
            if (!step.AllowReturn)
            {
                throw new UserFriendlyException(
                    "The current workflow step does not allow return.");
            }
            assignment.Reject(now);
            await TerminateAsync(
                instance,
                assignment,
                WorkflowRuntimeAction.Return,
                DocumentWorkflowStatus.Returned,
                now,
                input.Comment);
        }
        else if (action == WorkflowRuntimeAction.Reject)
        {
            assignment.Reject(now);
            await TerminateAsync(
                instance,
                assignment,
                WorkflowRuntimeAction.Reject,
                DocumentWorkflowStatus.Rejected,
                now,
                input.Comment);
        }

        await _dbContext.SaveChangesAsync();
        await uow.CompleteAsync();
        return Map(instance);
    }

    private async Task AdvanceOrCompleteAsync(
        DocumentWorkflowInstance instance,
        DocumentAssignment completed,
        DateTime now,
        string? comment)
    {
        AppendRecords(
            instance,
            completed,
            WorkflowRuntimeAction.Approve,
            instance.Status,
            instance.Status,
            now,
            comment);
        if (instance.SignMode == WorkflowSignMode.Parallel)
        {
            var hasPending = await _dbContext.DocumentAssignments.AnyAsync(x =>
                x.InstanceId == instance.Id &&
                x.Id != completed.Id &&
                x.Status == DocumentAssignmentStatus.Pending);
            if (hasPending)
            {
                return;
            }
            await CompleteAsync(instance, now);
            return;
        }

        var completedOrder = instance.Steps
            .Single(x => x.Id == completed.CommittedStepId).Order;
        var next = instance.Steps
            .Where(x =>
                x.Order > completedOrder &&
                x.Type != WorkflowStepType.View)
            .OrderBy(x => x.Order)
            .FirstOrDefault();
        if (next is null)
        {
            await CompleteAsync(instance, now);
            return;
        }
        var receiver = next.Receivers.Single(x => x.IsSelected);
        var assignment = new DocumentAssignment(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            instance.Id,
            instance.DocumentId,
            next.Id,
            receiver.UserId,
            next.Type == WorkflowStepType.Sign
                ? DocumentAssignmentAction.Sign
                : DocumentAssignmentAction.Process,
            now,
            isCurrent: true);
        _dbContext.DocumentAssignments.Add(assignment);
        instance.SetCurrentStep(next.Id);
        _dbContext.DocumentHistories.Add(new DocumentHistory(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            instance.DocumentId,
            instance.Id,
            WorkflowRuntimeAction.AssignUser,
            completed.ReceiverUserId,
            receiver.UserId,
            now));
    }

    private async Task CompleteAsync(
        DocumentWorkflowInstance instance,
        DateTime now)
    {
        var fromStatus = instance.Status;
        instance.Complete(now);
        var document = await _dbContext.Documents
            .SingleAsync(x => x.Id == instance.DocumentId);
        document.SetWorkflowStatus("WORKFLOW_COMPLETED", now);
        AppendRecords(
            instance,
            null,
            WorkflowRuntimeAction.Complete,
            fromStatus,
            DocumentWorkflowStatus.Completed,
            now,
            null);
    }

    private async Task TerminateAsync(
        DocumentWorkflowInstance instance,
        DocumentAssignment source,
        WorkflowRuntimeAction action,
        DocumentWorkflowStatus target,
        DateTime now,
        string? comment)
    {
        var pending = await _dbContext.DocumentAssignments
            .Where(x =>
                x.InstanceId == instance.Id &&
                x.Id != source.Id &&
                x.Status == DocumentAssignmentStatus.Pending)
            .ToListAsync();
        foreach (var assignment in pending)
        {
            assignment.Revoke(now);
        }
        var fromStatus = instance.Status;
        if (target == DocumentWorkflowStatus.Returned)
        {
            instance.Return(now);
        }
        else
        {
            instance.Reject(now);
        }
        await FinishAsync(instance, action, fromStatus, now, comment, source);
    }

    private async Task FinishAsync(
        DocumentWorkflowInstance instance,
        WorkflowRuntimeAction action,
        DocumentWorkflowStatus fromStatus,
        DateTime now,
        string? comment,
        DocumentAssignment? assignment = null)
    {
        var document = await _dbContext.Documents
            .SingleAsync(x => x.Id == instance.DocumentId);
        document.SetWorkflowStatus(
            instance.Status switch
            {
                DocumentWorkflowStatus.Returned => "WORKFLOW_RETURNED",
                DocumentWorkflowStatus.Rejected => "WORKFLOW_REJECTED",
                DocumentWorkflowStatus.Cancelled => "WORKFLOW_CANCELLED",
                _ => throw new UserFriendlyException(
                    "Unsupported terminal workflow status.")
            },
            now);
        AppendRecords(
            instance,
            assignment,
            action,
            fromStatus,
            instance.Status,
            now,
            comment);
        await _dbContext.SaveChangesAsync();
    }

    private void AppendRecords(
        DocumentWorkflowInstance instance,
        DocumentAssignment? assignment,
        WorkflowRuntimeAction action,
        DocumentWorkflowStatus fromStatus,
        DocumentWorkflowStatus toStatus,
        DateTime now,
        string? comment)
    {
        _dbContext.DocumentWorkflowInstanceLogs.Add(
            new DocumentWorkflowInstanceLog(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                instance.Id,
                action,
                CurrentUser.Id,
                fromStatus,
                toStatus,
                now,
                comment,
                assignment?.Id));
        _dbContext.DocumentHistories.Add(new DocumentHistory(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            instance.DocumentId,
            instance.Id,
            action,
            CurrentUser.Id,
            assignment?.ReceiverUserId,
            now,
            comment));
    }

    private async Task<DocumentWorkflowInstance> LoadInstanceAsync(Guid id) =>
        await _dbContext.DocumentWorkflowInstances
            .Include(x => x.Steps)
                .ThenInclude(x => x.Receivers)
            .SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException(
            typeof(DocumentWorkflowInstance), id);

    private void EnsureOwner(DocumentAssignment assignment)
    {
        if (assignment.ReceiverUserId != CurrentUser.Id)
        {
            throw new AbpAuthorizationException(
                "The current user does not own this assignment.");
        }
    }

    private static void EnsureActiveAndCurrent(
        DocumentWorkflowInstance instance,
        DocumentAssignment assignment)
    {
        if (instance.Status is not (DocumentWorkflowStatus.InProgress or
            DocumentWorkflowStatus.Overdue))
        {
            throw new UserFriendlyException(
                "The workflow is no longer active.");
        }
        if (!assignment.IsCurrent ||
            assignment.Status != DocumentAssignmentStatus.Pending)
        {
            throw new UserFriendlyException(
                "The assignment is not currently actionable.");
        }
    }

    private static bool IsReplay(
        DocumentAssignment assignment,
        WorkflowRuntimeAction action) =>
        action == WorkflowRuntimeAction.Approve &&
        assignment.Status == DocumentAssignmentStatus.Done;

    private static void EnsureConcurrency(string actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new AbpDbConcurrencyException();
        }
    }

    private async Task<IAsyncDisposable> AcquireDocumentLockAsync(Guid documentId)
    {
        var tenantKey = CurrentTenant.Id?.ToString("N") ?? "host";
        var handle = await _distributedLock.TryAcquireAsync(
            $"document-workflow-document:{tenantKey}:{documentId:N}",
            TimeSpan.FromSeconds(30));
        return handle ?? throw new UserFriendlyException(
            "The document workflow is busy. Please retry.");
    }

    private static DocumentWorkflowInstanceDto Map(
        DocumentWorkflowInstance instance) =>
        new()
        {
            Id = instance.Id,
            DocumentId = instance.DocumentId,
            SourceFileId = instance.SourceFileId,
            WorkflowId = instance.WorkflowId,
            WorkflowTemplateId = instance.WorkflowTemplateId,
            InitiatorUserId = instance.InitiatorUserId,
            SignMode = instance.SignMode,
            Status = instance.Status,
            CurrentCommittedStepId = instance.CurrentCommittedStepId,
            CurrentSignedFileId = instance.CurrentSignedFileId,
            PreviousInstanceId = instance.PreviousInstanceId,
            StartedAtUtc = instance.StartedAtUtc,
            DeadlineAtUtc = instance.DeadlineAtUtc,
            FinishedAtUtc = instance.FinishedAtUtc,
            OverdueAtUtc = instance.OverdueAtUtc,
            ExtensionCount = instance.ExtensionCount,
            TotalExtensionBusinessDays =
                instance.TotalExtensionBusinessDays,
            ConcurrencyStamp = instance.ConcurrencyStamp
        };
}

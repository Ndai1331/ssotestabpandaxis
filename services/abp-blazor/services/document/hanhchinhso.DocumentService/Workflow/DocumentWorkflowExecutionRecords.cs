using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Workflows;

public class DocumentAssignment : AggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid CommittedStepId { get; private set; }
    public Guid ReceiverUserId { get; private set; }
    public DocumentAssignmentAction Action { get; private set; }
    public DocumentAssignmentStatus Status { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public Guid? DocumentFileResultId { get; private set; }

    protected DocumentAssignment() { }

    public DocumentAssignment(
        Guid id,
        Guid? tenantId,
        Guid instanceId,
        Guid documentId,
        Guid committedStepId,
        Guid receiverUserId,
        DocumentAssignmentAction action,
        DateTime assignedAtUtc,
        bool isCurrent) : base(id)
    {
        TenantId = tenantId;
        InstanceId = instanceId;
        DocumentId = documentId;
        CommittedStepId = committedStepId;
        ReceiverUserId = Check.NotDefaultOrNull<Guid>(
            receiverUserId, nameof(receiverUserId));
        Action = action;
        Status = DocumentAssignmentStatus.Pending;
        AssignedAtUtc = DateTime.SpecifyKind(assignedAtUtc, DateTimeKind.Utc);
        IsCurrent = isCurrent;
    }

    public void MarkCurrent() => IsCurrent = true;

    public void ReplaceReceiver(Guid receiverUserId)
    {
        EnsurePending();
        ReceiverUserId = Check.NotDefaultOrNull<Guid>(
            receiverUserId,
            nameof(receiverUserId));
    }

    public void Complete(DateTime processedAtUtc, Guid? documentFileResultId = null)
    {
        EnsurePending();
        Status = DocumentAssignmentStatus.Done;
        IsCurrent = false;
        ProcessedAtUtc = DateTime.SpecifyKind(processedAtUtc, DateTimeKind.Utc);
        DocumentFileResultId = documentFileResultId;
    }

    public void Reject(DateTime processedAtUtc)
    {
        EnsurePending();
        Status = DocumentAssignmentStatus.Rejected;
        IsCurrent = false;
        ProcessedAtUtc = DateTime.SpecifyKind(processedAtUtc, DateTimeKind.Utc);
    }

    public void Revoke(DateTime processedAtUtc)
    {
        EnsurePending();
        Status = DocumentAssignmentStatus.Revoked;
        IsCurrent = false;
        ProcessedAtUtc = DateTime.SpecifyKind(processedAtUtc, DateTimeKind.Utc);
    }

    private void EnsurePending()
    {
        if (Status != DocumentAssignmentStatus.Pending)
        {
            throw new UserFriendlyException(
                "Only a pending assignment can be changed.");
        }
    }
}

public class DocumentWorkflowInstanceLog : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid? AssignmentId { get; private set; }
    public WorkflowRuntimeAction Action { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public DocumentWorkflowStatus? FromStatus { get; private set; }
    public DocumentWorkflowStatus? ToStatus { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Note { get; private set; }

    protected DocumentWorkflowInstanceLog() { }

    public DocumentWorkflowInstanceLog(
        Guid id,
        Guid? tenantId,
        Guid instanceId,
        WorkflowRuntimeAction action,
        Guid? actorUserId,
        DocumentWorkflowStatus? fromStatus,
        DocumentWorkflowStatus? toStatus,
        DateTime occurredAtUtc,
        string? note = null,
        Guid? assignmentId = null) : base(id)
    {
        TenantId = tenantId;
        InstanceId = instanceId;
        AssignmentId = assignmentId;
        Action = action;
        ActorUserId = actorUserId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        Note = note.IsNullOrWhiteSpace() ? null : note.Trim();
    }
}

public class DocumentHistory : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid InstanceId { get; private set; }
    public WorkflowRuntimeAction Action { get; private set; }
    public Guid? FromUserId { get; private set; }
    public Guid? ToUserId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Comment { get; private set; }

    protected DocumentHistory() { }

    public DocumentHistory(
        Guid id,
        Guid? tenantId,
        Guid documentId,
        Guid instanceId,
        WorkflowRuntimeAction action,
        Guid? fromUserId,
        Guid? toUserId,
        DateTime occurredAtUtc,
        string? comment = null) : base(id)
    {
        TenantId = tenantId;
        DocumentId = documentId;
        InstanceId = instanceId;
        Action = action;
        FromUserId = fromUserId;
        ToUserId = toUserId;
        OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        Comment = comment.IsNullOrWhiteSpace() ? null : comment.Trim();
    }
}

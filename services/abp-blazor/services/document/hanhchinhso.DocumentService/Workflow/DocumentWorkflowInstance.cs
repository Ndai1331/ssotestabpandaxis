using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Workflows;

public class DocumentWorkflowInstance :
    AggregateRoot<Guid>,
    IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid SourceFileId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public Guid WorkflowTemplateId { get; private set; }
    public Guid InitiatorUserId { get; private set; }
    public WorkflowSignMode SignMode { get; private set; }
    public DocumentWorkflowStatus Status { get; private set; }
    public Guid? CurrentCommittedStepId { get; private set; }
    public Guid? CurrentSignedFileId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? DeadlineAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public DateTime? OverdueAtUtc { get; private set; }
    public Guid? PreviousInstanceId { get; private set; }
    public int ExtensionCount { get; private set; }
    public int TotalExtensionBusinessDays { get; private set; }
    public ICollection<DocumentWorkflowCommittedStep> Steps { get; private set; } = [];

    protected DocumentWorkflowInstance() { }

    public DocumentWorkflowInstance(
        Guid id,
        Guid? tenantId,
        Guid documentId,
        Guid sourceFileId,
        Guid workflowId,
        Guid workflowTemplateId,
        Guid initiatorUserId,
        WorkflowSignMode signMode,
        DateTime startedAtUtc,
        Guid? previousInstanceId = null) : base(id)
    {
        TenantId = tenantId;
        DocumentId = Check.NotDefaultOrNull<Guid>(documentId, nameof(documentId));
        SourceFileId = Check.NotDefaultOrNull<Guid>(
            sourceFileId, nameof(sourceFileId));
        WorkflowId = Check.NotDefaultOrNull<Guid>(workflowId, nameof(workflowId));
        WorkflowTemplateId = Check.NotDefaultOrNull<Guid>(
            workflowTemplateId, nameof(workflowTemplateId));
        InitiatorUserId = Check.NotDefaultOrNull<Guid>(
            initiatorUserId, nameof(initiatorUserId));
        if (!Enum.IsDefined(signMode))
        {
            throw new UserFriendlyException("Workflow sign mode must be valid.");
        }
        SignMode = signMode;
        PreviousInstanceId = previousInstanceId;
        Status = DocumentWorkflowStatus.InProgress;
        StartedAtUtc = DateTime.SpecifyKind(startedAtUtc, DateTimeKind.Utc);
    }

    public void AddStep(DocumentWorkflowCommittedStep step) => Steps.Add(step);

    public void SetCurrentStep(Guid? committedStepId)
    {
        CurrentCommittedStepId = committedStepId;
    }

    public void SetCurrentSignedFile(Guid fileId)
    {
        CurrentSignedFileId = Check.NotDefaultOrNull<Guid>(
            fileId, nameof(fileId));
    }

    public void SetDeadline(DateTime? deadlineAtUtc)
    {
        DeadlineAtUtc = deadlineAtUtc.HasValue
            ? DateTime.SpecifyKind(deadlineAtUtc.Value, DateTimeKind.Utc)
            : null;
    }

    public void MarkOverdue(DateTime overdueAtUtc)
    {
        if (Status != DocumentWorkflowStatus.InProgress ||
            !DeadlineAtUtc.HasValue ||
            overdueAtUtc.ToUniversalTime() <= DeadlineAtUtc.Value)
        {
            throw new UserFriendlyException(
                "Only an active workflow past its deadline can be marked overdue.");
        }
        Status = DocumentWorkflowStatus.Overdue;
        OverdueAtUtc = DateTime.SpecifyKind(
            overdueAtUtc,
            DateTimeKind.Utc);
    }

    public void Extend(int businessDays, DateTime deadlineAtUtc)
    {
        if (Status != DocumentWorkflowStatus.Overdue ||
            businessDays is < 1 or > 365)
        {
            throw new UserFriendlyException(
                "Only an overdue workflow can receive a valid extension.");
        }
        DeadlineAtUtc = DateTime.SpecifyKind(deadlineAtUtc, DateTimeKind.Utc);
        ExtensionCount++;
        TotalExtensionBusinessDays += businessDays;
        Status = DocumentWorkflowStatus.InProgress;
        OverdueAtUtc = null;
    }

    public void Complete(DateTime finishedAtUtc) =>
        Finish(DocumentWorkflowStatus.Completed, finishedAtUtc);

    public void Return(DateTime finishedAtUtc) =>
        Finish(DocumentWorkflowStatus.Returned, finishedAtUtc);

    public void Reject(DateTime finishedAtUtc) =>
        Finish(DocumentWorkflowStatus.Rejected, finishedAtUtc);

    public void Cancel(DateTime finishedAtUtc) =>
        Finish(DocumentWorkflowStatus.Cancelled, finishedAtUtc);

    private void Finish(
        DocumentWorkflowStatus status,
        DateTime finishedAtUtc)
    {
        if (Status is not (DocumentWorkflowStatus.InProgress or
            DocumentWorkflowStatus.Overdue))
        {
            throw new UserFriendlyException(
                "Only an active workflow can enter a terminal state.");
        }
        Status = status;
        CurrentCommittedStepId = null;
        FinishedAtUtc = DateTime.SpecifyKind(finishedAtUtc, DateTimeKind.Utc);
        OverdueAtUtc = null;
    }
}

public static class WorkflowBusinessDayCalculator
{
    public static DateTime Add(DateTime startUtc, int businessDays)
    {
        var result = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        var remaining = businessDays;
        while (remaining > 0)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not (
                DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                remaining--;
            }
        }
        return result;
    }
}

public class DocumentWorkflowCommittedStep : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid TemplateStepId { get; private set; }
    public int Order { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WorkflowStepType Type { get; private set; }
    public bool AllowReturn { get; private set; }
    public int? SlaDays { get; private set; }
    public ICollection<DocumentWorkflowCommittedReceiver> Receivers { get; private set; } = [];
    public ICollection<DocumentWorkflowCommittedViewScope> ViewScopes { get; private set; } = [];

    protected DocumentWorkflowCommittedStep() { }

    public DocumentWorkflowCommittedStep(
        Guid id,
        Guid? tenantId,
        Guid instanceId,
        WorkflowStepTemplate template) : base(id)
    {
        TenantId = tenantId;
        InstanceId = instanceId;
        TemplateStepId = template.Id;
        Order = template.Order;
        Name = template.Name;
        Type = template.Type;
        AllowReturn = template.AllowReturn;
        SlaDays = template.SlaDays;
    }

    public void AddReceiver(DocumentWorkflowCommittedReceiver receiver) =>
        Receivers.Add(receiver);

    public void AddViewScope(DocumentWorkflowCommittedViewScope scope) =>
        ViewScopes.Add(scope);
}

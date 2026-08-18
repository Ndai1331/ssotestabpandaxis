namespace HCS.DocumentService.Workflows;

public sealed class WorkflowDefinition
{
    private readonly List<WorkflowStep> _steps = [];
    private WorkflowDefinition() { }
    public WorkflowDefinition(Guid id, string code, string name, IEnumerable<WorkflowStepInput> steps, DateTime now)
    {
        Id = id;
        Code = Required(code, 64);
        Name = Required(name, 256);
        CreationTime = now;
        var normalized = steps.OrderBy(x => x.Order).ToList();
        if (normalized.Count == 0) throw new InvalidOperationException("A workflow requires at least one step.");
        if (normalized.Select(x => x.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
            throw new InvalidOperationException("Workflow step codes must be unique.");
        if (normalized.Select(x => x.Order).Distinct().Count() != normalized.Count)
            throw new InvalidOperationException("Workflow step orders must be unique.");
        _steps.AddRange(normalized.Select(x => new WorkflowStep(Guid.NewGuid(), id,
            Required(x.Code, 64), Required(x.Name, 256), x.Order, Required(x.RequiredPermission, 128))));
    }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime CreationTime { get; private set; }
    public IReadOnlyCollection<WorkflowStep> Steps => _steps;
    public void Rename(string name) => Name = Required(name, 256);
    public void ReplaceSteps(IEnumerable<WorkflowStepInput> steps)
    {
        var normalized = steps.OrderBy(x => x.Order).ToList();
        if (normalized.Count == 0) throw new InvalidOperationException("A workflow requires at least one step.");
        if (normalized.Select(x => x.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
            throw new InvalidOperationException("Workflow step codes must be unique.");
        if (normalized.Select(x => x.Order).Distinct().Count() != normalized.Count)
            throw new InvalidOperationException("Workflow step orders must be unique.");
        _steps.Clear();
        _steps.AddRange(normalized.Select(x => new WorkflowStep(Guid.NewGuid(), Id,
            Required(x.Code, 64), Required(x.Name, 256), x.Order, Required(x.RequiredPermission, 128))));
    }
    private static string Required(string value, int max)
    {
        var result = value?.Trim();
        if (string.IsNullOrWhiteSpace(result) || result.Length > max) throw new ArgumentException("Invalid workflow value.");
        return result;
    }
}

public sealed class WorkflowStep
{
    private WorkflowStep() { }
    internal WorkflowStep(Guid id, Guid definitionId, string code, string name, int order, string requiredPermission)
        => (Id, DefinitionId, Code, Name, Order, RequiredPermission) = (id, definitionId, code, name, order, requiredPermission);
    public Guid Id { get; private set; }
    public Guid DefinitionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public string RequiredPermission { get; private set; } = string.Empty;
}

public sealed class WorkflowTemplate
{
    private WorkflowTemplate() { }
    public WorkflowTemplate(Guid id, string code, string name, Guid definitionId, int version, string templateJson, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 64) throw new ArgumentException("Invalid template code.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 256) throw new ArgumentException("Invalid template name.");
        if (definitionId == Guid.Empty) throw new ArgumentException("Definition is required.");
        if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
        if (string.IsNullOrWhiteSpace(templateJson)) throw new ArgumentException("Template JSON is required.");
        using var _ = System.Text.Json.JsonDocument.Parse(templateJson);
        (Id, Code, Name, DefinitionId, Version, TemplateJson, IsActive, CreationTime) =
            (id, code.Trim(), name.Trim(), definitionId, version, templateJson, true, now);
    }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid DefinitionId { get; private set; }
    public int Version { get; private set; }
    public string TemplateJson { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreationTime { get; private set; }
    public void SetActive(bool isActive) => IsActive = isActive;
}

public sealed class WorkflowInstance
{
    private readonly List<ApprovalTask> _tasks = [];
    private WorkflowInstance() { }
    public WorkflowInstance(Guid id, Guid documentId, WorkflowDefinition definition, string idempotencyKey, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.");
        Id = id;
        DocumentId = documentId;
        DefinitionId = definition.Id;
        IdempotencyKey = idempotencyKey.Trim();
        Status = WorkflowInstanceStatus.Running;
        CurrentStep = 0;
        CreationTime = now;
        AddTask(definition.Steps.OrderBy(x => x.Order).First(), now);
    }
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid DefinitionId { get; private set; }
    public WorkflowInstanceStatus Status { get; private set; }
    public int CurrentStep { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreationTime { get; private set; }
    public IReadOnlyCollection<ApprovalTask> Tasks => _tasks;

    public bool Decide(Guid taskId, bool approve, Guid actorUserId, string? comment, string commandKey,
        IReadOnlyList<WorkflowStep> orderedSteps, DateTime now)
    {
        var task = _tasks.SingleOrDefault(x => x.Id == taskId) ?? throw new KeyNotFoundException("Approval task not found.");
        if (task.DecisionKey == commandKey) return false;
        if (Status != WorkflowInstanceStatus.Running) throw new InvalidOperationException("Workflow is not running.");
        task.Decide(approve, actorUserId, comment, commandKey, now);
        if (!approve)
        {
            Status = WorkflowInstanceStatus.Rejected;
            return true;
        }
        CurrentStep++;
        if (CurrentStep >= orderedSteps.Count)
        {
            Status = WorkflowInstanceStatus.Completed;
            return true;
        }
        AddTask(orderedSteps[CurrentStep], now);
        return true;
    }

    private void AddTask(WorkflowStep step, DateTime now) => _tasks.Add(new ApprovalTask(Guid.NewGuid(), Id, step.Code, now));
}

public sealed class ApprovalTask
{
    private ApprovalTask() { }
    internal ApprovalTask(Guid id, Guid instanceId, string stepCode, DateTime now)
        => (Id, InstanceId, StepCode, Status, CreationTime) = (id, instanceId, stepCode, ApprovalTaskStatus.Pending, now);
    public Guid Id { get; private set; }
    public Guid InstanceId { get; private set; }
    public string StepCode { get; private set; } = string.Empty;
    public ApprovalTaskStatus Status { get; private set; }
    public Guid? DecidedBy { get; private set; }
    public string? Comment { get; private set; }
    public string? DecisionKey { get; private set; }
    public DateTime CreationTime { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    internal void Decide(bool approve, Guid actorUserId, string? comment, string key, DateTime now)
    {
        if (Status != ApprovalTaskStatus.Pending) throw new InvalidOperationException("Approval task was already decided.");
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Idempotency key is required.");
        Status = approve ? ApprovalTaskStatus.Approved : ApprovalTaskStatus.Rejected;
        DecidedBy = actorUserId;
        Comment = comment?.Trim();
        DecisionKey = key.Trim();
        DecidedAt = now;
    }
}

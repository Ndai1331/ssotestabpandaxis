using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Workflows;

public abstract class WorkflowCatalogAggregate : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; protected set; }
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public bool IsActive { get; protected set; }

    protected WorkflowCatalogAggregate() { }

    protected WorkflowCatalogAggregate(
        Guid id,
        Guid? tenantId,
        string code,
        string name,
        bool isActive) : base(id)
    {
        TenantId = tenantId;
        SetCatalogFields(code, name, isActive);
    }

    protected void SetCatalogFields(string code, string name, bool isActive)
    {
        Code = NormalizeCode(code);
        Name = Check.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
    }

    public static string NormalizeCode(string code) =>
        Check.NotNullOrWhiteSpace(
                code, nameof(code), WorkflowCatalogConsts.CodeMaxLength)
            .Trim()
            .ToUpperInvariant();
}

public class WorkflowDefinition : WorkflowCatalogAggregate
{
    public string? Description { get; private set; }

    protected WorkflowDefinition() { }

    public WorkflowDefinition(
        Guid id,
        Guid? tenantId,
        CreateUpdateWorkflowDefinitionDto input) :
        base(id, tenantId, input.Code, input.Name, input.IsActive)
    {
        Description = NormalizeOptional(input.Description);
    }

    public void Update(CreateUpdateWorkflowDefinitionDto input)
    {
        SetCatalogFields(input.Code, input.Name, input.IsActive);
        Description = NormalizeOptional(input.Description);
    }

    internal static string? NormalizeOptional(string? value) =>
        value.IsNullOrWhiteSpace() ? null : value.Trim();
}

public class Workflow : WorkflowCatalogAggregate
{
    public string? Description { get; private set; }
    public Guid WorkflowDefinitionId { get; private set; }

    protected Workflow() { }

    public Workflow(Guid id, Guid? tenantId, CreateUpdateWorkflowDto input) :
        base(id, tenantId, input.Code, input.Name, input.IsActive)
    {
        UpdateDetails(input);
    }

    public void Update(CreateUpdateWorkflowDto input)
    {
        SetCatalogFields(input.Code, input.Name, input.IsActive);
        UpdateDetails(input);
    }

    private void UpdateDetails(CreateUpdateWorkflowDto input)
    {
        WorkflowDefinitionId = input.WorkflowDefinitionId;
        Description = WorkflowDefinition.NormalizeOptional(input.Description);
    }
}

public class WorkflowTemplate : WorkflowCatalogAggregate
{
    public string? WordTemplatePath { get; private set; }
    public string? PdfTemplatePath { get; private set; }
    public string? ContentSchema { get; private set; }
    public WorkflowOutputFormat? OutputFormat { get; private set; }
    public WorkflowSignMode? SignMode { get; private set; }
    public Guid WorkflowId { get; private set; }

    protected WorkflowTemplate() { }

    public WorkflowTemplate(
        Guid id,
        Guid? tenantId,
        CreateUpdateWorkflowTemplateDto input) :
        base(id, tenantId, input.Code, input.Name, input.IsActive)
    {
        UpdateDetails(input);
    }

    public void Update(CreateUpdateWorkflowTemplateDto input)
    {
        SetCatalogFields(input.Code, input.Name, input.IsActive);
        UpdateDetails(input);
    }

    private void UpdateDetails(CreateUpdateWorkflowTemplateDto input)
    {
        if (input.OutputFormat.HasValue && !Enum.IsDefined(input.OutputFormat.Value))
        {
            throw new UserFriendlyException(
                "Workflow template output format must be valid when provided.");
        }
        if (input.SignMode.HasValue && !Enum.IsDefined(input.SignMode.Value))
        {
            throw new UserFriendlyException(
                "Workflow template sign mode must be valid when provided.");
        }
        if (!input.ContentSchema.IsNullOrWhiteSpace())
        {
            try
            {
                using var _ = JsonDocument.Parse(input.ContentSchema);
            }
            catch (JsonException exception)
            {
                throw new UserFriendlyException(
                    "Workflow template content schema must be valid JSON.",
                    innerException: exception);
            }
        }

        WorkflowId = input.WorkflowId;
        WordTemplatePath = WorkflowDefinition.NormalizeOptional(input.WordTemplatePath);
        PdfTemplatePath = WorkflowDefinition.NormalizeOptional(input.PdfTemplatePath);
        ContentSchema = WorkflowDefinition.NormalizeOptional(input.ContentSchema);
        OutputFormat = input.OutputFormat;
        SignMode = input.SignMode;
    }
}

public class WorkflowStepTemplate : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public int Order { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WorkflowStepType Type { get; private set; }
    public int? SlaDays { get; private set; }
    public bool AllowReturn { get; private set; }
    public bool IsActive { get; private set; }
    public Guid WorkflowTemplateId { get; private set; }

    protected WorkflowStepTemplate() { }

    public WorkflowStepTemplate(
        Guid id,
        Guid? tenantId,
        CreateUpdateWorkflowStepTemplateDto input) : base(id)
    {
        TenantId = tenantId;
        Update(input);
    }

    public void Update(CreateUpdateWorkflowStepTemplateDto input)
    {
        if (input.Order is < WorkflowCatalogConsts.StepOrderMin or > WorkflowCatalogConsts.StepOrderMax)
        {
            throw new UserFriendlyException(
                $"Workflow step order must be between {WorkflowCatalogConsts.StepOrderMin} and {WorkflowCatalogConsts.StepOrderMax}.");
        }
        if (!input.Type.HasValue || !Enum.IsDefined(input.Type.Value))
        {
            throw new UserFriendlyException("Workflow step type is required and must be valid.");
        }
        if (input.SlaDays < 0)
        {
            throw new UserFriendlyException("Workflow step SLA days cannot be negative.");
        }

        Order = input.Order;
        Name = Check.NotNullOrWhiteSpace(input.Name, nameof(input.Name)).Trim();
        Type = input.Type.Value;
        SlaDays = input.SlaDays;
        AllowReturn = input.AllowReturn;
        IsActive = input.IsActive;
        WorkflowTemplateId = input.WorkflowTemplateId;
    }
}

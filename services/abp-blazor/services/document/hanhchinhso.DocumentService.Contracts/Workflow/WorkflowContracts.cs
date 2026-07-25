using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.DocumentService.Workflows;

[JsonConverter(typeof(StrictWorkflowStepTypeJsonConverter))]
public enum WorkflowStepType
{
    Process = 0,
    Sign = 1,
    View = 2
}

[JsonConverter(typeof(StrictWorkflowOutputFormatJsonConverter))]
public enum WorkflowOutputFormat
{
    Docx = 0,
    Pdf = 1
}

[JsonConverter(typeof(StrictWorkflowSignModeJsonConverter))]
public enum WorkflowSignMode
{
    Sequential = 0,
    Parallel = 1
}

public sealed class StrictWorkflowStepTypeJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);

public sealed class StrictWorkflowOutputFormatJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);

public sealed class StrictWorkflowSignModeJsonConverter() :
    JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false);

public static class WorkflowCatalogConsts
{
    public const int CodeMaxLength = 50;
    public const int StepOrderMin = 1;
    public const int StepOrderMax = 10_000;
}

public class WorkflowCatalogListInput : PagedAndSortedResultRequestDto
{
    public string? FilterText { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ParentId { get; set; }
}

public abstract class WorkflowCatalogDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public abstract class WorkflowCatalogInput : IHasConcurrencyStamp
{
    [Required, StringLength(WorkflowCatalogConsts.CodeMaxLength, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class WorkflowDefinitionDto : WorkflowCatalogDto
{
    public string? Description { get; set; }
}

public class CreateUpdateWorkflowDefinitionDto : WorkflowCatalogInput
{
    public string? Description { get; set; }
}

public class WorkflowDto : WorkflowCatalogDto
{
    public string? Description { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
}

public class CreateUpdateWorkflowDto : WorkflowCatalogInput
{
    public string? Description { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
}

public class WorkflowTemplateDto : WorkflowCatalogDto
{
    public string? WordTemplatePath { get; set; }
    public string? PdfTemplatePath { get; set; }
    public string? ContentSchema { get; set; }
    public WorkflowOutputFormat? OutputFormat { get; set; }
    public WorkflowSignMode? SignMode { get; set; }
    public Guid WorkflowId { get; set; }
}

public class CreateUpdateWorkflowTemplateDto : WorkflowCatalogInput
{
    public string? WordTemplatePath { get; set; }
    public string? PdfTemplatePath { get; set; }
    public string? ContentSchema { get; set; }
    public WorkflowOutputFormat? OutputFormat { get; set; }
    public WorkflowSignMode? SignMode { get; set; }
    public Guid WorkflowId { get; set; }
}

public class WorkflowStepTemplateDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public int? SlaDays { get; set; }
    public bool AllowReturn { get; set; }
    public bool IsActive { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdateWorkflowStepTemplateDto : IHasConcurrencyStamp
{
    [Range(WorkflowCatalogConsts.StepOrderMin, WorkflowCatalogConsts.StepOrderMax)]
    public int Order { get; set; } = 1;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public WorkflowStepType? Type { get; set; }

    [Range(0, int.MaxValue)]
    public int? SlaDays { get; set; }

    public bool AllowReturn { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid WorkflowTemplateId { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public interface IWorkflowCatalogAppService<TDto, in TInput>
{
    Task<TDto> GetAsync(Guid id);
    Task<PagedResultDto<TDto>> GetListAsync(WorkflowCatalogListInput input);
    Task<TDto> CreateAsync(TInput input);
    Task<TDto> UpdateAsync(Guid id, TInput input);
    Task DeleteAsync(Guid id, string concurrencyStamp);
}

public interface IWorkflowDefinitionAppService :
    IWorkflowCatalogAppService<WorkflowDefinitionDto, CreateUpdateWorkflowDefinitionDto>;

public interface IWorkflowAppService :
    IWorkflowCatalogAppService<WorkflowDto, CreateUpdateWorkflowDto>;

public interface IWorkflowTemplateAppService :
    IWorkflowCatalogAppService<WorkflowTemplateDto, CreateUpdateWorkflowTemplateDto>;

public interface IWorkflowStepTemplateAppService :
    IWorkflowCatalogAppService<WorkflowStepTemplateDto, CreateUpdateWorkflowStepTemplateDto>;

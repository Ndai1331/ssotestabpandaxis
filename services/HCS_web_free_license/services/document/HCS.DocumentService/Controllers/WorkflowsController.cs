using HCS.DocumentService.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.DocumentService.Controllers;

[ApiController, Authorize(Policy = Documents.DocumentPermissions.WorkflowView), Route("api/workflows")]
public sealed class WorkflowsController(IWorkflowAppService workflows) : ControllerBase
{
    [HttpGet("definitions")]
    public Task<IReadOnlyList<WorkflowDefinitionDto>> GetDefinitions(CancellationToken cancellationToken) =>
        workflows.GetDefinitionsAsync(cancellationToken);
    [HttpGet("definitions/{id:guid}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> GetDefinition(Guid id, CancellationToken cancellationToken) =>
        await workflows.GetDefinitionAsync(id, cancellationToken) is { } result ? Ok(result) : NotFound();
    [HttpGet("templates")]
    public Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplates(CancellationToken cancellationToken) =>
        workflows.GetTemplatesAsync(cancellationToken);
    [HttpGet("templates/{id:guid}")]
    public async Task<ActionResult<WorkflowTemplateDto>> GetTemplate(Guid id, CancellationToken cancellationToken) =>
        await workflows.GetTemplateAsync(id, cancellationToken) is { } result ? Ok(result) : NotFound();
    [HttpGet("instances")]
    public Task<IReadOnlyList<WorkflowInstanceDto>> GetInstances([FromQuery] Guid? documentId,
        [FromQuery] WorkflowInstanceStatus? status, CancellationToken cancellationToken) =>
        workflows.GetInstancesAsync(documentId, status, cancellationToken);
    [HttpGet("instances/{id:guid}")]
    public async Task<ActionResult<WorkflowInstanceDto>> GetInstance(Guid id, CancellationToken cancellationToken) =>
        await workflows.GetInstanceAsync(id, cancellationToken) is { } result ? Ok(result) : NotFound();
    [HttpPost("definitions"), Authorize(Policy = Documents.DocumentPermissions.WorkflowManage)]
    public Task<Guid> CreateDefinition(CreateWorkflowDefinitionRequest input, CancellationToken cancellationToken) => workflows.CreateDefinitionAsync(input, cancellationToken);
    [HttpPut("definitions/{id:guid}"), Authorize(Policy = Documents.DocumentPermissions.WorkflowManage)]
    public async Task<IActionResult> UpdateDefinition(Guid id, UpdateWorkflowDefinitionRequest input, CancellationToken cancellationToken)
    {
        await workflows.UpdateDefinitionAsync(id, input, cancellationToken);
        return NoContent();
    }
    [HttpDelete("definitions/{id:guid}"), Authorize(Policy = Documents.DocumentPermissions.WorkflowManage)]
    public async Task<IActionResult> DeleteDefinition(Guid id, CancellationToken cancellationToken)
    {
        await workflows.DeleteDefinitionAsync(id, cancellationToken);
        return NoContent();
    }
    [HttpPost("templates"), Authorize(Policy = Documents.DocumentPermissions.WorkflowManage)]
    public Task<WorkflowTemplateDto> CreateTemplate(CreateWorkflowTemplateRequest input, CancellationToken cancellationToken) => workflows.CreateTemplateAsync(input, cancellationToken);
    [HttpPost("templates/{id:guid}/active"), Authorize(Policy = Documents.DocumentPermissions.WorkflowManage)]
    public Task<WorkflowTemplateDto> SetTemplateActive(Guid id, [FromBody] bool isActive, CancellationToken cancellationToken) =>
        workflows.SetTemplateActiveAsync(id, isActive, cancellationToken);
    [HttpPost("instances"), Authorize(Policy = Documents.DocumentPermissions.WorkflowStart)]
    public Task<WorkflowInstanceDto> Start(StartWorkflowRequest input, CancellationToken cancellationToken) => workflows.StartAsync(input, cancellationToken);
    [HttpPost("tasks/{taskId:guid}/decision"), Authorize(Policy = Documents.DocumentPermissions.WorkflowDecide)]
    public Task<WorkflowInstanceDto> Decide(Guid taskId, DecideApprovalTaskRequest input, CancellationToken cancellationToken) => workflows.DecideAsync(taskId, input, cancellationToken);
}

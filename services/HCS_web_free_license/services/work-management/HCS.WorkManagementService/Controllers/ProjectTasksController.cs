using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.WorkManagementService.Controllers;

[ApiController, Authorize(Policy = WorkPermissions.TasksRead), Route("api/project-tasks")]
public sealed class ProjectTasksController(ProjectTaskAppService service, WorkAssetService assets) : ControllerBase
{
    [HttpGet]
    public Task<PagedWorkDto<ProjectTaskDto>> GetList([FromQuery] GetProjectTaskListInput input, CancellationToken ct = default) =>
        service.GetListAsync(input, ct);

    [HttpGet("by-documents")]
    public Task<List<DocumentTaskSummaryDto>> GetByDocuments([FromQuery] Guid[]? documentIds, CancellationToken ct = default) =>
        service.GetByDocumentsAsync(documentIds, ct);

    [HttpGet("{id:guid}")]
    public Task<ProjectTaskDetailDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpGet("next-code")]
    public Task<NextCodeDto> GetNextCode(Guid projectId, CancellationToken ct) => service.GetNextCodeAsync(projectId, ct);

    [HttpPost, Authorize(Policy = WorkPermissions.Tasks)]
    public Task<ProjectTaskDto> Create(CreateProjectTaskDto input, CancellationToken ct) => service.CreateAsync(input, ct);

    [HttpPut("{id:guid}"), Authorize(Policy = WorkPermissions.Tasks)]
    public Task<ProjectTaskDto> Update(Guid id, UpdateProjectTaskDto input, CancellationToken ct) => service.UpdateAsync(id, input, ct);

    [HttpDelete("{id:guid}"), Authorize(Policy = WorkPermissions.Tasks)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/assignments"), Authorize(Policy = WorkPermissions.Tasks)]
    public Task<TaskAssignmentDto> AddAssignment(Guid id, AddTaskAssignmentDto input, CancellationToken ct) =>
        service.AddAssignmentAsync(id, input, ct);

    [HttpDelete("{id:guid}/assignments/{assignmentId:guid}"), Authorize(Policy = WorkPermissions.Tasks)]
    public async Task<IActionResult> RemoveAssignment(Guid id, Guid assignmentId, CancellationToken ct)
    {
        await service.RemoveAssignmentAsync(id, assignmentId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/documents"), Authorize(Policy = WorkPermissions.Tasks)]
    public Task<TaskDocumentReferenceDto> AddDocument(Guid id, AddTaskDocumentReferenceDto input, CancellationToken ct) =>
        service.AddDocumentAsync(id, input, ct);

    [HttpDelete("{id:guid}/documents/{referenceId:guid}"), Authorize(Policy = WorkPermissions.Tasks)]
    public async Task<IActionResult> RemoveDocument(Guid id, Guid referenceId, CancellationToken ct)
    {
        await service.RemoveDocumentAsync(id, referenceId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/files"), Authorize(Policy = WorkPermissions.Tasks)]
    [RequestSizeLimit(WorkAssetService.MaxFileSize + 1024 * 1024)]
    public async Task<ProjectTaskFileDto> UploadFile(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new BadHttpRequestException("File is required.");
        await using var stream = file.OpenReadStream();
        return await assets.SaveTaskFileAsync(id, stream, file.FileName, file.ContentType, file.Length, ct);
    }

    [HttpGet("{id:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DownloadFile(Guid id, Guid fileId, CancellationToken ct)
    {
        var result = await assets.GetTaskFileAsync(id, fileId, ct);
        return File(result.Stream, result.File.ContentType, result.File.FileName);
    }

    [HttpDelete("{id:guid}/files/{fileId:guid}"), Authorize(Policy = WorkPermissions.Tasks)]
    public async Task<IActionResult> DeleteFile(Guid id, Guid fileId, CancellationToken ct)
    {
        await assets.DeleteTaskFileAsync(id, fileId, ct);
        return NoContent();
    }
}

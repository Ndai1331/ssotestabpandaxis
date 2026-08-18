using HCS.DocumentService.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.DocumentService.Controllers;

[ApiController, Authorize(Policy = DocumentPermissions.View), Route("api/documents")]
public sealed class DocumentsController(IDocumentAppService documents, DocumentFileService files) : ControllerBase
{
    [HttpGet]
    public Task<PagedDocumentsDto> GetList([FromQuery] string? filter, [FromQuery] DocumentStatus? status,
        [FromQuery] bool mine = false, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        documents.GetListAsync(filter, status, mine, skip, take, cancellationToken);
    [HttpPost, Authorize(Policy = DocumentPermissions.Create)]
    public Task<DocumentDto> Create(CreateDocumentRequest input, CancellationToken cancellationToken) => documents.CreateAsync(input, cancellationToken);
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Get(Guid id, CancellationToken cancellationToken) =>
        await documents.GetAsync(id, cancellationToken) is { } result ? Ok(result) : NotFound();
    [HttpPut("{id:guid}"), Authorize(Policy = DocumentPermissions.Update)]
    public Task<DocumentDto> Update(Guid id, UpdateDocumentRequest input, CancellationToken cancellationToken) => documents.UpdateAsync(id, input, cancellationToken);
    [HttpPost("{id:guid}/assignments"), Authorize(Policy = DocumentPermissions.Assign)]
    public Task<DocumentDto> Assign(Guid id, AssignDocumentRequest input, CancellationToken cancellationToken) => documents.AssignAsync(id, input, cancellationToken);
    [HttpPost("{id:guid}/submit"), Authorize(Policy = DocumentPermissions.Update)]
    public Task<DocumentDto> Submit(Guid id, CancellationToken cancellationToken) => documents.SubmitAsync(id, cancellationToken);
    [HttpPost("{id:guid}/files"), Authorize(Policy = DocumentPermissions.ManageFiles)]
    [RequestSizeLimit(DocumentFileService.MaxFileSize)]
    public async Task<DocumentFileDto> Upload(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await files.UploadAsync(id, file.FileName, file.ContentType, stream, file.Length, cancellationToken);
    }
    [HttpGet("{id:guid}/files/{fileId:guid}/content")]
    public async Task<IActionResult> Download(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        var result = await files.OpenAuthorizedAsync(id, fileId, cancellationToken);
        return File(result.Content, result.File.ContentType, result.File.FileName, enableRangeProcessing: true);
    }
    [HttpDelete("{id:guid}/files/{fileId:guid}"), Authorize(Policy = DocumentPermissions.ManageFiles)]
    public async Task<IActionResult> DeleteFile(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        await files.DeleteAsync(id, fileId, cancellationToken);
        return NoContent();
    }
}

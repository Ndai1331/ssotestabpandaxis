using System.Security.Claims;
using HCS.DocumentService.Documents;
using HCS.DocumentService.Storage;
using HCS.IntegrationEvents.Documents;
using HCS.DocumentService.Integration;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Workflows;

public sealed class WorkflowAppService(DocumentServiceDbContext db, IHttpContextAccessor httpContext,
    IBlobContainer<DocumentBlobContainer> blobs) : IWorkflowAppService
{
    public async Task<IReadOnlyList<WorkflowDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var definitions = await db.WorkflowDefinitions.AsNoTracking().Include(x => x.Steps)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return definitions.Select(MapDefinition).ToList();
    }

    public async Task<WorkflowDefinitionDto?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var definition = await db.WorkflowDefinitions.AsNoTracking().Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return definition is null ? null : MapDefinition(definition);
    }

    public async Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var templates = await db.WorkflowTemplates.AsNoTracking().OrderBy(x => x.Name)
            .ThenByDescending(x => x.Version).ToListAsync(cancellationToken);
        return templates.Select(MapTemplate).ToList();
    }

    public async Task<WorkflowTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var template = await db.WorkflowTemplates.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return template is null ? null : MapTemplate(template);
    }

    public async Task<IReadOnlyList<WorkflowInstanceDto>> GetInstancesAsync(Guid? documentId = null,
        WorkflowInstanceStatus? status = null, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.WorkflowView);
        var query = Query().AsNoTracking();
        if (!DocumentAccess.IsElevated(principal))
        {
            query = query.Where(instance => db.Documents.Any(document => document.Id == instance.DocumentId &&
                (document.Assignments.Any(a => a.AssigneeUserId == userId) ||
                 document.History.Any(h => h.Action == "Created" && h.ActorUserId == userId))));
        }
        if (documentId.HasValue) query = query.Where(x => x.DocumentId == documentId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var instances = await query.OrderByDescending(x => x.CreationTime).Take(200).ToListAsync(cancellationToken);
        return instances.Select(Map).ToList();
    }

    public async Task<WorkflowInstanceDto?> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.WorkflowView);
        var instance = await Query().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (instance is null) return null;
        var document = await LoadDocumentAsync(instance.DocumentId, cancellationToken);
        DocumentAccess.EnsureCanView(document, userId, principal);
        return Map(instance);
    }

    public async Task<Guid> CreateDefinitionAsync(CreateWorkflowDefinitionRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        var definition = new WorkflowDefinition(Guid.NewGuid(), input.Code, input.Name, input.Steps, DateTime.UtcNow);
        db.WorkflowDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return definition.Id;
    }

    public async Task UpdateDefinitionAsync(Guid id, UpdateWorkflowDefinitionRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        if (await db.WorkflowInstances.AnyAsync(x => x.DefinitionId == id && x.Status == WorkflowInstanceStatus.Running, cancellationToken))
            throw new InvalidOperationException("A running workflow still uses this definition.");
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        definition.Rename(input.Name);
        definition.ReplaceSteps(input.Steps);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        if (await db.WorkflowInstances.AnyAsync(x => x.DefinitionId == id, cancellationToken))
            throw new InvalidOperationException("Workflow instances still reference this definition.");
        if (await db.WorkflowTemplates.AnyAsync(x => x.DefinitionId == id, cancellationToken))
            throw new InvalidOperationException("Workflow templates still reference this definition.");
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        db.WorkflowDefinitions.Remove(definition);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        if (!await db.WorkflowDefinitions.AnyAsync(x => x.Id == input.DefinitionId, cancellationToken))
            throw new KeyNotFoundException("Workflow definition not found.");
        var template = new WorkflowTemplate(Guid.NewGuid(), input.Code, input.Name, input.DefinitionId,
            input.Version, input.TemplateJson, DateTime.UtcNow);
        db.WorkflowTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return MapTemplate(template);
    }

    public async Task<WorkflowTemplateDto> SetTemplateActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        var template = await db.WorkflowTemplates.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow template not found.");
        template.SetActive(isActive);
        await db.SaveChangesAsync(cancellationToken);
        return MapTemplate(template);
    }

    public async Task<WorkflowTemplateDto> UploadTemplateFileAsync(Guid id, string kind, string fileName, string contentType,
        Stream content, long size, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        var template = await db.WorkflowTemplates.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow template not found.");
        var normalizedKind = kind?.Trim().ToLowerInvariant();
        if (normalizedKind is not ("pdf" or "word")) throw new InvalidOperationException("Template file kind must be pdf or word.");
        if (size is <= 0 or > DocumentFileService.MaxFileSize) throw new InvalidOperationException("File size is outside the allowed range.");
        var allowed = normalizedKind == "pdf"
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        var extension = normalizedKind == "pdf" ? ".pdf" : ".docx";
        var typeOk = string.Equals(contentType, allowed, StringComparison.OrdinalIgnoreCase);
        var nameOk = fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        if (!typeOk || !nameOk) throw new InvalidOperationException("File type is not allowed.");
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) throw new InvalidOperationException("File name is not allowed.");
        await using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        var fileId = Guid.NewGuid();
        var blobName = BlobNamePolicy.WorkflowTemplate(id, fileId);
        copy.Position = 0;
        await blobs.SaveAsync(blobName, copy, overrideExisting: true, cancellationToken: cancellationToken);
        if (normalizedKind == "pdf") template.AttachPdf(fileId, safeName, allowed, blobName);
        else template.AttachWord(fileId, safeName, allowed, blobName);
        await db.SaveChangesAsync(cancellationToken);
        return MapTemplate(template);
    }

    public async Task<(string FileName, string ContentType, Stream Content)> OpenTemplateFileAsync(Guid id, string kind,
        CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var template = await db.WorkflowTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow template not found.");
        var normalizedKind = kind?.Trim().ToLowerInvariant();
        string? blobName;
        string? fileName;
        string? contentType;
        if (normalizedKind == "pdf")
        {
            blobName = template.PdfBlobName; fileName = template.PdfFileName; contentType = template.PdfContentType;
        }
        else if (normalizedKind == "word")
        {
            blobName = template.WordBlobName; fileName = template.WordFileName; contentType = template.WordContentType;
        }
        else throw new InvalidOperationException("Template file kind must be pdf or word.");
        if (string.IsNullOrWhiteSpace(blobName) || string.IsNullOrWhiteSpace(fileName))
            throw new KeyNotFoundException("Template file not found.");
        return (fileName, contentType ?? "application/octet-stream", await blobs.GetAsync(blobName, cancellationToken: cancellationToken));
    }

    public async Task<WorkflowInstanceDto> StartAsync(StartWorkflowRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.WorkflowStart);
        var document = await LoadDocumentAsync(input.DocumentId, cancellationToken);
        DocumentAccess.EnsureCanManage(document, userId, principal);
        var existing = await Query().SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey &&
            x.DocumentId == input.DocumentId, cancellationToken);
        if (existing is not null) return Map(existing);
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps).SingleOrDefaultAsync(x => x.Id == input.DefinitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        if (document.Status == DocumentStatus.Draft) document.Submit(userId, DateTime.UtcNow);
        document.StartReview(userId, DateTime.UtcNow);
        var instance = new WorkflowInstance(Guid.NewGuid(), input.DocumentId, definition, input.IdempotencyKey, DateTime.UtcNow);
        db.WorkflowInstances.Add(instance);
        AddChangeEvent(instance, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Map(instance);
    }

    public async Task<WorkflowInstanceDto> DecideAsync(Guid taskId, DecideApprovalTaskRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var actor = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.WorkflowDecide);
        var instance = await Query().SingleOrDefaultAsync(x => x.Tasks.Any(t => t.Id == taskId), cancellationToken)
            ?? throw new KeyNotFoundException("Workflow task not found.");
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps).SingleAsync(x => x.Id == instance.DefinitionId, cancellationToken);
        var documentForAccess = await LoadDocumentAsync(instance.DocumentId, cancellationToken);
        DocumentAccess.EnsureCanView(documentForAccess, actor, principal);
        var task = instance.Tasks.Single(x => x.Id == taskId);
        var step = definition.Steps.SingleOrDefault(x => x.Code == task.StepCode)
            ?? throw new InvalidOperationException("Workflow step configuration is missing.");
        DocumentAccess.RequirePermission(principal, step.RequiredPermission);
        if (step.AssigneeUserId is { } assignee && assignee != actor && !DocumentAccess.IsElevated(principal))
            throw new UnauthorizedAccessException("Only the assigned user can decide this step.");
        var changed = instance.Decide(taskId, input.Approve, actor, input.Comment, input.IdempotencyKey,
            definition.Steps.OrderBy(x => x.Order).ToList(), DateTime.UtcNow);
        if (changed)
        {
            if (instance.Status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Rejected)
            {
                documentForAccess.CompleteReview(instance.Status == WorkflowInstanceStatus.Completed, actor, input.Comment, DateTime.UtcNow);
            }
            AddChangeEvent(instance, DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }
        return Map(instance);
    }

    private IQueryable<WorkflowInstance> Query() => db.WorkflowInstances.Include(x => x.Tasks);
    private ClaimsPrincipal Principal => httpContext.HttpContext?.User ?? new ClaimsPrincipal();
    private void Require(string permission)
    {
        DocumentAccess.RequireUser(Principal);
        DocumentAccess.RequirePermission(Principal, permission);
    }
    private async Task<DocumentAggregate> LoadDocumentAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Documents.Include(x => x.Files).Include(x => x.Assignments).Include(x => x.History)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new KeyNotFoundException("Document not found.");
    private string CorrelationId => httpContext.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
    private void AddChangeEvent(WorkflowInstance instance, DateTime now)
    {
        var integrationEvent = new DocumentWorkflowChangedEto(Guid.NewGuid(), new DateTimeOffset(now, TimeSpan.Zero), CorrelationId,
            instance.DocumentId, instance.Id, instance.Status.ToString());
        db.OutboxMessages.Add(OutboxFactory.CreateCanonical(integrationEvent, CorrelationId, now));
    }
    internal static WorkflowInstanceDto Map(WorkflowInstance x) => new(x.Id, x.DocumentId, x.DefinitionId, x.Status, x.CurrentStep,
        x.Tasks.OrderBy(t => t.CreationTime).Select(t => new ApprovalTaskDto(t.Id, t.InstanceId, t.StepCode, t.Status, t.DecidedBy, t.DecidedAt, t.AssigneeUserId)).ToList(), x.CreationTime);
    internal static WorkflowDefinitionDto MapDefinition(WorkflowDefinition x) => new(x.Id, x.Code, x.Name,
        x.Steps.OrderBy(step => step.Order).Select(step => new WorkflowStepDto(step.Id, step.Code, step.Name,
            step.Order, step.RequiredPermission, step.Type, step.AssigneeUserId)).ToList(), x.CreationTime);
    private static WorkflowTemplateDto MapTemplate(WorkflowTemplate x) => new(x.Id, x.Code, x.Name,
        x.DefinitionId, x.Version, x.IsActive, x.CreationTime, x.WordFileId, x.WordFileName, x.PdfFileId, x.PdfFileName);
}

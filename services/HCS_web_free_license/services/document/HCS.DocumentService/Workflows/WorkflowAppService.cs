using System.Security.Claims;
using HCS.DocumentService.Conversion;
using HCS.DocumentService.Documents;
using HCS.DocumentService.Storage;
using HCS.IntegrationEvents.Documents;
using HCS.DocumentService.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Workflows;

public sealed class WorkflowAppService(DocumentServiceDbContext db, IHttpContextAccessor httpContext,
    IBlobContainer<DocumentBlobContainer> blobs, DocumentFileService files, IDocxToPdfConverter converter,
    IWorkflowAssigneeResolver assigneeResolver, ILogger<WorkflowAppService> logger) : IWorkflowAppService
{
    public async Task<IReadOnlyList<WorkflowKindDto>> GetKindsAsync(CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var kinds = await db.WorkflowKinds.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return kinds.Select(MapKind).ToList();
    }

    public async Task<WorkflowKindDto?> GetKindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var kind = await db.WorkflowKinds.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return kind is null ? null : MapKind(kind);
    }

    public async Task<Guid> CreateKindAsync(CreateWorkflowKindRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("Code and name are required.");
        var kind = new WorkflowKind(Guid.NewGuid(), input.Code, input.Name, input.Description, input.IsActive, DateTime.UtcNow);
        db.WorkflowKinds.Add(kind);
        await db.SaveChangesAsync(cancellationToken);
        return kind.Id;
    }

    public async Task UpdateKindAsync(Guid id, UpdateWorkflowKindRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        var kind = await db.WorkflowKinds.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow kind not found.");
        kind.Update(input.Name, input.Description, input.IsActive);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteKindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        if (await db.WorkflowDefinitions.AnyAsync(x => x.KindId == id, cancellationToken))
            throw new InvalidOperationException("Workflow definitions still reference this type.");
        var kind = await db.WorkflowKinds.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow kind not found.");
        db.WorkflowKinds.Remove(kind);
        await db.SaveChangesAsync(cancellationToken);
    }

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
            query = query.Where(instance =>
                instance.Tasks.Any(task => task.AssigneeUserId == userId) ||
                db.Documents.Any(document => document.Id == instance.DocumentId &&
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
        var definition = new WorkflowDefinition(Guid.NewGuid(), input.Code, input.Name, input.Steps, DateTime.UtcNow,
            input.KindId, input.Description, input.IsActive, input.SignMode);
        db.WorkflowDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return definition.Id;
    }

    public async Task UpdateDefinitionAsync(Guid id, UpdateWorkflowDefinitionRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        // Do not Include(Steps) here: WorkflowDefinitionStepReplacer owns the step lifecycle
        // and needs the definition loaded without tracked children.
        var definition = await db.WorkflowDefinitions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        definition.Rename(input.Name);
        definition.SetMetadata(input.KindId, input.Description, input.IsActive, input.SignMode);
        await WorkflowDefinitionStepReplacer.ReplaceAsync(db, definition, input.Steps, cancellationToken);
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
        template.UpdateContent(input.Name, input.TemplateJson, input.OutputFormat);
        db.WorkflowTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return MapTemplate(template);
    }

    public async Task<WorkflowTemplateDto> UpdateTemplateAsync(Guid id, UpdateWorkflowTemplateRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        var template = await db.WorkflowTemplates.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow template not found.");
        template.UpdateContent(input.Name, input.TemplateJson, input.OutputFormat);
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
        var bytes = copy.ToArray();
        var fileId = Guid.NewGuid();
        var blobName = BlobNamePolicy.WorkflowTemplate(id, fileId);
        copy.Position = 0;
        await blobs.SaveAsync(blobName, copy, overrideExisting: true, cancellationToken: cancellationToken);
        if (normalizedKind == "pdf") template.AttachPdf(fileId, safeName, allowed, blobName);
        else
        {
            template.AttachWord(fileId, safeName, allowed, blobName);
            await TryConvertTemplateWordAsync(template, bytes, cancellationToken);
        }
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
        if (!WorkflowStartRequestRules.HasExactlyOneSource(input))
            throw new InvalidOperationException("Exactly one workflow source document or workflow template file is required.");
        if (input.UseWorkflowTemplateFile && input.UseTemplateFile)
            throw new InvalidOperationException("A workflow template file cannot be combined with a source document template file.");

        DocumentAggregate? source = null;
        if (!input.UseWorkflowTemplateFile)
        {
            source = await LoadDocumentAsync(input.DocumentId!.Value, cancellationToken);
            DocumentAccess.EnsureCanManage(source, userId, principal);
        }

        var existing = await Query().SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey, cancellationToken);
        if (existing is not null) return Map(existing);
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps).SingleOrDefaultAsync(x => x.Id == input.DefinitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        definition.EnsureStartable();
        var now = DateTime.UtcNow;
        DocumentAggregate document;
        if (input.UseWorkflowTemplateFile)
        {
            document = await CreateWorkflowDocumentFromTemplateAsync(definition, userId, now, cancellationToken);
            db.Documents.Add(document);
        }
        else if (source!.SourceType == DocumentSourceType.Workflow)
        {
            document = source;
        }
        else
        {
            var number = await NextWorkflowNumberAsync(source.Number, cancellationToken);
            document = source.DuplicateAsWorkflow(Guid.NewGuid(), number, userId, now);
            db.Documents.Add(document);
            var template = input.UseTemplateFile
                ? await db.WorkflowTemplates.AsNoTracking()
                    .Where(x => x.DefinitionId == definition.Id && x.IsActive && !string.IsNullOrWhiteSpace(x.PdfBlobName))
                    .OrderByDescending(x => x.CreationTime)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;
            if (template?.PdfBlobName is { } blob)
            {
                await using var content = await blobs.GetAsync(blob, cancellationToken: cancellationToken);
                await files.AttachBlobAsync(document, template.PdfFileName ?? "template.pdf",
                    template.PdfContentType ?? "application/pdf", content, userId, now, cancellationToken);
            }
            else
            {
                await files.CopyFilesAsync(source, document, userId, now, cancellationToken);
            }
        }
        if (document.Status == DocumentStatus.Draft) document.Submit(userId, now);
        document.StartReview(userId, now);
        var overrides = (input.Signers ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.StepCode))
            .GroupBy(x => x.StepCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().UserId, StringComparer.OrdinalIgnoreCase);
        await ApplyRoleAssigneesAsync(definition, userId, overrides, cancellationToken);
        var viewScopesJson = input.ViewScopes is { Count: > 0 } ? System.Text.Json.JsonSerializer.Serialize(input.ViewScopes) : null;
        var instance = new WorkflowInstance(Guid.NewGuid(), document.Id, definition, input.IdempotencyKey, now,
            overrides, viewScopesJson);
        db.WorkflowInstances.Add(instance);
        GrantWorkflowAccess(document, instance, input.ViewScopes, userId, now);
        AddChangeEvent(instance, now);
        await db.SaveChangesAsync(cancellationToken);
        return Map(instance);
    }

    public async Task<IReadOnlyList<WorkflowStepCandidateGroupDto>> GetAssigneeCandidatesAsync(Guid definitionId,
        CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.WorkflowStart);
        var definition = await db.WorkflowDefinitions.AsNoTracking().Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == definitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        var groups = new List<WorkflowStepCandidateGroupDto>();
        foreach (var step in definition.Steps.OrderBy(x => x.Order))
        {
            if (step.Type == WorkflowStepTypes.View)
            {
                groups.Add(new WorkflowStepCandidateGroupDto(step.Code, step.Name, step.AssigneeType, step.RoleId, []));
                continue;
            }

            if (step.AssigneeType == WorkflowStepAssigneeTypes.RoleInSubmitterOu && step.RoleId is { } roleId)
            {
                var candidates = await assigneeResolver.ResolveByRoleAsync(roleId, userId, cancellationToken);
                groups.Add(new WorkflowStepCandidateGroupDto(step.Code, step.Name, step.AssigneeType, roleId, candidates));
                continue;
            }

            var preset = step.AssigneeUserId is { } assignee
                ? new[] { new WorkflowAssigneeCandidateDto(assignee, string.Empty) }
                : Array.Empty<WorkflowAssigneeCandidateDto>();
            groups.Add(new WorkflowStepCandidateGroupDto(step.Code, step.Name, step.AssigneeType, step.RoleId, preset));
        }

        return groups;
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
        if (task.AssigneeUserId is { } assignee && assignee != actor && !DocumentAccess.IsElevated(principal))
            throw new UnauthorizedAccessException("Only the assigned user can decide this step.");
        var changed = instance.Decide(taskId, input.Approve, actor, input.Comment, input.IdempotencyKey,
            definition.Steps.OrderBy(x => x.Order).ToList(), DateTime.UtcNow, input.Return);
        if (changed)
        {
            if (instance.Status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Rejected)
            {
                documentForAccess.CompleteReview(instance.Status == WorkflowInstanceStatus.Completed, actor, input.Comment, DateTime.UtcNow);
            }
            else
            {
                GrantWorkflowAccess(documentForAccess, instance, null, actor, DateTime.UtcNow);
            }
            AddChangeEvent(instance, DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }
        return Map(instance);
    }

    public async Task<WorkflowInstanceDto> ResubmitAsync(Guid instanceId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var actor = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.WorkflowStart);
        var instance = await Query().SingleOrDefaultAsync(x => x.Id == instanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow instance not found.");
        var document = await LoadDocumentAsync(instance.DocumentId, cancellationToken);
        DocumentAccess.EnsureCanManage(document, actor, principal);
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps).SingleAsync(x => x.Id == instance.DefinitionId, cancellationToken);
        instance.Resubmit(definition.Steps.OrderBy(x => x.Order).ToList(), DateTime.UtcNow, idempotencyKey);
        if (document.Status != DocumentStatus.InReview) document.StartReview(actor, DateTime.UtcNow);
        GrantWorkflowAccess(document, instance, null, actor, DateTime.UtcNow);
        AddChangeEvent(instance, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Map(instance);
    }

    private static void GrantWorkflowAccess(DocumentAggregate document, WorkflowInstance instance,
        IReadOnlyList<WorkflowViewScopeSelection>? viewScopes, Guid? actor, DateTime now)
    {
        foreach (var task in instance.Tasks.Where(t => t.AssigneeUserId is not null))
            document.Assign(Guid.NewGuid(), task.AssigneeUserId!.Value, task.StepCode, actor, now, task.StepCode);
        foreach (var scope in viewScopes ?? [])
        {
            foreach (var user in scope.UserIds)
                document.Assign(Guid.NewGuid(), user, "VIEW", actor, now, scope.StepCode);
        }
    }
    private IQueryable<WorkflowInstance> Query() => db.WorkflowInstances.Include(x => x.Tasks);
    private ClaimsPrincipal Principal => httpContext.HttpContext?.User ?? new ClaimsPrincipal();
    private void Require(string permission)
    {
        DocumentAccess.RequireUser(Principal);
        DocumentAccess.RequirePermission(Principal, permission);
    }
    private async Task<string> NextWorkflowNumberAsync(string sourceNumber, CancellationToken cancellationToken)
    {
        var prefix = sourceNumber.Length > 60 ? sourceNumber[..60] : sourceNumber;
        var candidate = $"{prefix}-WF";
        var i = 1;
        while (await db.Documents.AnyAsync(x => x.Number == candidate, cancellationToken))
        {
            candidate = $"{prefix}-WF{i++}";
            if (candidate.Length > 64) candidate = $"{prefix[..Math.Max(1, 64 - 6)]}-WF{i}";
        }
        return candidate;
    }

    private async Task<DocumentAggregate> CreateWorkflowDocumentFromTemplateAsync(
        WorkflowDefinition definition, Guid actorUserId, DateTime now, CancellationToken cancellationToken)
    {
        var template = await db.WorkflowTemplates.AsNoTracking()
            .Where(x => x.DefinitionId == definition.Id && x.IsActive &&
                        (!string.IsNullOrWhiteSpace(x.PdfBlobName) || !string.IsNullOrWhiteSpace(x.WordBlobName)))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The workflow has no active template file.");

        var blobName = !string.IsNullOrWhiteSpace(template.PdfBlobName)
            ? template.PdfBlobName!
            : template.WordBlobName!;
        var fileName = !string.IsNullOrWhiteSpace(template.PdfFileName)
            ? template.PdfFileName!
            : template.WordFileName ?? "workflow-template";
        var contentType = !string.IsNullOrWhiteSpace(template.PdfContentType)
            ? template.PdfContentType!
            : template.WordContentType ?? "application/octet-stream";
        var number = await NextWorkflowNumberAsync(template.Code, cancellationToken);
        var document = new DocumentAggregate(Guid.NewGuid(), number, template.Name, null, actorUserId, now,
            DocumentSourceType.Workflow);

        await using var content = await blobs.GetAsync(blobName, cancellationToken: cancellationToken);
        await files.AttachBlobAsync(document, fileName, contentType, content, actorUserId, now, cancellationToken);
        return document;
    }

    private async Task TryConvertTemplateWordAsync(WorkflowTemplate template, byte[] docxBytes, CancellationToken cancellationToken)
    {
        if (!converter.IsAvailable)
        {
            logger.LogInformation("Skipping template Word-to-PDF conversion because LibreOffice is not available.");
            return;
        }
        var pdfBytes = await converter.ConvertAsync(docxBytes, cancellationToken);
        if (pdfBytes is null or { Length: 0 })
        {
            logger.LogWarning("Template Word-to-PDF conversion produced no output for {Template}.", template.Id);
            return;
        }
        var pdfId = Guid.NewGuid();
        var pdfName = Path.ChangeExtension(template.WordFileName ?? "template.docx", ".pdf");
        var blobName = BlobNamePolicy.WorkflowTemplate(template.Id, pdfId);
        await using var stream = new MemoryStream(pdfBytes);
        await blobs.SaveAsync(blobName, stream, overrideExisting: true, cancellationToken: cancellationToken);
        template.AttachPdf(pdfId, pdfName, "application/pdf", blobName);
    }

    private async Task ApplyRoleAssigneesAsync(WorkflowDefinition definition, Guid submitterUserId,
        Dictionary<string, Guid> overrides, CancellationToken cancellationToken)
    {
        foreach (var step in definition.Steps.Where(x => x.IsBlocking))
        {
            if (step.AssigneeType != WorkflowStepAssigneeTypes.RoleInSubmitterOu || step.RoleId is not { } roleId)
                continue;
            var candidates = await assigneeResolver.ResolveByRoleAsync(roleId, submitterUserId, cancellationToken);
            if (candidates.Count == 0)
                throw new InvalidOperationException($"No assignee candidates for step '{step.Code}'.");
            var allowed = candidates.Select(x => x.UserId).ToHashSet();
            if (overrides.TryGetValue(step.Code, out var chosen))
            {
                if (!allowed.Contains(chosen))
                    throw new InvalidOperationException($"Chosen signer is not in the submitter OU role for step '{step.Code}'.");
                continue;
            }
            if (candidates.Count == 1)
                overrides[step.Code] = candidates[0].UserId;
            else
                throw new InvalidOperationException($"Choose a signer for step '{step.Code}'.");
        }
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
        x.Tasks.OrderBy(t => t.CreationTime).Select(t => new ApprovalTaskDto(t.Id, t.InstanceId, t.StepCode, t.Status, t.DecidedBy, t.DecidedAt, t.AssigneeUserId, t.DueAt)).ToList(), x.CreationTime);
    internal static WorkflowDefinitionDto MapDefinition(WorkflowDefinition x) => new(x.Id, x.Code, x.Name, x.KindId, x.Description, x.IsActive,
        x.Steps.OrderBy(step => step.Order).Select(step => new WorkflowStepDto(step.Id, step.Code, step.Name,
            step.Order, step.RequiredPermission, step.Type, step.AssigneeUserId, step.AssigneeType, step.RoleId,
            step.UserIds, step.DepartmentIds, step.SlaDays, step.AllowReturn)).ToList(), x.CreationTime, x.SignMode);
    private static WorkflowKindDto MapKind(WorkflowKind x) => new(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.CreationTime);
    private static WorkflowTemplateDto MapTemplate(WorkflowTemplate x) => new(x.Id, x.Code, x.Name,
        x.DefinitionId, x.Version, x.IsActive, x.CreationTime, x.WordFileId, x.WordFileName, x.PdfFileId, x.PdfFileName,
        x.TemplateJson, x.OutputFormat);
}

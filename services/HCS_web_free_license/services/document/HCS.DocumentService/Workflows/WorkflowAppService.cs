using System.Security.Claims;
using HCS.DocumentService.Conversion;
using HCS.DocumentService.Documents;
using HCS.DocumentService.Storage;
using HCS.IntegrationEvents.Documents;
using HCS.DocumentService.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Workflows;

public sealed class WorkflowAppService(DocumentServiceDbContext db, IHttpContextAccessor httpContext,
    IBlobContainer<DocumentBlobContainer> blobs, DocumentFileService files, IDocxToPdfConverter converter,
    IWorkflowAssigneeResolver assigneeResolver, WorkflowSubmissionPreparationService submissionPreparation,
    ILogger<WorkflowAppService> logger) : IWorkflowAppService
{
    public async Task<IReadOnlyList<WorkflowKindDto>> GetKindsAsync(CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var kinds = await db.WorkflowKinds.AsNoTracking()
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return kinds.Select(MapKind).ToList();
    }

    public async Task<WorkflowKindDto?> GetKindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var kind = await db.WorkflowKinds.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
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
        if (await db.WorkflowDefinitions.WhereVisibleWorkflowDefinitions()
                .AnyAsync(x => x.KindId == id, cancellationToken))
            throw new InvalidOperationException("Workflow definitions still reference this type.");
        var kind = await db.WorkflowKinds.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow kind not found.");
        db.WorkflowKinds.Remove(kind);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var definitions = await db.WorkflowDefinitions.AsNoTracking()
            .WhereVisibleWorkflowDefinitions()
            .Include(x => x.Steps)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return definitions.Select(MapDefinition).ToList();
    }

    public async Task<WorkflowDefinitionDto?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var definition = await db.WorkflowDefinitions.AsNoTracking()
            .WhereVisibleWorkflowDefinitions()
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return definition is null ? null : MapDefinition(definition);
    }

    public async Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var templates = await db.WorkflowTemplates.AsNoTracking()
            .WhereVisibleWorkflowTemplates(db.WorkflowDefinitions)
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Version).ToListAsync(cancellationToken);
        return templates.Select(MapTemplate).ToList();
    }

    public async Task<WorkflowTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowView);
        var template = await db.WorkflowTemplates.AsNoTracking()
            .WhereVisibleWorkflowTemplates(db.WorkflowDefinitions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return template is null ? null : MapTemplate(template);
    }

    public async Task<IReadOnlyList<WorkflowInstanceDto>> GetInstancesAsync(Guid? documentId = null,
        WorkflowInstanceStatus? status = null, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
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
        var definition = await db.WorkflowDefinitions.WhereVisibleWorkflowDefinitions()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        definition.Rename(input.Name);
        definition.SetMetadata(input.KindId, input.Description, input.IsActive, input.SignMode);
        await WorkflowDefinitionStepReplacer.ReplaceAsync(db, definition, input.Steps, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        var definition = await db.WorkflowDefinitions.WhereVisibleWorkflowDefinitions()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        definition.MarkDeleted();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest input, CancellationToken cancellationToken = default)
    {
        Require(DocumentPermissions.WorkflowManage);
        if (!await db.WorkflowDefinitions.WhereActiveWorkflowDefinitions()
                .AnyAsync(x => x.Id == input.DefinitionId, cancellationToken))
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
        var template = await db.WorkflowTemplates.AsNoTracking()
            .WhereVisibleWorkflowTemplates(db.WorkflowDefinitions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
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
            throw new BusinessException("Document:WorkflowSourceRequired");
        if (input.UseWorkflowTemplateFile && input.UseTemplateFile)
            throw new BusinessException("Document:WorkflowTemplateConflict");

        DocumentAggregate? source = null;
        if (!input.UseWorkflowTemplateFile)
        {
            source = await LoadDocumentAsync(input.DocumentId!.Value, cancellationToken);
            DocumentAccess.EnsureCanManage(source, userId, principal);
        }

        var existing = await Query().SingleOrDefaultAsync(x => x.IdempotencyKey == input.IdempotencyKey, cancellationToken);
        if (existing is not null) return Map(existing);
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps)
            .WhereActiveWorkflowDefinitions()
            .SingleOrDefaultAsync(x => x.Id == input.DefinitionId, cancellationToken)
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
            var number = NextWorkflowNumber(source.Number);
            document = source.DuplicateAsWorkflow(Guid.NewGuid(), number, userId, now);
            db.Documents.Add(document);
            var template = input.UseTemplateFile
                ? await db.WorkflowTemplates.AsNoTracking()
                    .Where(x => x.DefinitionId == definition.Id && x.IsActive &&
                        (!string.IsNullOrWhiteSpace(x.PdfBlobName) || !string.IsNullOrWhiteSpace(x.WordBlobName)))
                    .OrderByDescending(x => x.CreationTime)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;
            if (template is not null)
            {
                var useWord = !string.IsNullOrWhiteSpace(template.WordBlobName);
                var blob = useWord ? template.WordBlobName! : template.PdfBlobName!;
                await using var content = await blobs.GetAsync(blob, cancellationToken: cancellationToken);
                await files.AttachBlobAsync(document,
                    useWord ? template.WordFileName ?? "template.docx" : template.PdfFileName ?? "template.pdf",
                    useWord
                        ? template.WordContentType ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        : template.PdfContentType ?? "application/pdf",
                    content, userId, now, cancellationToken);
            }
            else
            {
                await files.CopyFilesAsync(source, document, userId, now, cancellationToken);
            }
        }
        if (document.SourceType == DocumentSourceType.Workflow && document.FromUserId is null)
        {
            var submitterUserId = document.FromUserId
                ?? document.History.FirstOrDefault(x => x.Action == "Created")?.ActorUserId
                ?? userId;
            document.SetWorkflowSubmitter(submitterUserId);
        }
        await submissionPreparation.PrepareAsync(document, userId, definition, input.SigningContent, cancellationToken);
        var workflowPdf = document.Files.Where(x => !x.IsPendingDeletion && WorkflowFileSelection.IsPdf(x))
            .OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id).FirstOrDefault();
        if (workflowPdf is not null) document.SetWorkflowFile(workflowPdf.Id);
        if (document.Status == DocumentStatus.Draft) document.Submit(userId, now);
        document.StartReview(userId, now, input.SigningContent);
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
        EnqueueWorkflowTaskAssigned(document, instance, userId, now);
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
            .WhereActiveWorkflowDefinitions()
            .SingleOrDefaultAsync(x => x.Id == definitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");
        var orderedSteps = definition.Steps.OrderBy(x => x.Order).ToList();
        var roleIds = orderedSteps.Where(x => x.AssigneeType == WorkflowStepAssigneeTypes.RoleInSubmitterOu && x.RoleId.HasValue)
            .Select(x => x.RoleId!.Value).Distinct().ToArray();
        var userIds = orderedSteps.Where(x => x.AssigneeUserId.HasValue).Select(x => x.AssigneeUserId!.Value).Distinct().ToArray();
        var candidatesByRole = await assigneeResolver.ResolveByRolesAsync(roleIds, userId, cancellationToken);
        var candidatesByUser = await assigneeResolver.ResolveByUsersAsync(userIds, cancellationToken);
        var groups = new List<WorkflowStepCandidateGroupDto>();
        foreach (var step in orderedSteps)
        {
            if (step.Type == WorkflowStepTypes.View)
            {
                groups.Add(new WorkflowStepCandidateGroupDto(step.Code, step.Name, step.AssigneeType, step.RoleId, []));
                continue;
            }

            if (step.AssigneeType == WorkflowStepAssigneeTypes.RoleInSubmitterOu && step.RoleId is { } roleId)
            {
                var candidates = candidatesByRole.GetValueOrDefault(roleId) ?? [];
                groups.Add(new WorkflowStepCandidateGroupDto(step.Code, step.Name, step.AssigneeType, roleId, candidates));
                continue;
            }

            var preset = Array.Empty<WorkflowAssigneeCandidateDto>();
            if (step.AssigneeUserId is { } assignee
                && candidatesByUser.GetValueOrDefault(assignee) is { } candidate)
            {
                preset = [candidate];
            }
            groups.Add(new WorkflowStepCandidateGroupDto(step.Code, step.Name, step.AssigneeType, step.RoleId, preset));
        }

        return groups;
    }

    public async Task<WorkflowInstanceDto> DecideAsync(Guid taskId, DecideApprovalTaskRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var actor = DocumentAccess.RequireUser(principal);
        var instance = await Query().SingleOrDefaultAsync(x => x.Tasks.Any(t => t.Id == taskId), cancellationToken)
            ?? throw new KeyNotFoundException("Workflow task not found.");
        var definition = await db.WorkflowDefinitions.Include(x => x.Steps).SingleAsync(x => x.Id == instance.DefinitionId, cancellationToken);
        var documentForAccess = await LoadDocumentAsync(instance.DocumentId, cancellationToken);
        DocumentAccess.EnsureCanView(documentForAccess, actor, principal);
        var task = instance.Tasks.Single(x => x.Id == taskId);
        var step = definition.Steps.SingleOrDefault(x => x.Code == task.StepCode)
            ?? throw new InvalidOperationException("Workflow step configuration is missing.");
        var isAssignee = task.AssigneeUserId is { } assigned && assigned == actor;
        if (!isAssignee && task.AssigneeUserId is { } && !DocumentAccess.IsElevated(principal))
            throw new UnauthorizedAccessException("Only the assigned user can decide this step.");
        DocumentAccess.EnsureCanDecideStep(principal, step.Type == WorkflowStepTypes.Sign, step.RequiredPermission);
        if (input.Approve && !input.Return && step.Type == WorkflowStepTypes.Sign
            && (input.SigningAttemptId is not { } signingAttemptId
                || input.SigningFileId is not { } signingFileId
                || !await db.SigningAttempts.AnyAsync(x => x.Id == signingAttemptId
                    && x.DocumentId == instance.DocumentId && x.FileId == signingFileId
                    && x.UserId == actor && x.Status == HCS.DocumentService.Signing.SigningStatus.Completed
                    && x.CompletedAt >= task.CreationTime, cancellationToken)))
            throw new BusinessException("Signing:AttemptIncomplete");
        var existingDocumentHistoryIds = documentForAccess.History.Select(x => x.Id).ToHashSet();
        var existingDocumentAssignmentIds = documentForAccess.Assignments.Select(x => x.Id).ToHashSet();
        var existingTaskIds = instance.Tasks.Select(x => x.Id).ToHashSet();
        var changed = instance.Decide(taskId, input.Approve, actor, input.Comment, input.IdempotencyKey,
            definition.Steps.OrderBy(x => x.Order).ToList(), DateTime.UtcNow, input.Return);
        if (changed)
        {
            if (instance.Status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Rejected)
            {
                documentForAccess.CompleteReview(instance.Status == WorkflowInstanceStatus.Completed, actor, input.Comment, DateTime.UtcNow);

                var documentEntry = db.Entry(documentForAccess);
                var loadedVersion = documentEntry.Property(x => x.Version).OriginalValue;
                var databaseDocument = await db.Documents.AsNoTracking()
                    .Where(x => x.Id == documentForAccess.Id)
                    .Select(x => new { x.Status, x.Version })
                    .SingleAsync(cancellationToken);
                if (databaseDocument.Status != DocumentStatus.InReview)
                    throw new DbUpdateConcurrencyException("The document review status changed before the workflow decision was saved.");

                // The decision only changes the document status. Rebase the xmin
                // token so unrelated document edits made while the task was open
                // do not make the whole signing decision fail.
                documentEntry.Property(x => x.Version).OriginalValue = databaseDocument.Version;
                logger.LogInformation(
                    "Workflow decision is saving document {DocumentId}: status {Status}, xmin original {OriginalVersion}, current {CurrentVersion}, database {DatabaseVersion}, task {TaskId}, instance {InstanceId}",
                    documentForAccess.Id,
                    documentForAccess.Status,
                    loadedVersion,
                    documentEntry.Property(x => x.Version).CurrentValue,
                    databaseDocument.Version,
                    taskId,
                    instance.Id);
            }
            else
            {
                GrantWorkflowAccess(documentForAccess, instance, null, actor, DateTime.UtcNow);
            }
            // These children are added through the aggregate's private backing
            // fields. Track them explicitly so EF inserts them instead of
            // treating their client-generated Guid keys as existing rows.
            TrackNewApprovalTasks(db, instance, existingTaskIds);
            db.DocumentHistories.AddRange(documentForAccess.History
                .Where(x => !existingDocumentHistoryIds.Contains(x.Id)));
            db.DocumentAssignments.AddRange(documentForAccess.Assignments
                .Where(x => !existingDocumentAssignmentIds.Contains(x.Id)));
            AddChangeEvent(instance, DateTime.UtcNow);
            var newTaskIds = instance.Tasks.Select(x => x.Id).Where(id => !existingTaskIds.Contains(id)).ToHashSet();
            EnqueueWorkflowTaskAssigned(documentForAccess, instance, actor, DateTime.UtcNow, newTaskIds);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BusinessException("Document:WorkflowDecisionConflict");
            }
        }
        return Map(instance);
    }

    public async Task<WorkflowInstanceDto> ExtendDueDateAsync(Guid taskId, ExtendWorkflowDueDateRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var actor = DocumentAccess.RequireUser(principal);
        if (input.AdditionalDays is < 1 or > 365) throw new ArgumentOutOfRangeException(nameof(input.AdditionalDays));
        var instance = await Query().SingleOrDefaultAsync(x => x.Tasks.Any(t => t.Id == taskId), cancellationToken)
            ?? throw new KeyNotFoundException("Workflow task not found.");
        var document = await LoadDocumentAsync(instance.DocumentId, cancellationToken);
        DocumentAccess.EnsureCanView(document, actor, principal);
        var task = instance.Tasks.Single(x => x.Id == taskId);
        DocumentAccess.EnsureCanActOnWorkflowTask(principal, actor, task.AssigneeUserId);
        task.ExtendDueDate(input.AdditionalDays, DateTime.UtcNow, input.Reason);
        AddChangeEvent(instance, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
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
        var existingTaskIds = instance.Tasks.Select(x => x.Id).ToHashSet();
        instance.Resubmit(definition.Steps.OrderBy(x => x.Order).ToList(), DateTime.UtcNow, idempotencyKey);
        if (document.Status != DocumentStatus.InReview) document.StartReview(actor, DateTime.UtcNow);
        GrantWorkflowAccess(document, instance, null, actor, DateTime.UtcNow);
        TrackNewApprovalTasks(db, instance, existingTaskIds);
        AddChangeEvent(instance, DateTime.UtcNow);
        var newTaskIds = instance.Tasks.Select(x => x.Id).Where(id => !existingTaskIds.Contains(id)).ToHashSet();
        EnqueueWorkflowTaskAssigned(document, instance, actor, DateTime.UtcNow, newTaskIds);
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
    internal static void TrackNewApprovalTasks(DocumentServiceDbContext db, WorkflowInstance instance,
        IReadOnlySet<Guid> existingTaskIds)
    {
        db.ApprovalTasks.AddRange(instance.Tasks.Where(x => !existingTaskIds.Contains(x.Id)));
    }
    private ClaimsPrincipal Principal => httpContext.HttpContext?.User ?? new ClaimsPrincipal();
    private void Require(string permission)
    {
        DocumentAccess.RequireUser(Principal);
        DocumentAccess.RequirePermission(Principal, permission);
    }
    private static string NextWorkflowNumber(string sourceNumber)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var prefix = sourceNumber.Length > 49 ? sourceNumber[..49] : sourceNumber;
        return $"{prefix}-WF-{suffix}";
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

        // Keep the Word source when one exists. Submission preparation merges
        // placeholders in DOCX first and then regenerates the PDF pair.
        var useWord = !string.IsNullOrWhiteSpace(template.WordBlobName);
        var blobName = useWord ? template.WordBlobName! : template.PdfBlobName!;
        var fileName = useWord
            ? template.WordFileName ?? "workflow-template.docx"
            : template.PdfFileName ?? "workflow-template.pdf";
        var contentType = useWord
            ? template.WordContentType ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            : template.PdfContentType ?? "application/pdf";
        var number = NextWorkflowNumber(template.Code);
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
        var roleIds = definition.Steps.Where(x => x.IsBlocking &&
                x.AssigneeType == WorkflowStepAssigneeTypes.RoleInSubmitterOu && x.RoleId.HasValue)
            .Select(x => x.RoleId!.Value).Distinct().ToArray();
        var candidatesByRole = await assigneeResolver.ResolveByRolesAsync(roleIds, submitterUserId, cancellationToken);
        foreach (var step in definition.Steps.Where(x => x.IsBlocking))
        {
            if (step.AssigneeType != WorkflowStepAssigneeTypes.RoleInSubmitterOu || step.RoleId is not { } roleId)
                continue;
            var candidates = candidatesByRole.GetValueOrDefault(roleId) ?? [];
            if (candidates.Count == 0)
                throw new BusinessException("Document:NoAssigneeCandidates");
            var allowed = candidates.Select(x => x.UserId).ToHashSet();
            if (overrides.TryGetValue(step.Code, out var chosen))
            {
                if (!allowed.Contains(chosen))
                    throw new BusinessException("Document:SignerNotInRole");
                continue;
            }
            if (candidates.Count == 1)
                overrides[step.Code] = candidates[0].UserId;
            else
                throw new BusinessException("Document:ChooseSigner");
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

    private void EnqueueWorkflowTaskAssigned(DocumentAggregate document, WorkflowInstance instance, Guid senderUserId,
        DateTime now, IReadOnlySet<Guid>? newTaskIds = null)
    {
        var recipients = PendingAssigneeUserIds(instance, senderUserId, newTaskIds);
        if (recipients.Length == 0) return;
        var label = string.IsNullOrWhiteSpace(document.Title) ? document.Number : document.Title.Trim();
        var integrationEvent = new DocumentWorkflowTaskAssignedEto(Guid.NewGuid(), new DateTimeOffset(now, TimeSpan.Zero),
            CorrelationId, document.Id, instance.Id, senderUserId, label, document.Number, recipients);
        db.OutboxMessages.Add(OutboxFactory.CreateCanonical(integrationEvent, CorrelationId, now));
    }

    internal static Guid[] PendingAssigneeUserIds(WorkflowInstance instance, Guid senderUserId,
        IReadOnlySet<Guid>? newTaskIds = null) =>
        instance.Tasks
            .Where(task => task.Status == ApprovalTaskStatus.Pending
                           && task.AssigneeUserId is { } assignee
                           && assignee != Guid.Empty
                           && assignee != senderUserId
                           && (newTaskIds is null || newTaskIds.Contains(task.Id)))
            .Select(task => task.AssigneeUserId!.Value)
            .Distinct()
            .ToArray();
    internal static WorkflowInstanceDto Map(WorkflowInstance x) => new(x.Id, x.DocumentId, x.DefinitionId, x.Status, x.CurrentStep,
        x.Tasks.OrderBy(t => t.CreationTime).Select(t => new ApprovalTaskDto(t.Id, t.InstanceId, t.StepCode, t.Status, t.DecidedBy, t.DecidedAt, t.AssigneeUserId, t.DueAt, t.Comment)).ToList(), x.CreationTime);
    internal static WorkflowDefinitionDto MapDefinition(WorkflowDefinition x) => new(x.Id, x.Code, x.Name, x.KindId, x.Description, x.IsActive,
        x.Steps.OrderBy(step => step.Order).Select(step => new WorkflowStepDto(step.Id, step.Code, step.Name,
            step.Order, step.RequiredPermission, step.Type, step.AssigneeUserId, step.AssigneeType, step.RoleId,
            step.UserIds, step.DepartmentIds, step.SlaDays, step.AllowReturn)).ToList(), x.CreationTime, x.SignMode);
    private static WorkflowKindDto MapKind(WorkflowKind x) => new(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.CreationTime);
    private static WorkflowTemplateDto MapTemplate(WorkflowTemplate x) => new(x.Id, x.Code, x.Name,
        x.DefinitionId, x.Version, x.IsActive, x.CreationTime, x.WordFileId, x.WordFileName, x.PdfFileId, x.PdfFileName,
        x.TemplateJson, x.OutputFormat);
}

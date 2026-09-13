using System.Security.Claims;
using HCS.DocumentService.Integration;
using HCS.DocumentService.Storage;
using HCS.DocumentService.Workflows;
using HCS.IntegrationEvents.Auditing;
using HCS.IntegrationEvents.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Volo.Abp;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Documents;

public sealed class DocumentAppService(
    DocumentServiceDbContext db,
    IHttpContextAccessor httpContext,
    IBlobContainer<DocumentBlobContainer> documentBlobs,
    IBlobContainer<SigningBlobContainer> signingBlobs,
    ILogger<DocumentAppService> logger) : IDocumentAppService
{
    public async Task<PagedDocumentsDto> GetListAsync(string? filter = null, DocumentStatus? status = null,
        bool mine = false, int skip = 0, int take = 50, int? sourceType = null,
        Guid? documentTypeId = null, Guid? sectorId = null, Guid? urgencyId = null, Guid? confidentialityId = null,
        DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.View);
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(skip, 0);
        // List responses do not need the full file/assignment/history collections.
        // Loading those collections for every row multiplied payload size by the
        // number of children and made the list endpoint a hidden fan-out.
        var query = db.Documents.AsNoTracking()
            .Where(x => x.SourceType != DocumentSourceType.Workflow || sourceType == 3);
        query = DocumentAccess.FilterBySource(query, sourceType, userId, mine, principal);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var value = filter.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Functions.ILike(x.Number, $"%{value}%") ||
                                     EF.Functions.ILike(x.Title, $"%{value}%") ||
                                     (x.DocumentCode != null && EF.Functions.ILike(x.DocumentCode, $"%{value}%")));
        }
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (documentTypeId.HasValue) query = query.Where(x => x.DocumentTypeId == documentTypeId);
        if (sectorId.HasValue) query = query.Where(x => x.SectorId == sectorId);
        if (urgencyId.HasValue) query = query.Where(x => x.UrgencyId == urgencyId);
        if (confidentialityId.HasValue) query = query.Where(x => x.ConfidentialityId == confidentialityId);
        if (from.HasValue) query = query.Where(x => x.CreationTime >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(x => x.CreationTime < to.Value.ToUniversalTime().AddDays(1));
        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreationTime).Skip(skip).Take(take)
            .Select(x => new
            {
                Document = x,
                FileCount = x.Files.Count(f => !f.IsPendingDeletion),
                IsSent = x.History.Any(h => h.Action == DocumentSendState.SentAction) &&
                         !x.History.Any(h => h.Action == DocumentSendState.RevokedAction &&
                             h.OccurredAt >= x.History.Where(s => s.Action == DocumentSendState.SentAction)
                                 .Max(s => s.OccurredAt))
            })
            .ToListAsync(cancellationToken);
        return new PagedDocumentsDto(totalCount, items.Select(x => MapList(x.Document, x.FileCount, x.IsSent)).ToList());
    }

    public async Task<DocumentDto> CreateAsync(CreateDocumentRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.Create);
        var now = DateTime.UtcNow;
        var sourceType = input.SourceType is DocumentSourceType.Personal ? DocumentSourceType.Personal : DocumentSourceType.Archive;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var number = attempt == 0 ? ResolveNumber(input.Number, now) : GenerateNumber(now, attempt);
            var document = new DocumentAggregate(Guid.NewGuid(), number, input.Title, input.Description, userId, now, sourceType);
            document.SetDocumentCode(input.DocumentCode);
            document.SetOrganizationUnit(input.OrganizationUnitId);
            if (input.DocumentTypeId is not null || input.SectorId is not null || input.UrgencyId is not null || input.ConfidentialityId is not null)
                document.Classify(input.DocumentTypeId, input.SectorId, input.UrgencyId, input.ConfidentialityId, userId, now);
            db.Documents.Add(document);
            AddAudit("DocumentCreated", document.Id, 201, null, now);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return Map(document);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception) && attempt < 2)
            {
                db.ChangeTracker.Clear();
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                db.ChangeTracker.Clear();
                throw new BusinessException("Document:DuplicateNumber");
            }
        }

        throw new BusinessException("Document:UnableToGenerateNumber");
    }

    public async Task<DocumentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.View);
        var document = await Query().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is not null) DocumentAccess.EnsureCanView(document, userId, principal);
        return document is null ? null : Map(document);
    }

    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.Update);
        var document = await LoadAsync(id, cancellationToken);
        DocumentAccess.EnsureCanManage(document, userId, principal);
        var existingAssignmentIds = document.Assignments.Select(x => x.Id).ToHashSet();
        var existingHistoryIds = document.History.Select(x => x.Id).ToHashSet();
        document.Update(input.Title, input.Description, userId, DateTime.UtcNow);
        document.SetDocumentCode(input.DocumentCode);
        document.SetOrganizationUnit(input.OrganizationUnitId);
        document.Classify(input.DocumentTypeId, input.SectorId, input.UrgencyId, input.ConfidentialityId, userId, DateTime.UtcNow);
        TrackNewChildren(db, document, existingAssignmentIds, existingHistoryIds);
        AddAudit("DocumentUpdated", id, 200, null, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Map(document);
    }

    public async Task<DocumentDto> AssignAsync(Guid id, AssignDocumentRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        var document = await LoadAsync(id, cancellationToken);
        if (!DocumentAccess.CanInboxViewAssign(document, userId, input.Responsibility))
        {
            DocumentAccess.RequirePermission(principal, DocumentPermissions.Assign);
            DocumentAccess.EnsureCanManage(document, userId, principal);
        }
        var existingAssignmentIds = document.Assignments.Select(x => x.Id).ToHashSet();
        var existingHistoryIds = document.History.Select(x => x.Id).ToHashSet();
        var now = DateTime.UtcNow;
        var before = document.Assignments.Count;
        var existingView = document.Assignments.FirstOrDefault(x =>
            x.AssigneeUserId == input.AssigneeUserId && x.Responsibility == input.Responsibility && x.StepCode == null);
        var viewWasCurrent = existingView?.IsCurrent == true;
        var assignment = document.Assign(Guid.NewGuid(), input.AssigneeUserId, input.Responsibility, userId, now);
        if (document.Assignments.Count != before)
        {
            var integrationEvent = new DocumentAssignedEto(Guid.NewGuid(), now, CorrelationId, id,
                assignment.Id, input.AssigneeUserId, null, assignment.Responsibility);
            db.OutboxMessages.Add(OutboxFactory.CreateCanonical(integrationEvent, CorrelationId, now));
        }
        if (IsInboxView(assignment.Responsibility, assignment.StepCode) && !viewWasCurrent)
            EnqueueSentToInbox(document, userId, now, input.AssigneeUserId);
        TrackNewChildren(db, document, existingAssignmentIds, existingHistoryIds);
        AddAudit("DocumentAssigned", id, 200, input.Responsibility, now);
        await db.SaveChangesAsync(cancellationToken);
        return Map(document);
    }

    public async Task<DocumentDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.Update);
        var document = await LoadAsync(id, cancellationToken);
        DocumentAccess.EnsureCanManage(document, userId, principal);
        var existingAssignmentIds = document.Assignments.Select(x => x.Id).ToHashSet();
        var existingHistoryIds = document.History.Select(x => x.Id).ToHashSet();
        document.Submit(userId, DateTime.UtcNow);
        TrackNewChildren(db, document, existingAssignmentIds, existingHistoryIds);
        AddAudit("DocumentSubmitted", id, 200, null, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Map(document);
    }

    public async Task<DocumentDto> SendAsync(Guid id, SendDocumentRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        var document = await LoadAsync(id, cancellationToken);
        DocumentAccess.EnsureCanSend(document, userId, principal);
        if (!DocumentAccess.HasInboxView(document, userId))
            DocumentAccess.RequirePermission(principal, DocumentPermissions.Assign);
        var existingAssignmentIds = document.Assignments.Select(x => x.Id).ToHashSet();
        var existingHistoryIds = document.History.Select(x => x.Id).ToHashSet();
        var now = DateTime.UtcNow;
        document.Send(input.ReceiverUserId, input.OrganizationUnitId, userId, now);
        TrackNewChildren(db, document, existingAssignmentIds, existingHistoryIds);
        if (input.ReceiverUserId is { } receiverUserId)
            EnqueueSentToInbox(document, userId, now, receiverUserId);
        AddAudit("DocumentSent", id, 200, input.ReceiverUserId?.ToString() ?? input.OrganizationUnitId?.ToString(), now);
        await db.SaveChangesAsync(cancellationToken);
        return Map(document);
    }

    public async Task<DocumentDto> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.Assign);
        var document = await LoadAsync(id, cancellationToken);
        DocumentAccess.EnsureCanManage(document, userId, principal);
        var existingAssignmentIds = document.Assignments.Select(x => x.Id).ToHashSet();
        var existingHistoryIds = document.History.Select(x => x.Id).ToHashSet();
        var now = DateTime.UtcNow;
        var wasSent = DocumentSendState.IsActivelySent(document.History);
        document.RevokeInbox(userId, now);
        TrackNewChildren(db, document, existingAssignmentIds, existingHistoryIds);
        if (wasSent)
            EnqueueInboxCleared(id, now);
        AddAudit("DocumentRevoked", id, 200, null, now);
        await db.SaveChangesAsync(cancellationToken);
        return Map(document);
    }

    public async Task<DocumentDto> RecordActivityAsync(Guid id, DocumentActivityRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.View);
        var document = await LoadAsync(id, cancellationToken);
        DocumentAccess.EnsureCanView(document, userId, principal);
        var existingAssignmentIds = document.Assignments.Select(x => x.Id).ToHashSet();
        var existingHistoryIds = document.History.Select(x => x.Id).ToHashSet();
        var now = DateTime.UtcNow;
        try
        {
            document.RecordAccess(input.Action, userId, now);
        }
        catch (ArgumentException)
        {
            throw new BusinessException("Document:InvalidActivity");
        }
        TrackNewChildren(db, document, existingAssignmentIds, existingHistoryIds);
        AddAudit("DocumentActivity", id, 200, input.Action, now);
        await db.SaveChangesAsync(cancellationToken);
        return Map(document);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.Update);
        var document = await LoadAsync(id, cancellationToken);
        DocumentAccess.EnsureCanManage(document, userId, principal);
        if (await db.WorkflowInstances.AnyAsync(x => x.DocumentId == id && x.Status == WorkflowInstanceStatus.Running, cancellationToken))
            throw new BusinessException("Document:CannotDeleteWithRunningWorkflow");

        var now = DateTime.UtcNow;
        var documentBlobNames = document.Files.Select(x => x.BlobName).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
        var signingAttempts = await db.SigningAttempts.Where(x => x.DocumentId == id).ToListAsync(cancellationToken);
        var signingBlobNames = signingAttempts.Select(x => x.OutputBlobName)
            .Where(name => !string.IsNullOrWhiteSpace(name)).Cast<string>().Distinct().ToList();
        var workflowInstances = await db.WorkflowInstances.Include(x => x.Tasks)
            .Where(x => x.DocumentId == id).ToListAsync(cancellationToken);

        EnqueueInboxCleared(id, now);
        AddAudit("DocumentDeleted", id, 200, null, now);
        db.SigningAttempts.RemoveRange(signingAttempts);
        db.WorkflowInstances.RemoveRange(workflowInstances);
        db.Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        await TryDeleteBlobsAsync(documentBlobs, documentBlobNames, cancellationToken);
        await TryDeleteBlobsAsync(signingBlobs, signingBlobNames, cancellationToken);
    }

    private IQueryable<DocumentAggregate> Query() => db.Documents.AsNoTracking().AsSplitQuery()
        .Include(x => x.Files).Include(x => x.Assignments).Include(x => x.History);
    private async Task<DocumentAggregate> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Documents.AsSplitQuery().Include(x => x.Files).Include(x => x.Assignments).Include(x => x.History)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new KeyNotFoundException("Document not found.");
    private ClaimsPrincipal Principal => httpContext.HttpContext?.User ?? new ClaimsPrincipal();
    private Guid? UserId => Guid.TryParse(httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string CorrelationId => httpContext.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");

    internal static void TrackNewChildren(DocumentServiceDbContext db, DocumentAggregate document,
        IReadOnlySet<Guid> existingAssignmentIds, IReadOnlySet<Guid> existingHistoryIds)
    {
        // Aggregate methods append client-generated Guid children through private
        // backing fields. Track only children created by this request so EF inserts
        // them instead of treating non-empty keys as existing rows.
        db.DocumentAssignments.AddRange(document.Assignments.Where(x => !existingAssignmentIds.Contains(x.Id)));
        db.DocumentHistories.AddRange(document.History.Where(x => !existingHistoryIds.Contains(x.Id)));
    }

    private static string ResolveNumber(string? requested, DateTime now, int attempt = 0)
    {
        var normalized = requested?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized)) return normalized;
        return GenerateNumber(now, attempt);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static readonly TimeZoneInfo VietnamZone = ResolveVietnamZone();

    internal static string GenerateNumber(DateTime now, int attempt = 0)
    {
        var utc = now.Kind switch
        {
            DateTimeKind.Utc => now,
            DateTimeKind.Local => now.ToUniversalTime(),
            _ => DateTime.SpecifyKind(now, DateTimeKind.Utc)
        };
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamZone);
        var stamp = local.ToString("yyyyMMdd-HHmmss");
        return attempt <= 0 ? stamp : $"{stamp}-{attempt}";
    }

    private static TimeZoneInfo ResolveVietnamZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("ICT", TimeSpan.FromHours(7), "ICT", "ICT");
    }

    private void EnqueueSentToInbox(DocumentAggregate document, Guid senderUserId, DateTime now, params Guid[] recipientUserIds)
    {
        var recipients = recipientUserIds.Where(id => id != Guid.Empty && id != senderUserId).Distinct().ToArray();
        if (recipients.Length == 0) return;
        var label = string.IsNullOrWhiteSpace(document.Title) ? document.Number : document.Title.Trim();
        var integrationEvent = new DocumentSentToInboxEto(Guid.NewGuid(), now, CorrelationId, document.Id,
            senderUserId, label, document.Number, recipients);
        db.OutboxMessages.Add(OutboxFactory.CreateCanonical(integrationEvent, CorrelationId, now));
    }

    private void EnqueueInboxCleared(Guid documentId, DateTime now)
    {
        var integrationEvent = new DocumentInboxClearedEto(Guid.NewGuid(), now, CorrelationId, documentId);
        db.OutboxMessages.Add(OutboxFactory.CreateCanonical(integrationEvent, CorrelationId, now));
    }

    private static bool IsInboxView(string responsibility, string? stepCode) =>
        string.Equals(responsibility, "VIEW", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(stepCode);

    private async Task TryDeleteBlobsAsync<TContainer>(IBlobContainer<TContainer> container, IEnumerable<string> blobNames,
        CancellationToken cancellationToken)
        where TContainer : class
    {
        foreach (var blobName in blobNames)
        {
            try
            {
                await container.DeleteAsync(blobName, cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not delete blob {BlobName} after document removal", blobName);
            }
        }
    }

    private void AddAudit(string action, Guid id, int status, string? detail, DateTime now)
    {
        var audit = new AuditRecordCapturedEto(Guid.NewGuid(), "HCS.DocumentService", "HCS.DocumentService",
            UserId, Principal.Identity?.Name, now, 0, action, httpContext.HttpContext?.Request.Method,
            httpContext.HttpContext?.Request.Path, status, CorrelationId,
            httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            httpContext.HttpContext?.Request.Headers.UserAgent, null, detail, [],
            [new AuditEntityChangeCapturedEto(Guid.NewGuid(), now, action, id.ToString(), nameof(DocumentAggregate))]);
        db.OutboxMessages.Add(OutboxFactory.CreateAudit(audit, CorrelationId, now));
    }
    internal static DocumentDto Map(DocumentAggregate x) => new(x.Id, x.Number, x.Title, x.Description, x.Status,
        x.DocumentTypeId, x.SectorId, x.UrgencyId, x.ConfidentialityId,
        x.Files.Where(f => !f.IsPendingDeletion)
            .Select(f => new DocumentFileDto(f.Id, f.FileName, f.ContentType, f.Size, f.Sha256, f.CreationTime, f.PairedFileId)).ToList(),
        x.Assignments.Select(a => new DocumentAssignmentDto(a.Id, a.AssigneeUserId, a.Responsibility, a.AssignedAt, a.IsCurrent, a.StepCode)).ToList(),
        x.History.OrderBy(h => h.OccurredAt).Select(h => new DocumentHistoryDto(h.Id, h.Action, h.ActorUserId, h.Detail, h.OccurredAt)).ToList(),
        x.CreationTime, x.SourceType, x.ParentDocumentId, x.FromUserId, x.OrganizationUnitId,
        x.Files.Count(f => !f.IsPendingDeletion), DocumentSendState.IsActivelySent(x.History), x.DocumentCode);

    private static DocumentDto MapList(DocumentAggregate x, int fileCount, bool isSent) => new(x.Id, x.Number, x.Title, x.Description, x.Status,
        x.DocumentTypeId, x.SectorId, x.UrgencyId, x.ConfidentialityId,
        Array.Empty<DocumentFileDto>(), Array.Empty<DocumentAssignmentDto>(), Array.Empty<DocumentHistoryDto>(),
        x.CreationTime, x.SourceType, x.ParentDocumentId, x.FromUserId, x.OrganizationUnitId, fileCount, isSent, x.DocumentCode);
}

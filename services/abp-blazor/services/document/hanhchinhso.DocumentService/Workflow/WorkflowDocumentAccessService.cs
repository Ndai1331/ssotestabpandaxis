using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Workflows;

public interface IWorkflowDocumentAccessService
{
    Task<IQueryable<DocumentWorkflowInstance>> GetAccessibleInstancesAsync(
        Guid userId,
        Guid? documentId = null);
    Task<IQueryable<DocumentWorkflowInstance>> GetReceivedInstancesAsync(
        Guid userId,
        Guid? documentId = null);
    IQueryable<DocumentWorkflowInstance> ApplyDocumentFilter(
        IQueryable<DocumentWorkflowInstance> query,
        string filterText);
    Task<bool> CanAccessDocumentAsync(Guid documentId, Guid userId);
    Task<bool> CanMutateDocumentAsync(Guid documentId, Guid userId);
    Task<bool> CanAccessFileAsync(Guid fileId, Guid userId);
    Task<IReadOnlySet<Guid>> GetAccessibleFileIdsAsync(
        Guid documentId,
        Guid userId);
}

public class WorkflowDocumentAccessService :
    IWorkflowDocumentAccessService,
    ITransientDependency
{
    private readonly DocumentServiceDbContext _db;
    private readonly IWorkflowIdentityMembershipResolver _membershipResolver;

    public WorkflowDocumentAccessService(
        DocumentServiceDbContext db,
        IWorkflowIdentityMembershipResolver membershipResolver)
    {
        _db = db;
        _membershipResolver = membershipResolver;
    }

    public async Task<IQueryable<DocumentWorkflowInstance>>
        GetAccessibleInstancesAsync(
            Guid userId,
            Guid? documentId = null)
    {
        var query = BaseQuery(documentId);
        var received = await GetReceivedInstancesAsync(userId, documentId);
        return query.Where(instance =>
            instance.InitiatorUserId == userId ||
            received.Any(receivedInstance =>
                receivedInstance.Id == instance.Id));
    }

    public async Task<IQueryable<DocumentWorkflowInstance>>
        GetReceivedInstancesAsync(
            Guid userId,
            Guid? documentId = null)
    {
        var query = BaseQuery(documentId);
        var memberships = await _membershipResolver.ResolveAllAsync(userId);

        return query.Where(instance =>
            _db.DocumentAssignments.Any(assignment =>
                assignment.InstanceId == instance.Id &&
                assignment.ReceiverUserId == userId) ||
            instance.Steps.Any(step =>
                step.Type == WorkflowStepType.View &&
                (instance.SignMode == WorkflowSignMode.Parallel ||
                 instance.Status == DocumentWorkflowStatus.Completed ||
                 (instance.CurrentCommittedStepId.HasValue &&
                  instance.Steps.Any(current =>
                      current.Id ==
                          instance.CurrentCommittedStepId.Value &&
                      step.Order <= current.Order)) ||
                 (!instance.CurrentCommittedStepId.HasValue &&
                  instance.Steps.Any(assignedStep =>
                      assignedStep.Order >= step.Order &&
                      _db.DocumentAssignments.Any(assignment =>
                          assignment.InstanceId == instance.Id &&
                          assignment.CommittedStepId ==
                              assignedStep.Id)))) &&
                step.ViewScopes.Any(scope =>
                    scope.UserId == userId ||
                    (scope.OrganizationUnitId.HasValue &&
                     memberships.Contains(
                         scope.OrganizationUnitId.Value)))));
    }

    public async Task<bool> CanAccessFileAsync(Guid fileId, Guid userId)
    {
        var file = await _db.DocumentFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == fileId && !x.BlobDeletionPending);
        if (file is null)
        {
            return false;
        }
        var ownerAccess = await _db.Documents
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == file.DocumentId &&
                (x.FromUserId == userId ||
                 x.ReceiverUserId == userId) &&
                (x.SourceType != DocumentSourceType.Workflow ||
                 !_db.DocumentWorkflowInstances.Any(instance =>
                     instance.DocumentId == x.Id)));
        if (ownerAccess)
        {
            return true;
        }

        var accessible = await GetAccessibleInstancesAsync(
            userId, file.DocumentId);
        return await accessible.AnyAsync(instance =>
            instance.SourceFileId == fileId ||
            instance.CurrentSignedFileId == fileId ||
            _db.DocumentAssignments.Any(assignment =>
                assignment.InstanceId == instance.Id &&
                assignment.DocumentFileResultId == fileId));
    }

    public async Task<IReadOnlySet<Guid>> GetAccessibleFileIdsAsync(
        Guid documentId,
        Guid userId)
    {
        var hasWorkflow = await _db.DocumentWorkflowInstances
            .AsNoTracking()
            .AnyAsync(x => x.DocumentId == documentId);
        if (!hasWorkflow &&
            await _db.Documents.AsNoTracking().AnyAsync(x =>
                x.Id == documentId &&
                (x.FromUserId == userId ||
                 x.ReceiverUserId == userId)))
        {
            return (await _db.DocumentFiles.AsNoTracking()
                    .Where(x =>
                        x.DocumentId == documentId &&
                        !x.BlobDeletionPending)
                    .Select(x => x.Id)
                    .ToListAsync())
                .ToHashSet();
        }

        var accessible = await GetAccessibleInstancesAsync(
            userId, documentId);
        var sourceIds = accessible.Select(x => x.SourceFileId);
        var currentIds = accessible
            .Where(x => x.CurrentSignedFileId.HasValue)
            .Select(x => x.CurrentSignedFileId!.Value);
        var resultIds = _db.DocumentAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.DocumentFileResultId.HasValue &&
                accessible.Any(instance =>
                    instance.Id == assignment.InstanceId))
            .Select(x => x.DocumentFileResultId!.Value);
        return (await sourceIds
                .Concat(currentIds)
                .Concat(resultIds)
                .Distinct()
                .ToListAsync())
            .ToHashSet();
    }

    public IQueryable<DocumentWorkflowInstance> ApplyDocumentFilter(
        IQueryable<DocumentWorkflowInstance> query,
        string filterText)
    {
        var filter = filterText.Trim();
        return query.Where(instance =>
            _db.Documents.Any(document =>
                document.Id == instance.DocumentId &&
                (document.Title.Contains(filter) ||
                 (document.Number != null &&
                  document.Number.Contains(filter)) ||
                 document.StorageNumber.Contains(filter))));
    }

    public async Task<bool> CanAccessDocumentAsync(
        Guid documentId,
        Guid userId)
    {
        var hasWorkflow = await _db.DocumentWorkflowInstances
            .AsNoTracking()
            .AnyAsync(x => x.DocumentId == documentId);
        if (!hasWorkflow &&
            await _db.Documents.AsNoTracking().AnyAsync(x =>
                x.Id == documentId &&
                (x.FromUserId == userId ||
                 x.ReceiverUserId == userId)))
        {
            return true;
        }
        var accessible = await GetAccessibleInstancesAsync(
            userId, documentId);
        return await accessible.AnyAsync();
    }

    public async Task<bool> CanMutateDocumentAsync(
        Guid documentId,
        Guid userId)
    {
        var hasWorkflow = await _db.DocumentWorkflowInstances
            .AsNoTracking()
            .AnyAsync(x => x.DocumentId == documentId);
        if (!hasWorkflow)
        {
            return await _db.Documents.AsNoTracking().AnyAsync(x =>
                x.Id == documentId &&
                (x.FromUserId == userId ||
                 x.ReceiverUserId == userId));
        }
        return await _db.DocumentWorkflowInstances
            .AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .AnyAsync(instance =>
                instance.InitiatorUserId == userId ||
                _db.DocumentAssignments.Any(assignment =>
                    assignment.InstanceId == instance.Id &&
                    assignment.ReceiverUserId == userId &&
                    assignment.IsCurrent &&
                    assignment.Status ==
                        DocumentAssignmentStatus.Pending));
    }

    private IQueryable<DocumentWorkflowInstance> BaseQuery(Guid? documentId)
    {
        var query = _db.DocumentWorkflowInstances.AsNoTracking();
        return documentId.HasValue
            ? query.Where(x => x.DocumentId == documentId.Value)
            : query;
    }

}

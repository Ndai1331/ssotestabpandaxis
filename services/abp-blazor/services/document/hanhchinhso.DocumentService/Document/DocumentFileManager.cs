using Microsoft.Extensions.Logging;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.DistributedLocking;
using Volo.Abp.MultiTenancy;
using hanhchinhso.DocumentService.Signing;
using hanhchinhso.DocumentService.Workflows;

namespace hanhchinhso.DocumentService.Documents;

public class DocumentFileManager : ITransientDependency
{
    private readonly IRepository<DocumentFile, Guid> _files;
    private readonly IRepository<DocumentBlobCleanup, Guid> _cleanups;
    private readonly IBlobContainer<DocumentBlobContainer> _blobs;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<DocumentFileManager> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly ICurrentTenant _currentTenant;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IRepository<SigningAttempt, Guid> _signingAttempts;
    private readonly IRepository<DocumentAssignment, Guid> _assignments;
    private readonly IRepository<DocumentWorkflowInstance, Guid>
        _workflowInstances;

    public DocumentFileManager(
        IRepository<DocumentFile, Guid> files,
        IRepository<DocumentBlobCleanup, Guid> cleanups,
        IBlobContainer<DocumentBlobContainer> blobs,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<DocumentFileManager> logger,
        IDataFilter dataFilter,
        ICurrentTenant currentTenant,
        IAbpDistributedLock distributedLock,
        IRepository<SigningAttempt, Guid> signingAttempts,
        IRepository<DocumentAssignment, Guid> assignments,
        IRepository<DocumentWorkflowInstance, Guid> workflowInstances)
    {
        _files = files;
        _cleanups = cleanups;
        _blobs = blobs;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
        _dataFilter = dataFilter;
        _currentTenant = currentTenant;
        _distributedLock = distributedLock;
        _signingAttempts = signingAttempts;
        _assignments = assignments;
        _workflowInstances = workflowInstances;
    }

    public async Task<DocumentFile> SaveAsync(
        DocumentFile entity,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var cleanup = new DocumentBlobCleanup(
            Guid.NewGuid(), entity.TenantId, entity.BlobName);
        await using var uploadHandle = await _distributedLock.TryAcquireAsync(
            $"document-blob-cleanup:{cleanup.Id:N}",
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (uploadHandle is null)
        {
            throw new Volo.Abp.UserFriendlyException(
                "The file store is busy. Please retry the upload.");
        }

        using (var markerUow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: true))
        {
            await _cleanups.InsertAsync(cleanup, autoSave: true, cancellationToken);
            await markerUow.CompleteAsync(cancellationToken);
        }

        var blobWritten = false;
        try
        {
            await _blobs.SaveAsync(
                entity.BlobName, content, overrideExisting: false, cancellationToken);
            blobWritten = true;

            using var uow = _unitOfWorkManager.Begin(
                requiresNew: true, isTransactional: true);
            await _files.InsertAsync(entity, autoSave: true, cancellationToken);
            await _cleanups.DeleteAsync(cleanup, autoSave: true, cancellationToken);
            await uow.CompleteAsync(cancellationToken);
            return entity;
        }
        catch
        {
            if (blobWritten)
            {
                try
                {
                    using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await _blobs.DeleteAsync(entity.BlobName, cleanupTimeout.Token);
                    await DeleteCleanupMarkerAsync(cleanup.Id, cleanupTimeout.Token);
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(
                        cleanupException,
                        "Failed to compensate blob {BlobName} after metadata transaction failure",
                        entity.BlobName);
                }
            }

            throw;
        }
    }

    public async Task RequestDeleteAsync(
        Guid fileId,
        string concurrencyStamp,
        CancellationToken cancellationToken = default)
    {
        Guid documentId;
        using (var readUow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: false))
        {
            var existing = await _files.GetAsync(
                fileId, cancellationToken: cancellationToken);
            documentId = existing.DocumentId;
            await readUow.CompleteAsync(cancellationToken);
        }

        var tenantKey = _currentTenant.Id?.ToString("N") ?? "host";
        await using var documentHandle = await _distributedLock.TryAcquireAsync(
            $"document-workflow-document:{tenantKey}:{documentId:N}",
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (documentHandle is null)
        {
            throw new Volo.Abp.UserFriendlyException(
                "The document is busy. Please retry deleting the file.");
        }

        var alreadyPending = false;
        using (var uow = _unitOfWorkManager.Begin(
                         requiresNew: true, isTransactional: true))
        {
            var entity = await _files.GetAsync(
                fileId, cancellationToken: cancellationToken);
            alreadyPending = entity.BlobDeletionPending;
            if (!alreadyPending && !string.Equals(
                    entity.ConcurrencyStamp,
                    concurrencyStamp,
                    StringComparison.Ordinal))
            {
                throw new Volo.Abp.Data.AbpDbConcurrencyException();
            }

            if (!alreadyPending)
            {
                if (await _files.AnyAsync(x =>
                        x.SourceFileId == fileId) ||
                    await _assignments.AnyAsync(x =>
                        x.DocumentFileResultId == fileId) ||
                    await _workflowInstances.AnyAsync(x =>
                        x.SourceFileId == fileId ||
                        x.CurrentSignedFileId == fileId) ||
                    await _signingAttempts.AnyAsync(x =>
                        x.SourceFileId == fileId ||
                        x.ResultFileId == fileId))
                {
                    throw new Volo.Abp.BusinessException(
                        "DocumentService:DocumentFileInUse");
                }
                entity.MarkBlobDeletionPending();
                await _files.UpdateAsync(entity, autoSave: true, cancellationToken);
            }
            await uow.CompleteAsync(cancellationToken);
        }

        try
        {
            using var purgeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await TryPurgeAsync(fileId, purgeTimeout.Token);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Immediate purge for document file {FileId} failed; background cleanup will retry",
                fileId);
        }
    }

    public async Task<bool> TryPurgeAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        await using var handle = await _distributedLock.TryAcquireAsync(
            $"document-file-purge:{fileId:N}",
            TimeSpan.Zero,
            cancellationToken);
        if (handle is null)
        {
            return false;
        }

        DocumentFile? entity;
        using (var readUow = _unitOfWorkManager.Begin(
                         requiresNew: true, isTransactional: false))
        {
            entity = await _files.FindAsync(fileId, cancellationToken: cancellationToken);
            await readUow.CompleteAsync(cancellationToken);
        }

        if (entity is null || !entity.BlobDeletionPending)
        {
            return false;
        }

        try
        {
            await _blobs.DeleteAsync(entity.BlobName, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Blob deletion for document file {FileId} will be retried",
                entity.Id);
            return false;
        }

        using var deleteUow = _unitOfWorkManager.Begin(
            requiresNew: true, isTransactional: true);
        var current = await _files.FindAsync(fileId, cancellationToken: cancellationToken);
        if (current?.BlobDeletionPending == true)
        {
            await _files.DeleteAsync(current, autoSave: true, cancellationToken);
        }
        await deleteUow.CompleteAsync(cancellationToken);
        return true;
    }

    public async Task<int> ReconcilePendingAsync(
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        List<(Guid Id, Guid? TenantId)> pending;
        List<(Guid Id, Guid? TenantId)> orphanMarkers;
        using (_dataFilter.Disable<IMultiTenant>())
        using (var uow = _unitOfWorkManager.Begin(
                         requiresNew: true, isTransactional: false))
        {
            var query = await _files.GetQueryableAsync();
            pending = query
                .Where(x => x.BlobDeletionPending)
                .OrderBy(x => x.LastModificationTime)
                .Select(x => new ValueTuple<Guid, Guid?>(x.Id, x.TenantId))
                .Take(maxCount)
                .ToList();
            var cleanupQuery = await _cleanups.GetQueryableAsync();
            orphanMarkers = cleanupQuery
                .OrderBy(x => x.CreationTime)
                .Select(x => new ValueTuple<Guid, Guid?>(x.Id, x.TenantId))
                .Take(maxCount)
                .ToList();
            await uow.CompleteAsync(cancellationToken);
        }

        var purged = 0;
        foreach (var (id, tenantId) in pending)
        {
            using (_currentTenant.Change(tenantId))
            {
                if (await TryPurgeAsync(id, cancellationToken))
                {
                    purged++;
                }
            }
        }

        foreach (var (id, tenantId) in orphanMarkers)
        {
            using (_currentTenant.Change(tenantId))
            {
                if (await TryCleanupOrphanAsync(id, cancellationToken))
                {
                    purged++;
                }
            }
        }

        return purged;
    }

    private async Task<bool> TryCleanupOrphanAsync(
        Guid cleanupId,
        CancellationToken cancellationToken)
    {
        await using var handle = await _distributedLock.TryAcquireAsync(
            $"document-blob-cleanup:{cleanupId:N}",
            TimeSpan.Zero,
            cancellationToken);
        if (handle is null)
        {
            return false;
        }

        DocumentBlobCleanup? cleanup;
        using (var uow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: false))
        {
            cleanup = await _cleanups.FindAsync(
                cleanupId, cancellationToken: cancellationToken);
            await uow.CompleteAsync(cancellationToken);
        }
        if (cleanup is null)
        {
            return true;
        }

        try
        {
            await _blobs.DeleteAsync(cleanup.BlobName, cancellationToken);
            await DeleteCleanupMarkerAsync(cleanup.Id, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Orphan blob cleanup {CleanupId} will be retried",
                cleanup.Id);
            return false;
        }
    }

    private async Task DeleteCleanupMarkerAsync(
        Guid cleanupId,
        CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true, isTransactional: true);
        var marker = await _cleanups.FindAsync(
            cleanupId, cancellationToken: cancellationToken);
        if (marker is not null)
        {
            await _cleanups.DeleteAsync(marker, autoSave: true, cancellationToken);
        }
        await uow.CompleteAsync(cancellationToken);
    }
}

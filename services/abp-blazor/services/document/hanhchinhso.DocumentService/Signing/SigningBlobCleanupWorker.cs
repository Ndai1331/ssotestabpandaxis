using Microsoft.Extensions.Logging;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using Volo.Abp.BackgroundWorkers;

namespace hanhchinhso.DocumentService.Signing;

public sealed class SigningBlobCleanupWorker :
    AsyncPeriodicBackgroundWorkerBase,
    ISingletonDependency
{
    private readonly IRepository<SigningBlobCleanup, Guid> _cleanups;
    private readonly IBlobContainer<SigningBlobContainer> _blobs;
    private readonly IDataFilter _dataFilter;
    private readonly ICurrentTenant _currentTenant;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<SigningBlobCleanupWorker> _logger;
    private readonly SigningAssetManager _assetManager;

    public SigningBlobCleanupWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        IRepository<SigningBlobCleanup, Guid> cleanups,
        IBlobContainer<SigningBlobContainer> blobs,
        IDataFilter dataFilter,
        ICurrentTenant currentTenant,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<SigningBlobCleanupWorker> logger,
        SigningAssetManager assetManager)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 60_000;
        _cleanups = cleanups;
        _blobs = blobs;
        _dataFilter = dataFilter;
        _currentTenant = currentTenant;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
        _assetManager = assetManager;
    }

    protected override async Task DoWorkAsync(
        PeriodicBackgroundWorkerContext workerContext)
    {
        List<(Guid Id, Guid? TenantId)> pending;
        using (_dataFilter.Disable<IMultiTenant>())
        using (var uow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: false))
        {
            var query = await _cleanups.GetQueryableAsync();
            pending = query.OrderBy(x => x.CreationTime)
                .Select(x => new ValueTuple<Guid, Guid?>(
                    x.Id, x.TenantId))
                .Take(100)
                .ToList();
            await uow.CompleteAsync();
        }

        foreach (var (id, tenantId) in pending)
        {
            using (_currentTenant.Change(tenantId))
            {
                await TryCleanupAsync(id);
            }
        }
        var pendingAssets =
            await _assetManager.GetPendingAssetsAsync(100);
        foreach (var (id, tenantId) in pendingAssets)
        {
            using (_currentTenant.Change(tenantId))
            {
                await _assetManager.TryPurgeAsync(
                    id, CancellationToken.None);
            }
        }
    }

    private async Task TryCleanupAsync(Guid id)
    {
        await using var handle = await _distributedLock.TryAcquireAsync(
            $"signing-blob-cleanup:{id:N}", TimeSpan.Zero);
        if (handle is null)
        {
            return;
        }
        SigningBlobCleanup? marker;
        using (var readUow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: false))
        {
            marker = await _cleanups.FindAsync(id);
            await readUow.CompleteAsync();
        }
        if (marker is null)
        {
            return;
        }
        try
        {
            await _blobs.DeleteAsync(
                marker.BlobName, CancellationToken.None);
            using var deleteUow = _unitOfWorkManager.Begin(
                requiresNew: true, isTransactional: true);
            var current = await _cleanups.FindAsync(id);
            if (current is not null)
            {
                await _cleanups.DeleteAsync(current, autoSave: true);
            }
            await deleteUow.CompleteAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Signing orphan cleanup {CleanupId} will retry",
                id);
        }
    }
}

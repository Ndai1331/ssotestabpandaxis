using Microsoft.Extensions.Logging;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Data;

namespace hanhchinhso.DocumentService.Signing;

public sealed class SigningAssetManager : ITransientDependency
{
    private readonly IRepository<SigningAsset, Guid> _assets;
    private readonly IRepository<SigningBlobCleanup, Guid> _cleanups;
    private readonly IBlobContainer<SigningBlobContainer> _blobs;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ILogger<SigningAssetManager> _logger;
    private readonly IRepository<UserSignature, Guid> _userSignatures;
    private readonly IRepository<SignatureSetting, Guid> _settings;
    private readonly ISigningAssetLock _assetLock;
    private readonly ICurrentTenant _currentTenant;
    private readonly IDataFilter _dataFilter;

    public SigningAssetManager(
        IRepository<SigningAsset, Guid> assets,
        IRepository<SigningBlobCleanup, Guid> cleanups,
        IBlobContainer<SigningBlobContainer> blobs,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpDistributedLock distributedLock,
        ILogger<SigningAssetManager> logger,
        IRepository<UserSignature, Guid> userSignatures,
        IRepository<SignatureSetting, Guid> settings,
        ISigningAssetLock assetLock,
        ICurrentTenant currentTenant,
        IDataFilter dataFilter)
    {
        _assets = assets;
        _cleanups = cleanups;
        _blobs = blobs;
        _unitOfWorkManager = unitOfWorkManager;
        _distributedLock = distributedLock;
        _logger = logger;
        _userSignatures = userSignatures;
        _settings = settings;
        _assetLock = assetLock;
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

    public async Task<SigningAsset> SaveAsync(
        SigningAsset asset,
        Stream content,
        CancellationToken cancellationToken)
    {
        var marker = new SigningBlobCleanup(
            Guid.NewGuid(), asset.TenantId, asset.BlobName);
        await using var handle = await _distributedLock.TryAcquireAsync(
            $"signing-blob-cleanup:{marker.Id:N}",
            TimeSpan.FromSeconds(30),
            cancellationToken);
        if (handle is null)
        {
            throw new Volo.Abp.UserFriendlyException(
                "The signing asset store is busy. Please retry.");
        }

        using (var markerUow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: true))
        {
            await _cleanups.InsertAsync(
                marker, autoSave: true, cancellationToken);
            await markerUow.CompleteAsync(cancellationToken);
        }

        var written = false;
        try
        {
            await _blobs.SaveAsync(
                asset.BlobName,
                content,
                overrideExisting: false,
                cancellationToken);
            written = true;
            using var uow = _unitOfWorkManager.Begin(
                requiresNew: true, isTransactional: true);
            await _assets.InsertAsync(
                asset, autoSave: true, cancellationToken);
            await _cleanups.DeleteAsync(
                marker, autoSave: true, cancellationToken);
            await uow.CompleteAsync(cancellationToken);
            return asset;
        }
        catch
        {
            if (written)
            {
                try
                {
                    await _blobs.DeleteAsync(
                        asset.BlobName, CancellationToken.None);
                    using var cleanupUow = _unitOfWorkManager.Begin(
                        requiresNew: true, isTransactional: true);
                    var persisted = await _cleanups.FindAsync(
                        marker.Id,
                        cancellationToken: CancellationToken.None);
                    if (persisted is not null)
                    {
                        await _cleanups.DeleteAsync(
                            persisted,
                            autoSave: true,
                            CancellationToken.None);
                    }
                    await cleanupUow.CompleteAsync(
                        CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to compensate signing blob {BlobName}",
                        asset.BlobName);
                }
            }
            throw;
        }
    }

    public Task RequestDeleteAsync(
        Guid assetId,
        string concurrencyStamp,
        CancellationToken cancellationToken) =>
        _assetLock.ExecuteAsync(
            _currentTenant.Id,
            [assetId],
            async () =>
            {
                using (var uow = _unitOfWorkManager.Begin(
                           requiresNew: true, isTransactional: true))
                {
                    var asset = await _assets.GetAsync(
                        assetId,
                        cancellationToken: cancellationToken);
                    if (!string.Equals(
                            asset.ConcurrencyStamp,
                            concurrencyStamp,
                            StringComparison.Ordinal))
                    {
                        throw new Volo.Abp.Data.AbpDbConcurrencyException();
                    }
                    if (await _settings.AnyAsync(x =>
                            x.LayoutAssetId == assetId) ||
                        await _userSignatures.AnyAsync(x =>
                            x.SignatureAssetId == assetId ||
                            x.SealAssetId == assetId))
                    {
                        throw new Volo.Abp.BusinessException(
                            "DocumentService:SigningAssetInUse");
                    }
                    asset.MarkBlobDeletionPending();
                    await _assets.UpdateAsync(
                        asset,
                        autoSave: true,
                        cancellationToken);
                    await uow.CompleteAsync(cancellationToken);
                }
                await TryPurgeAsync(assetId, cancellationToken);
                return true;
            });

    public async Task<bool> TryPurgeAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        SigningAsset? asset;
        using (var readUow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: false))
        {
            asset = await _assets.FindAsync(
                assetId, cancellationToken: cancellationToken);
            await readUow.CompleteAsync(cancellationToken);
        }
        if (asset?.BlobDeletionPending != true)
        {
            return false;
        }
        try
        {
            await _blobs.DeleteAsync(
                asset.BlobName, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Signing asset purge {AssetId} will retry",
                assetId);
            return false;
        }
        using var deleteUow = _unitOfWorkManager.Begin(
            requiresNew: true, isTransactional: true);
        var current = await _assets.FindAsync(
            assetId, cancellationToken: cancellationToken);
        if (current?.BlobDeletionPending == true)
        {
            await _assets.DeleteAsync(
                current, autoSave: true, cancellationToken);
        }
        await deleteUow.CompleteAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<(Guid Id, Guid? TenantId)>>
        GetPendingAssetsAsync(int maxCount)
    {
        using (_dataFilter.Disable<IMultiTenant>())
        using (var uow = _unitOfWorkManager.Begin(
                   requiresNew: true, isTransactional: false))
        {
            var query = await _assets.GetQueryableAsync();
            var rows = query.Where(x => x.BlobDeletionPending)
                .OrderBy(x => x.LastModificationTime)
                .Select(x => new ValueTuple<Guid, Guid?>(
                    x.Id, x.TenantId))
                .Take(maxCount)
                .ToList();
            await uow.CompleteAsync();
            return rows;
        }
    }
}

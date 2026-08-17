using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace hanhchinhso.DocumentService.Signing;

public sealed class SigningAttemptRecoveryWorker :
    AsyncPeriodicBackgroundWorkerBase,
    ISingletonDependency
{
    public SigningAttemptRecoveryWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 60_000;
    }

    protected override async Task DoWorkAsync(
        PeriodicBackgroundWorkerContext context)
    {
        var recovery = context.ServiceProvider
            .GetRequiredService<SigningAttemptRecoveryManager>();
        await recovery.ReconcileAsync(CancellationToken.None);
    }
}

public sealed class SigningAttemptRecoveryManager : ITransientDependency
{
    private readonly DocumentServiceDbContext _db;
    private readonly IBlobContainer<DocumentBlobContainer> _blobs;
    private readonly DocumentFileManager _files;
    private readonly IDataFilter _dataFilter;
    private readonly ICurrentTenant _tenant;
    private readonly IAbpDistributedLock _locks;
    private readonly IUnitOfWorkManager _uows;
    private readonly ILogger<SigningAttemptRecoveryManager> _logger;

    public SigningAttemptRecoveryManager(
        DocumentServiceDbContext db,
        IBlobContainer<DocumentBlobContainer> blobs,
        DocumentFileManager files,
        IDataFilter dataFilter,
        ICurrentTenant tenant,
        IAbpDistributedLock locks,
        IUnitOfWorkManager uows,
        ILogger<SigningAttemptRecoveryManager> logger)
    {
        _db = db;
        _blobs = blobs;
        _files = files;
        _dataFilter = dataFilter;
        _tenant = tenant;
        _locks = locks;
        _uows = uows;
        _logger = logger;
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        List<(Guid Id, Guid? TenantId)> pending;
        using (_dataFilter.Disable<IMultiTenant>())
        using (var uow = _uows.Begin(
                   requiresNew: true, isTransactional: false))
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            pending = await _db.SigningAttempts
                .AsNoTracking()
                .Where(x =>
                    x.StartedAtUtc < cutoff &&
                    ((x.Status == SigningAttemptStatus.Processing) ||
                     (x.Status == SigningAttemptStatus.Failed &&
                      x.PendingResultFileId != null)))
                .OrderBy(x => x.StartedAtUtc)
                .Select(x => new ValueTuple<Guid, Guid?>(
                    x.Id, x.TenantId))
                .Take(100)
                .ToListAsync(cancellationToken);
            await uow.CompleteAsync(cancellationToken);
        }
        foreach (var (id, tenantId) in pending)
        {
            using (_tenant.Change(tenantId))
            {
                await TryRecoverAsync(id, cancellationToken);
            }
        }
    }

    private async Task TryRecoverAsync(
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            SigningAttempt attempt;
            using (var readUow = _uows.Begin(
                       requiresNew: true, isTransactional: false))
            {
                attempt = await _db.SigningAttempts
                    .AsNoTracking()
                    .SingleAsync(
                        x => x.Id == attemptId,
                        cancellationToken);
                await readUow.CompleteAsync(cancellationToken);
            }
            var tenantKey = attempt.TenantId?.ToString("N") ?? "host";
            await using var handle = await _locks.TryAcquireAsync(
                $"document-signing-attempt:{tenantKey}:" +
                attempt.IdempotencyKey,
                TimeSpan.Zero,
                cancellationToken);
            if (handle is null)
            {
                return;
            }
            if (attempt.Status == SigningAttemptStatus.Succeeded)
            {
                return;
            }
            if (attempt.PendingResultFileId.HasValue &&
                attempt.PendingResultBlobName is not null)
            {
                var file = await _db.DocumentFiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.Id == attempt.PendingResultFileId,
                        cancellationToken);
                if (file is not null && !file.BlobDeletionPending)
                {
                    await _files.RequestDeleteAsync(
                        file.Id,
                        file.ConcurrencyStamp,
                        cancellationToken);
                }
                else
                {
                    await _blobs.DeleteAsync(
                        attempt.PendingResultBlobName,
                        cancellationToken);
                }
            }
            using var writeUow = _uows.Begin(
                requiresNew: true, isTransactional: true);
            var current = await _db.SigningAttempts
                .SingleAsync(x => x.Id == attemptId, cancellationToken);
            if (current.Status == SigningAttemptStatus.Processing)
            {
                current.Fail("SigningArtifactRecovery", DateTime.UtcNow);
            }
            current.ClearPendingResult();
            await _db.SaveChangesAsync(cancellationToken);
            await writeUow.CompleteAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Signing attempt {AttemptId} recovery will retry",
                attemptId);
        }
    }
}

using hanhchinhso.DocumentService.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace hanhchinhso.DocumentService.HealthChecks;

public class DocumentServiceDatabaseCheck : IHealthCheck, ITransientDependency
{
    private readonly IDbContextProvider<DocumentServiceDbContext> _dbContextProvider;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public DocumentServiceDatabaseCheck(
        IDbContextProvider<DocumentServiceDbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _dbContextProvider = dbContextProvider;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var unitOfWork = _unitOfWorkManager.Begin(
                requiresNew: true,
                isTransactional: false);
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            var canConnect = await dbContext.Database.CanConnectAsync(timeout.Token);
            await unitOfWork.CompleteAsync(timeout.Token);

            return canConnect
                ? HealthCheckResult.Healthy("Document database is reachable.")
                : HealthCheckResult.Unhealthy("Document database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Error while checking Document database.",
                exception);
        }
    }
}

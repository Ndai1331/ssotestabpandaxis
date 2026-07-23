using Microsoft.Extensions.Diagnostics.HealthChecks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.AIManagement.Workspaces;

namespace abptestwithsso.AIManagementService.HealthChecks;

public class AIManagementServiceDatabaseCheck : IHealthCheck, ITransientDependency
{
    private readonly IWorkspaceRepository _workspaceRepository;

    public AIManagementServiceDatabaseCheck(IWorkspaceRepository workspaceRepository)
    {
        _workspaceRepository = workspaceRepository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbConnectionCancellationTokenProvider = new CancellationTokenSource();
            dbConnectionCancellationTokenProvider.CancelAfter(TimeSpan.FromSeconds(4));
            await _workspaceRepository.FirstOrDefaultAsync(cancellationToken: dbConnectionCancellationTokenProvider.Token);

            return HealthCheckResult.Healthy($"Could connect to database and get record.");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy($"Error when trying to get database record. ", e);
        }
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.WorkflowService.HealthChecks;

public class WorkflowServiceDatabaseCheck : IHealthCheck, ITransientDependency
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WorkflowServiceDatabaseCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Data.WorkflowServiceDbContext>();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));
            var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
            return canConnect
                ? HealthCheckResult.Healthy("Connected to WorkflowService database.")
                : HealthCheckResult.Unhealthy("Cannot connect to WorkflowService database.");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("Error connecting to WorkflowService database.", e);
        }
    }
}

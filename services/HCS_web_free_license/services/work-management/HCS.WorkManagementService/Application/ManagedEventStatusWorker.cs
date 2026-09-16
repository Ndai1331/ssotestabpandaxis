namespace HCS.WorkManagementService.Application;

public sealed class ManagedEventStatusWorker(IServiceScopeFactory scopeFactory, ILogger<ManagedEventStatusWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var changed = await scope.ServiceProvider
                    .GetRequiredService<ManagedEventStatusSynchronizer>()
                    .SynchronizeAsync(stoppingToken);
                if (changed > 0)
                    logger.LogInformation("Applied scheduled managed event statuses. Changed={Changed}", changed);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Managed event status cycle failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}

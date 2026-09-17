using HCS.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HCS.PlatformService;

/// <summary>
/// Identity APIs run on Platform. ABP seeds <c>admin</c> as a public role; HCS
/// clears that flag here so the live database matches the product rule without
/// waiting for Auth Server or DbMigrator.
/// </summary>
public sealed class RolePermissionSyncHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RolePermissionSyncHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider
                    .GetRequiredService<HCSRolePermissionSynchronizer>()
                    .SynchronizeExistingRolesAsync();

                logger.LogInformation("Application role flags synchronized at Platform startup.");
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && !stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug(exception,
                    "Role flag synchronization attempt {Attempt}/{MaxAttempts} failed; retrying.",
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Could not synchronize application role flags at Platform startup.");
                return;
            }
        }
    }
}

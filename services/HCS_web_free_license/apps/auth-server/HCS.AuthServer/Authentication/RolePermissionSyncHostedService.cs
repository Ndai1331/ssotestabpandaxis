using HCS.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HCS.AuthServer;

/// <summary>
/// Synchronizes built-in role permissions when Auth Server starts. This covers
/// local development setups that launch services directly and do not execute the
/// standalone DbMigrator process. The employee role (<c>nhanvien</c>) is reset
/// to the product allowlist on each run.
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

                logger.LogInformation("Application role permissions synchronized at Auth Server startup.");
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && !stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug(exception,
                    "Role permission synchronization attempt {Attempt}/{MaxAttempts} failed; retrying.",
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // Database migrations may still be running in another process. The
                // normal DbMigrator seed remains the fallback, so Auth Server stays
                // available even if all startup retries fail.
                logger.LogWarning(exception, "Could not synchronize application role permissions at startup.");
                return;
            }
        }
    }
}

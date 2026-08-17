using Volo.Abp.DependencyInjection;

namespace hanhchinhso.WorkflowService.Data;

public class WorkflowServiceDataSeeder : ITransientDependency
{
    private readonly ILogger<WorkflowServiceDataSeeder> _logger;

    public WorkflowServiceDataSeeder(ILogger<WorkflowServiceDataSeeder> logger)
    {
        _logger = logger;
    }

    public Task SeedAsync(Guid? tenantId = null)
    {
        _logger.LogInformation("WorkflowService seed completed (no seed data required).");
        return Task.CompletedTask;
    }
}

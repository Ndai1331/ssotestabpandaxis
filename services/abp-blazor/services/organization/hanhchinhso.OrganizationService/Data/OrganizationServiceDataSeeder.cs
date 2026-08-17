using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.OrganizationService.Data;

public class OrganizationServiceDataSeeder : ITransientDependency
{
    private readonly ILogger<OrganizationServiceDataSeeder> _logger;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public OrganizationServiceDataSeeder(
        ILogger<OrganizationServiceDataSeeder> logger,
        ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;
        _currentTenant = currentTenant;
    }

    public async Task SeedAsync(Guid? tenantId = null)
    {
        using (_currentTenant.Change(tenantId))
        {
            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                await uow.CompleteAsync();
            }
        }
    }
}

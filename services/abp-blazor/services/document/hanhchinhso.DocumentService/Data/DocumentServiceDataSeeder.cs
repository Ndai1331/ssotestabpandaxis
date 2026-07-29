using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Data;

public class DocumentServiceDataSeeder : ITransientDependency
{
    private readonly ILogger<DocumentServiceDataSeeder> _logger;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public DocumentServiceDataSeeder(
        ILogger<DocumentServiceDataSeeder> logger,
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

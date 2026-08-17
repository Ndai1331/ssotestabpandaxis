using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace hanhchinhso.IdentityService.Data;

public class IdentityServiceDataSeeder : ITransientDependency
{
    private readonly ILogger<IdentityServiceDataSeeder> _logger;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IIdentityDataSeeder _identityDataSeeder;
    private readonly OpenIddictDataSeeder _openIddictDataSeeder;
    private readonly IConfiguration _configuration;
    private readonly IdentityRoleManager _roleManager;

    public IdentityServiceDataSeeder(
        ILogger<IdentityServiceDataSeeder> logger,
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionDataSeeder permissionDataSeeder,
        ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWorkManager,
        IIdentityDataSeeder identityDataSeeder,
        OpenIddictDataSeeder openIddictDataSeeder,
        IConfiguration configuration,
        IdentityRoleManager roleManager)
    {
        _logger = logger;
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionDataSeeder = permissionDataSeeder;
        _currentTenant = currentTenant;
        _unitOfWorkManager = unitOfWorkManager;
        _identityDataSeeder = identityDataSeeder;
        _openIddictDataSeeder = openIddictDataSeeder;
        _configuration = configuration;
        _roleManager = roleManager;
    }

    public async Task SeedAsync(Guid? tenantId = null, string? adminEmail = null, string? adminPassword = null)
    {
        _logger.LogInformation("Seeding Permission data...");
        await SeedPermissionsAsync(tenantId);
            
        _logger.LogInformation("Seeding Identity data...");
        await SeedIdentityAsync(tenantId, adminEmail, adminPassword);

        if (tenantId == null)
        {
            _logger.LogInformation("Seeding OpenIddict data...");
            await SeedOpenIddictAsync();
        }
    }
    
    private async Task SeedPermissionsAsync(Guid? tenantId = null)
    {
        using (_currentTenant.Change(tenantId))
        {
            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                var multiTenancySide = tenantId == null
                    ? MultiTenancySides.Host
                    : MultiTenancySides.Tenant;

                var permissionNames = (await _permissionDefinitionManager.GetPermissionsAsync())
                    .Where(p => p.MultiTenancySide.HasFlag(multiTenancySide))
                    .Where(p => !p.Providers.Any() || p.Providers.Contains(RolePermissionValueProvider.ProviderName))
                    .Select(p => p.Name)
                    .ToArray();

                _logger.LogInformation($"Seeding admin permissions.");
                await _permissionDataSeeder.SeedAsync(
                    RolePermissionValueProvider.ProviderName,
                    "admin",
                    permissionNames,
                    tenantId
                );

                await uow.CompleteAsync();
            }
        }
    }
    
    private async Task SeedIdentityAsync(Guid? tenantId = null, string? adminEmail = null, string? adminPassword = null)
    {
        using (_currentTenant.Change(tenantId))
        {
            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                await _identityDataSeeder.SeedAsync(
                    adminEmail ?? _configuration["App:InitialAdminEmailAddress"],
                    adminPassword ?? _configuration["App:InitialAdminPassword"],
                    tenantId
                );

                // BD SSO lab roles (admin already seeded by IdentityDataSeeder)
                foreach (var roleName in new[] { "bacsi", "lanhdao", "nhanvien" })
                {
                    if (await _roleManager.FindByNameAsync(roleName) == null)
                    {
                        await _roleManager.CreateAsync(new IdentityRole(Guid.NewGuid(), roleName, tenantId)
                        {
                            IsPublic = true,
                            IsStatic = false
                        });
                        _logger.LogInformation("Seeded BD role {Role}", roleName);
                    }
                }

                await uow.CompleteAsync();
            }
        }
    }
    
    private async Task SeedOpenIddictAsync()
    {
        // It internally uses transaction. So, it's not necessary to use UnitOfWork.
        await _openIddictDataSeeder.SeedAsync();
    }
}
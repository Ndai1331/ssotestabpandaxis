using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace hanhchinhso.AdministrationService.Data;

public class AdministrationServiceDataSeeder : ITransientDependency
{
    private readonly ILogger<AdministrationServiceDataSeeder> _logger;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AdministrationServiceDataSeeder(
        ILogger<AdministrationServiceDataSeeder> logger,
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionDataSeeder permissionDataSeeder,
        ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _logger = logger;
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionDataSeeder = permissionDataSeeder;
        _currentTenant = currentTenant;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task SeedAsync(Guid? tenantId = null)
    {
        using (_currentTenant.Change(tenantId))
        {
            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                await SeedAdminPermissionsAsync(tenantId);
                await SeedBdRolePermissionsAsync(tenantId);
                await uow.CompleteAsync();
            }
        }
    }

    private async Task SeedAdminPermissionsAsync(Guid? tenantId)
    {
        _logger.LogInformation($"Seeding admin permissions.");

        var multiTenancySide = tenantId == null
            ? MultiTenancySides.Host
            : MultiTenancySides.Tenant;

        var permissionNames = (await _permissionDefinitionManager.GetPermissionsAsync())
            .Where(p => p.MultiTenancySide.HasFlag(multiTenancySide))
            .Where(p => !p.Providers.Any() || p.Providers.Contains(RolePermissionValueProvider.ProviderName))
            .Select(p => p.Name)
            .ToArray();

        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            "admin",
            permissionNames,
            tenantId
        );
    }

    /// <summary>
    /// Lab sample grants (Directus-like scopes): lanhdao=oversight, bacsi=scoped, nhanvien=basic.
    /// </summary>
    private async Task SeedBdRolePermissionsAsync(Guid? tenantId)
    {
        // Lãnh đạo — read / oversight (no create/update/delete users)
        await SeedRoleAsync(tenantId, "lanhdao",
        [
            "AdministrationService.Dashboard.Host",
            "AbpIdentity.Users",
            "AbpIdentity.Users.ViewDetails",
            "AbpIdentity.UserLookup",
            "AbpIdentity.Roles",
            "AbpIdentity.SecurityLogs",
            "AbpIdentity.Sessions",
            "AbpIdentity.OrganizationUnits",
            "AbpIdentity.OrganizationUnits.ManageOU",
            "AbpIdentity.OrganizationUnits.ManageMembers",
            "AuditLogging.AuditLogs",
            "HanhChinhSo.Organization.MasterData",
            "HanhChinhSo.Organization.Units",
            "HanhChinhSo.Organization.Positions",
            "HanhChinhSo.Document.Documents",
            "HanhChinhSo.Document.Files",
            "HanhChinhSo.Document.Files.Download",
        ]);

        // Bác sĩ — scoped ops
        await SeedRoleAsync(tenantId, "bacsi",
        [
            "AdministrationService.Dashboard.Host",
            "AbpIdentity.UserLookup",
            "AbpIdentity.Users.ViewDetails",
            "AbpIdentity.OrganizationUnits",
            "AIManagement.Workspaces",
            "AIManagement.Workspaces.Playground",
            "Volo.AIManagement.Workspaces.Workspace.Consume",
            "HanhChinhSo.Organization.MasterData",
            "HanhChinhSo.Organization.Units",
            "HanhChinhSo.Organization.Positions",
            "HanhChinhSo.Document.Documents",
            "HanhChinhSo.Document.Files",
            "HanhChinhSo.Document.Files.Download",
        ]);

        // Nhân viên — basic
        await SeedRoleAsync(tenantId, "nhanvien",
        [
            "AdministrationService.Dashboard.Host",
            "AbpIdentity.OrganizationUnits",
            "HanhChinhSo.Organization.MasterData",
            "HanhChinhSo.Organization.Units",
            "HanhChinhSo.Organization.Positions",
            "HanhChinhSo.Document.Documents",
            "HanhChinhSo.Document.Files",
            "HanhChinhSo.Document.Files.Download",
        ]);
    }

    private async Task SeedRoleAsync(Guid? tenantId, string roleName, string[] permissionNames)
    {
        _logger.LogInformation("Seeding BD role permissions for {Role} ({Count})", roleName, permissionNames.Length);
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            roleName,
            permissionNames,
            tenantId
        );
    }
}

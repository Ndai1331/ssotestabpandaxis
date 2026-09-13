using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;

namespace HCS.OrganizationUnits;
[Authorize(HCSOrganizationPermissions.Departments)]
public partial class OrganizationUnitManagementAppService : HCSAppService, IOrganizationUnitManagementAppService
{
    private const int MaxTreeItems = 5000;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MaxFilterLength = 256;

    private readonly IOrganizationUnitRepository organizationUnitRepository;
    private readonly OrganizationUnitManager organizationUnitManager;
    private readonly IdentityUserManager identityUserManager;

    public OrganizationUnitManagementAppService(
        IOrganizationUnitRepository organizationUnitRepository,
        OrganizationUnitManager organizationUnitManager,
        IdentityUserManager identityUserManager)
    {
        this.organizationUnitRepository = organizationUnitRepository;
        this.organizationUnitManager = organizationUnitManager;
        this.identityUserManager = identityUserManager;
    }

    public async Task<List<OrganizationUnitDto>> GetListAsync()
    {
        var units = await organizationUnitRepository.GetListAsync(
            nameof(OrganizationUnit.Code), MaxTreeItems, 0, includeDetails: false);

        return units.Select(Map).ToList();
    }
    public async Task<PagedResultDto<OrganizationUnitMemberDto>> GetMembersAsync(
        Guid id,
        GetOrganizationUnitMembersInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var unit = await organizationUnitRepository.GetAsync(id);
        var filter = NormalizeFilter(input.Filter);
        var totalCount = await organizationUnitRepository.GetMembersCountAsync(unit, filter);
        var pageSize = input.MaxResultCount <= 0
            ? DefaultPageSize
            : Math.Min(input.MaxResultCount, MaxPageSize);
        var skipCount = Math.Max(0, input.SkipCount);
        var members = await organizationUnitRepository.GetMembersAsync(
            unit,
            nameof(IdentityUser.UserName),
            pageSize,
            skipCount,
            filter,
            includeChildren: false,
            includeDetails: false);

        return new PagedResultDto<OrganizationUnitMemberDto>(
            totalCount,
            members.Select(Map).ToList());
    }
    public async Task<PagedResultDto<OrganizationUnitMemberDto>> GetAvailableMembersAsync(
        Guid id,
        GetOrganizationUnitMembersInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var unit = await organizationUnitRepository.GetAsync(id);
        var filter = NormalizeFilter(input.Filter);
        var totalCount = await organizationUnitRepository.GetUnaddedUsersCountAsync(unit, filter);
        var pageSize = input.MaxResultCount <= 0
            ? DefaultPageSize
            : Math.Min(input.MaxResultCount, MaxPageSize);
        var skipCount = Math.Max(0, input.SkipCount);
        var users = await organizationUnitRepository.GetUnaddedUsersAsync(
            unit,
            nameof(IdentityUser.UserName),
            pageSize,
            skipCount,
            filter,
            includeDetails: false);

        return new PagedResultDto<OrganizationUnitMemberDto>(
            totalCount,
            users.Select(Map).ToList());
}
    [Authorize(HCSOrganizationPermissions.Departments + HcsCrudPermissions.CreateSuffix)]
    public async Task<OrganizationUnitDto> CreateAsync(CreateOrganizationUnitInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var displayName = NormalizeDisplayName(input.DisplayName);
        var unit = new OrganizationUnit(
            GuidGenerator.Create(),
            displayName,
            input.ParentId,
            CurrentTenant.Id);

        await organizationUnitManager.CreateAsync(unit);
        return Map(unit);
    }

    [Authorize(HCSOrganizationPermissions.Departments + HcsCrudPermissions.UpdateSuffix)]
    public async Task<OrganizationUnitDto> UpdateAsync(Guid id, UpdateOrganizationUnitInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var unit = await organizationUnitRepository.GetAsync(id);
        unit.DisplayName = NormalizeDisplayName(input.DisplayName);
        await organizationUnitManager.UpdateAsync(unit);
        return Map(unit);
    }

    [Authorize(HCSOrganizationPermissions.Departments + HcsCrudPermissions.UpdateSuffix)]
    public async Task<OrganizationUnitDto> MoveAsync(Guid id, MoveOrganizationUnitInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var unit = await organizationUnitRepository.GetAsync(id);
        if (unit.ParentId == input.ParentId)
        {
            return Map(unit);
        }

        await EnsureValidParentAsync(id, input.ParentId);
        await organizationUnitManager.MoveAsync(id, input.ParentId);
        return Map(await organizationUnitRepository.GetAsync(id));
    }

    [Authorize(HCSOrganizationPermissions.Departments + HcsCrudPermissions.DeleteSuffix)]
    public async Task DeleteAsync(Guid id)
    {
        await organizationUnitManager.DeleteAsync(id);
    }

    [Authorize(HCSOrganizationPermissions.Departments + HcsCrudPermissions.UpdateSuffix)]
    public async Task MoveAllMembersAsync(Guid id, MoveAllOrganizationUnitMembersInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (id == input.TargetOrganizationUnitId)
        {
            throw new BusinessException("HCS:OrganizationUnit.SameTarget");
        }

        await organizationUnitRepository.GetAsync(id);
        await organizationUnitRepository.GetAsync(input.TargetOrganizationUnitId);
        await identityUserManager.UpdateOrganizationAsync(id, input.TargetOrganizationUnitId);
    }

    private async Task EnsureValidParentAsync(Guid id, Guid? parentId)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        if (id == parentId.Value)
        {
            throw new BusinessException("HCS:OrganizationUnit.SameTarget");
        }

        var descendants = await organizationUnitManager.FindChildrenAsync(id, recursive: true);
        if (descendants.Any(unit => unit.Id == parentId.Value))
        {
            throw new BusinessException("HCS:OrganizationUnit.InvalidParent");
        }
    }

    private static string NormalizeDisplayName(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException("HCS:OrganizationUnit.DisplayNameRequired");
        }

        return normalized;
    }

    private static string? NormalizeFilter(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized[..Math.Min(normalized.Length, MaxFilterLength)];
    }

}

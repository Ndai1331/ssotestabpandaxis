using System;
using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace HCS.OrganizationUnits;

public partial class OrganizationUnitManagementAppService
{
    [Authorize(HCSOrganizationPermissions.Departments + HcsCrudPermissions.UpdateSuffix)]
    public async Task AddMemberAsync(Guid id, AddOrganizationUnitMemberInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        await organizationUnitRepository.GetAsync(id);
        await identityUserManager.AddToOrganizationUnitAsync(input.UserId, id);
    }

    [Authorize(HCSOrganizationPermissions.Departments + HcsCrudPermissions.UpdateSuffix)]
    public async Task RemoveMemberAsync(Guid id, Guid userId)
    {
        await organizationUnitRepository.GetAsync(id);
        await identityUserManager.RemoveFromOrganizationUnitAsync(userId, id);
    }
}

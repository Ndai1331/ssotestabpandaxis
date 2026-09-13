using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace HCS.OrganizationUnits;

public interface IOrganizationUnitManagementAppService : IApplicationService
{
    Task<List<OrganizationUnitDto>> GetListAsync();

    Task<PagedResultDto<OrganizationUnitMemberDto>> GetMembersAsync(
        Guid id,
        GetOrganizationUnitMembersInput input);

    Task<PagedResultDto<OrganizationUnitMemberDto>> GetAvailableMembersAsync(
        Guid id,
        GetOrganizationUnitMembersInput input);

    Task<OrganizationUnitDto> CreateAsync(CreateOrganizationUnitInput input);

    Task<OrganizationUnitDto> UpdateAsync(Guid id, UpdateOrganizationUnitInput input);

    Task<OrganizationUnitDto> MoveAsync(Guid id, MoveOrganizationUnitInput input);

    Task DeleteAsync(Guid id);

    Task MoveAllMembersAsync(Guid id, MoveAllOrganizationUnitMembersInput input);

    Task AddMemberAsync(Guid id, AddOrganizationUnitMemberInput input);

    Task RemoveMemberAsync(Guid id, Guid userId);
}

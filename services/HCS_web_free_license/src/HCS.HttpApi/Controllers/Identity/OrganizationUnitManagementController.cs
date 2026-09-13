using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCS.OrganizationUnits;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace HCS.Controllers.Identity;

[ApiController]
[Authorize(HCSOrganizationPermissions.Departments)]
[Route("api/identity/organization-units")]
public sealed class OrganizationUnitManagementController : HCSController
{
    private readonly IOrganizationUnitManagementAppService service;

    public OrganizationUnitManagementController(IOrganizationUnitManagementAppService service)
    {
        this.service = service;
    }

    [HttpGet]
    public Task<List<OrganizationUnitDto>> GetListAsync() => service.GetListAsync();

    [HttpGet("{id:guid}/members")]
    public Task<PagedResultDto<OrganizationUnitMemberDto>> GetMembersAsync(
        Guid id,
        [FromQuery] GetOrganizationUnitMembersInput input) => service.GetMembersAsync(id, input);

    [HttpGet("{id:guid}/available-members")]
    public Task<PagedResultDto<OrganizationUnitMemberDto>> GetAvailableMembersAsync(
        Guid id,
        [FromQuery] GetOrganizationUnitMembersInput input) => service.GetAvailableMembersAsync(id, input);

    [HttpPost]
    public Task<OrganizationUnitDto> CreateAsync([FromBody] CreateOrganizationUnitInput input) => service.CreateAsync(input);

    [HttpPut("{id:guid}")]
    public Task<OrganizationUnitDto> UpdateAsync(Guid id, [FromBody] UpdateOrganizationUnitInput input) => service.UpdateAsync(id, input);

    [HttpPost("{id:guid}/move")]
    public Task<OrganizationUnitDto> MoveAsync(Guid id, [FromBody] MoveOrganizationUnitInput input) => service.MoveAsync(id, input);

    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id) => service.DeleteAsync(id);

    [HttpPost("{id:guid}/move-all-members")]
    public Task MoveAllMembersAsync(Guid id, [FromBody] MoveAllOrganizationUnitMembersInput input) => service.MoveAllMembersAsync(id, input);

    [HttpPost("{id:guid}/members")]
    public Task AddMemberAsync(Guid id, [FromBody] AddOrganizationUnitMemberInput input) => service.AddMemberAsync(id, input);

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public Task RemoveMemberAsync(Guid id, Guid userId) => service.RemoveMemberAsync(id, userId);
}

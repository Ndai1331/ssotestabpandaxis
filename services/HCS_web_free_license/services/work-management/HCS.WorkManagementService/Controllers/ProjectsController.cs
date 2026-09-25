using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCS.WorkManagementService.Controllers;

[ApiController, Authorize(Policy = WorkPermissions.ProjectsRead), Route("api/projects")]
public sealed class ProjectsController(ProjectAppService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedWorkDto<ProjectDto>> GetList(string? filter, string? status, int skip = 0, int take = 20, CancellationToken ct = default) =>
        service.GetListAsync(filter, status, skip, take, ct);

    [HttpGet("{id:guid}")]
    public Task<ProjectDetailDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpGet("next-code")]
    public Task<NextCodeDto> GetNextCode(CancellationToken ct) => service.GetNextCodeAsync(ct);

    [HttpPost, Authorize(Policy = WorkPermissions.Projects)]
    public Task<ProjectDto> Create(CreateProjectDto input, CancellationToken ct) => service.CreateAsync(input, ct);

    [HttpPut("{id:guid}"), Authorize(Policy = WorkPermissions.Projects)]
    public Task<ProjectDto> Update(Guid id, UpdateProjectDto input, CancellationToken ct) => service.UpdateAsync(id, input, ct);

    [HttpDelete("{id:guid}"), Authorize(Policy = WorkPermissions.Projects)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/chat-access"), Authorize(Policy = WorkPermissions.Projects)]
    public async Task<IActionResult> SyncChatAccess(Guid id, CancellationToken ct)
    {
        await service.SyncChatAccessAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/members"), Authorize(Policy = WorkPermissions.Projects)]
    public Task<ProjectMemberDto> AddMember(Guid id, AddProjectMemberDto input, CancellationToken ct) =>
        service.AddMemberAsync(id, input, ct);

    [HttpDelete("{id:guid}/members/{memberId:guid}"), Authorize(Policy = WorkPermissions.Projects)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId, CancellationToken ct)
    {
        await service.RemoveMemberAsync(id, memberId, ct);
        return NoContent();
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HCS.OrganizationUnits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;

namespace HCS.Controllers.Identity;

[ApiController]
[Authorize]
[Route("api/identity/organization-unit-lookup")]
public sealed class OrganizationUnitLookupController(IOrganizationUnitLookupAppService service) : HCSController
{
    [HttpGet]
    public Task<List<OrganizationUnitDto>> GetListAsync() => service.GetListAsync();

    [HttpGet("users")]
    public Task<IReadOnlyList<UserOrganizationUnitLookupDto>> GetUsersAsync(
        [FromQuery] Guid[]? userIds,
        CancellationToken cancellationToken) =>
        service.GetUsersAsync(userIds ?? [], cancellationToken);

    [HttpGet("{id:guid}/members")]
    public Task<IReadOnlyList<UserOrganizationUnitLookupDto>> GetMembersAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        service.GetMembersAsync(id, cancellationToken);

    [HttpPut("users/{userId:guid}")]
    [Authorize(IdentityPermissions.Users.Update)]
    public Task SetUserAsync(Guid userId, [FromBody] SetUserOrganizationUnitsInput input) =>
        service.SetUserAsync(userId, input);
}

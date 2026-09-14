using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HCS.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;

namespace HCS.Controllers.Identity;

[ApiController]
[Authorize(IdentityPermissions.Users.Default)]
[Route("api/identity/user-roles")]
public sealed class UserRoleLookupController(IUserRoleLookupAppService service) : HCSController
{
    [HttpGet]
    public Task<IReadOnlyList<UserRoleLookupDto>> List(
        [FromQuery] Guid[]? userIds,
        CancellationToken cancellationToken) =>
        service.GetUserRolesAsync(userIds ?? [], cancellationToken);
}

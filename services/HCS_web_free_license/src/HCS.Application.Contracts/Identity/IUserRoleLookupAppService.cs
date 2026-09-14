using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace HCS.Identity;

public interface IUserRoleLookupAppService : IApplicationService
{
    Task<IReadOnlyList<UserRoleLookupDto>> GetUserRolesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}

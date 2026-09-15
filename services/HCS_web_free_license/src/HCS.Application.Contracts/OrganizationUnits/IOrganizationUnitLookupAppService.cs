using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace HCS.OrganizationUnits;

public interface IOrganizationUnitLookupAppService : IApplicationService
{
    Task<List<OrganizationUnitDto>> GetListAsync();

    Task<IReadOnlyList<UserOrganizationUnitLookupDto>> GetUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOrganizationUnitLookupDto>> GetMembersAsync(
        Guid organizationUnitId,
        CancellationToken cancellationToken = default);

    Task SetUserAsync(Guid userId, SetUserOrganizationUnitsInput input);
}

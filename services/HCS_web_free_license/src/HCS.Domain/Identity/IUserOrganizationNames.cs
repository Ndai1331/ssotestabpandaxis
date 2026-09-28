using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HCS.Identity;

public interface IUserOrganizationNames
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}

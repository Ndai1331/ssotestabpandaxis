using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HCS.Logging;

public interface IServiceLogQuery
{
    Task<IReadOnlyList<ServiceLogDto>> GetEventsAsync(
        string filter,
        int count,
        string? afterId,
        CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace HCS.DocumentService;

internal static class DocumentTransaction
{
    // ABP owns request transactions. Only return a transaction when this call
    // creates it, so callers never commit or dispose the surrounding unit of work.
    internal static async Task<IDbContextTransaction?> BeginIfNeededAsync(
        DatabaseFacade database, CancellationToken cancellationToken = default) =>
        database.CurrentTransaction is null
            ? await database.BeginTransactionAsync(cancellationToken)
            : null;
}

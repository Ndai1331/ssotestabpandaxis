using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HCS.CollaborationService.Application;

/// <summary>
/// Serializes one user's reaction changes for the same target across service instances.
/// PostgreSQL advisory locks avoid a read-then-insert race without adding a lock column.
/// </summary>
internal sealed class SocialReactionTransaction : IAsyncDisposable
{
    private readonly IDbContextTransaction? ownedTransaction;

    private SocialReactionTransaction(IDbContextTransaction? ownedTransaction) =>
        this.ownedTransaction = ownedTransaction;

    public static async Task<SocialReactionTransaction> BeginAsync(DbContext db, string targetType,
        Guid targetId, Guid userId, CancellationToken ct)
    {
        var transaction = db.Database.CurrentTransaction;
        var ownsTransaction = transaction is null;
        if (ownsTransaction)
            transaction = await db.Database.BeginTransactionAsync(ct);

        var lockKey = $"social:{targetType}:{targetId:N}:{userId:N}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));", ct);
        return new SocialReactionTransaction(ownsTransaction ? transaction : null);
    }

    public Task CommitAsync(CancellationToken ct) => ownedTransaction?.CommitAsync(ct) ?? Task.CompletedTask;

    public ValueTask DisposeAsync() => ownedTransaction?.DisposeAsync() ?? ValueTask.CompletedTask;
}

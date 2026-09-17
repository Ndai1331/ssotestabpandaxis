using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace HCS.DocumentService.Tests;

public sealed class DocumentTransactionTests
{
    [Fact]
    public async Task Existing_request_transaction_is_not_started_committed_or_disposed()
    {
        using var context = new DbContext(new DbContextOptions<DbContext>());
        var existing = new TestTransaction();
        var database = new TestDatabase(context, existing);
        await using (var owned = await DocumentTransaction.BeginIfNeededAsync(database))
        {
            Assert.Null(owned);
            if (owned is not null) await owned.CommitAsync();
        }
        Assert.Equal(0, database.BeginCount);
        Assert.False(existing.Committed);
        Assert.False(existing.Disposed);
    }

    [Fact]
    public async Task Standalone_call_owns_the_new_transaction()
    {
        using var context = new DbContext(new DbContextOptions<DbContext>());
        var database = new TestDatabase(context, null);
        await using (var owned = await DocumentTransaction.BeginIfNeededAsync(database))
        {
            Assert.Same(database.Created, owned);
            await owned!.CommitAsync();
        }
        Assert.Equal(1, database.BeginCount);
        Assert.True(database.Created.Committed);
        Assert.True(database.Created.Disposed);
    }

    private sealed class TestDatabase(DbContext context, IDbContextTransaction? existing) : DatabaseFacade(context)
    {
        public int BeginCount { get; private set; }
        public TestTransaction Created { get; } = new();
        public override IDbContextTransaction? CurrentTransaction => existing;
        public override Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            BeginCount++;
            if (existing is not null) throw new InvalidOperationException("Nested transaction.");
            return Task.FromResult<IDbContextTransaction>(Created);
        }
    }

    private sealed class TestTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public bool Committed { get; private set; }
        public bool Disposed { get; private set; }
        public void Commit() => Committed = true;
        public Task CommitAsync(CancellationToken cancellationToken = default) { Commit(); return Task.CompletedTask; }
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}

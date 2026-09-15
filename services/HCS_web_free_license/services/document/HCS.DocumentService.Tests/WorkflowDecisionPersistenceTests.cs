using HCS.DocumentService.Workflows;
using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Tests;

public sealed class WorkflowDecisionPersistenceTests
{
    private static readonly DateTime Now = new(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Decide_tracks_the_next_approval_task_as_added()
    {
        await using var db = CreateDb();
        var definition = new WorkflowDefinition(Guid.NewGuid(), "standard", "Standard",
        [
            new WorkflowStepInput("sign", "Sign", 1, "Documents.Approve", "SIGN"),
            new WorkflowStepInput("approve", "Approve", 2, "Documents.Approve")
        ], Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start-1", Now);
        db.WorkflowDefinitions.Add(definition);
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await db.WorkflowInstances.Include(x => x.Tasks).SingleAsync(x => x.Id == instance.Id);
        var existingTaskIds = loaded.Tasks.Select(x => x.Id).ToHashSet();
        Assert.True(loaded.Decide(loaded.Tasks.Single().Id, true, Guid.NewGuid(), null, "decision-1",
            definition.Steps.OrderBy(x => x.Order).ToList(), Now));
        var nextTask = loaded.Tasks.Single(x => !existingTaskIds.Contains(x.Id));

        WorkflowAppService.TrackNewApprovalTasks(db, loaded, existingTaskIds);
        db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Added, db.Entry(nextTask).State);
        await db.SaveChangesAsync();

        var saved = await db.ApprovalTasks.AsNoTracking().Where(x => x.InstanceId == instance.Id).ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.Contains(saved, task => task.Id == nextTask.Id && task.Status == ApprovalTaskStatus.Pending);
    }

    private static DocumentServiceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new DocumentServiceDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}

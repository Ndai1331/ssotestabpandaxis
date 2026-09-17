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

    [Fact]
    public async Task Submission_assignees_survive_reload_step_changes_and_resubmission()
    {
        await using var db = CreateDb();
        var firstSigner = Guid.NewGuid();
        var secondSigner = Guid.NewGuid();
        var finalSigner = Guid.NewGuid();
        var definition = new WorkflowDefinition(Guid.NewGuid(), "selected", "Selected signers",
        [
            new WorkflowStepInput("view", "View", 1, "Documents.Approve", "VIEW"),
            new WorkflowStepInput("sign1", "First", 2, "Documents.Approve", "SIGN"),
            new WorkflowStepInput("sign2", "Second", 3, "Documents.Approve", "SIGN", AllowReturn: true),
            new WorkflowStepInput("sign3", "Final", 4, "Documents.Approve", "SIGN", finalSigner)
        ], Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "selected-start", Now,
            new Dictionary<string, Guid> { ["SIGN1"] = firstSigner, ["SIGN2"] = secondSigner });
        db.WorkflowDefinitions.Add(definition);
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await db.WorkflowInstances.Include(x => x.Tasks).SingleAsync();
        var steps = definition.Steps.OrderBy(x => x.Order).ToList();
        var initialIds = loaded.Tasks.Select(x => x.Id).ToHashSet();
        var first = Assert.Single(loaded.Tasks);
        Assert.Equal(firstSigner, first.AssigneeUserId);
        loaded.Decide(first.Id, true, firstSigner, null, "first", steps, Now);
        WorkflowAppService.TrackNewApprovalTasks(db, loaded, initialIds);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        loaded = await db.WorkflowInstances.Include(x => x.Tasks).SingleAsync();
        var second = loaded.Tasks.Single(x => x.Status == ApprovalTaskStatus.Pending);
        Assert.Equal(secondSigner, second.AssigneeUserId);
        loaded.Decide(second.Id, false, secondSigner, null, "return", steps, Now, returnStep: true);
        loaded.Resubmit(steps, Now, "resubmit");
        var restarted = loaded.Tasks.Single(x => x.Status == ApprovalTaskStatus.Pending);
        Assert.Equal(firstSigner, restarted.AssigneeUserId);
        loaded.Decide(restarted.Id, true, firstSigner, null, "first-again", steps, Now);
        var secondAgain = loaded.Tasks.Single(x => x.Status == ApprovalTaskStatus.Pending);
        Assert.Equal(secondSigner, secondAgain.AssigneeUserId);
        loaded.Decide(secondAgain.Id, true, secondSigner, null, "second", steps, Now);
        Assert.Equal(finalSigner, loaded.Tasks.Single(x => x.Status == ApprovalTaskStatus.Pending).AssigneeUserId);
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

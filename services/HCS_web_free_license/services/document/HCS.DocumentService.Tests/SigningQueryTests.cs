using HCS.DocumentService.Signing;
using HCS.DocumentService.Workflows;
using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Tests;

public sealed class SigningQueryTests
{
    private static readonly DateTime Now = new(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(20, 1)]
    [InlineData(50, 2)]
    public void Signature_slots_count_only_sign_steps_in_the_current_workflow(int order, int expected)
    {
        var definition = new WorkflowDefinition(Guid.NewGuid(), "mixed", "Mixed",
        [
            new WorkflowStepInput("process", "Process", 10, "Documents.Approve", "PROCESS"),
            new WorkflowStepInput("sign1", "Sign first", 20, "Documents.Approve", "SIGN"),
            new WorkflowStepInput("view", "View", 30, "Documents.Approve", "VIEW"),
            new WorkflowStepInput("process2", "Process again", 40, "Documents.Approve", "PROCESS"),
            new WorkflowStepInput("sign2", "Sign second", 50, "Documents.Approve", "SIGN")
        ], Now);
        var (_, otherDefinition) = StartSignedWorkflow(Guid.NewGuid());
        var steps = definition.Steps.Concat(otherDefinition.Steps).Reverse().AsQueryable();

        Assert.Equal(expected,
            SigningAppService.QuerySigningStepsThrough(steps, definition.Id, order).Count());
    }

    [Fact]
    public void Paired_file_lookup_is_translatable_by_postgresql()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=hcs").Options;
        using var db = new DocumentServiceDbContext(options);

        var sql = SigningAppService.QueryPairedFile(db.DocumentFiles, Guid.NewGuid(), Guid.NewGuid())
            .ToQueryString();

        Assert.Contains("\"DocumentFiles\"", sql);
        Assert.Contains("\"IsPendingDeletion\"", sql);
    }

    [Fact]
    public void Queue_keeps_the_assignee_processed_sign_task()
    {
        var assignee = Guid.NewGuid();
        var (instance, definition) = StartSignedWorkflow(assignee);
        var task = instance.Tasks.Single();
        Assert.True(instance.Decide(task.Id, true, assignee, null, "decision-1",
            definition.Steps.OrderBy(x => x.Order).ToList(), Now));

        var selected = SigningAppService.SelectQueueTasks(instance.Tasks, definition.Steps, assignee, Guid.NewGuid());

        var kept = Assert.Single(selected);
        Assert.Equal(task.Id, kept.Id);
        Assert.Equal(ApprovalTaskStatus.Approved, kept.Status);
    }

    [Fact]
    public void Queue_keeps_completed_items_for_the_submitter()
    {
        var assignee = Guid.NewGuid();
        var submitter = Guid.NewGuid();
        var (instance, definition) = StartSignedWorkflow(assignee);
        Assert.True(instance.Decide(instance.Tasks.Single().Id, true, assignee, null, "decision-1",
            definition.Steps.OrderBy(x => x.Order).ToList(), Now));

        var selected = SigningAppService.SelectQueueTasks(instance.Tasks, definition.Steps, submitter, submitter);

        Assert.Equal(ApprovalTaskStatus.Approved, Assert.Single(selected).Status);
        Assert.Empty(SigningAppService.SelectQueueTasks(instance.Tasks, definition.Steps, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Queue_still_includes_pending_action_tasks()
    {
        var assignee = Guid.NewGuid();
        var (instance, definition) = StartSignedWorkflow(assignee);

        var selected = SigningAppService.SelectQueueTasks(instance.Tasks, definition.Steps, Guid.NewGuid(), null);

        Assert.Equal(ApprovalTaskStatus.Pending, Assert.Single(selected).Status);
    }

    private static (WorkflowInstance Instance, WorkflowDefinition Definition) StartSignedWorkflow(Guid assignee)
    {
        var definition = new WorkflowDefinition(Guid.NewGuid(), "sign", "Sign",
        [
            new WorkflowStepInput("sign", "Sign", 1, "Documents.Approve", "SIGN", assignee)
        ], Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start-sign", Now);
        return (instance, definition);
    }
}

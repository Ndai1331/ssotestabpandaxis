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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Queue_returns_one_current_row_for_a_submission_with_multiple_sign_steps(bool completed)
    {
        var firstSigner = Guid.NewGuid();
        var nextSigner = Guid.NewGuid();
        var submitter = Guid.NewGuid();
        var definition = new WorkflowDefinition(Guid.NewGuid(), "multi", "Multi",
        [
            new WorkflowStepInput("view", "View", 1, "Documents.Approve", "VIEW", submitter),
            new WorkflowStepInput("sign1", "Sign first", 2, "Documents.Approve", "SIGN", firstSigner),
            new WorkflowStepInput("sign2", "Sign second", 3, "Documents.Approve", "SIGN", nextSigner),
            new WorkflowStepInput("sign3", "Sign third", 4, "Documents.Approve", "SIGN", nextSigner)
        ], Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start", Now);
        var steps = definition.Steps.OrderBy(step => step.Order).ToList();
        // Equal timestamps must still select the last step consistently.
        for (var index = 0; index < (completed ? 3 : 1); index++)
        {
            var pending = instance.Tasks.Single(task => task.Status == ApprovalTaskStatus.Pending
                && task.StepCode.StartsWith("sign"));
            Assert.True(instance.Decide(pending.Id, true, pending.AssigneeUserId!.Value,
                null, $"decision-{index}", steps, Now));
        }

        foreach (var viewer in new[] { firstSigner, nextSigner, submitter })
        {
            var row = Assert.Single(SigningAppService.SelectQueueTasks(
                instance.Tasks.Reverse(), definition.Steps, viewer, submitter));
            Assert.Equal(completed ? "sign3" : "sign2", row.StepCode);
            Assert.Equal(completed ? ApprovalTaskStatus.Approved : ApprovalTaskStatus.Pending, row.Status);
        }
        Assert.Equal(completed ? 3 : 2, instance.Tasks.Count(task => task.StepCode.StartsWith("sign")));
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

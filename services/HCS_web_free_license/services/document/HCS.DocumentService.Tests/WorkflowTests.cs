using HCS.DocumentService.Workflows;

namespace HCS.DocumentService.Tests;

public sealed class WorkflowTests
{
    private static readonly DateTime Now = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Definition_rejects_duplicate_steps()
    {
        var steps = new[] { new WorkflowStepInput("review", "Review", 1, "Documents.Review"), new WorkflowStepInput("review", "Again", 2, "Documents.Approve") };
        Assert.Throws<InvalidOperationException>(() => new WorkflowDefinition(Guid.NewGuid(), "standard", "Standard", steps, Now));
    }

    [Fact]
    public void Decision_is_idempotent_and_completes_in_order()
    {
        var definition = new WorkflowDefinition(Guid.NewGuid(), "standard", "Standard", new[]
        {
            new WorkflowStepInput("review", "Review", 1, "Documents.Review"),
            new WorkflowStepInput("approve", "Approve", 2, "Documents.Approve")
        }, Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start-1", Now);
        var steps = definition.Steps.OrderBy(x => x.Order).ToList();
        var firstTask = instance.Tasks.Single();
        Assert.True(instance.Decide(firstTask.Id, true, Guid.NewGuid(), null, "decision-1", steps, Now));
        Assert.False(instance.Decide(firstTask.Id, true, Guid.NewGuid(), null, "decision-1", steps, Now));
        Assert.Equal(WorkflowInstanceStatus.Running, instance.Status);
        var secondTask = instance.Tasks.Single(x => x.Status == ApprovalTaskStatus.Pending);
        Assert.True(instance.Decide(secondTask.Id, true, Guid.NewGuid(), null, "decision-2", steps, Now));
        Assert.Equal(WorkflowInstanceStatus.Completed, instance.Status);
    }

    [Fact]
    public void Replace_steps_keeps_unique_codes_and_orders()
    {
        var definition = new WorkflowDefinition(Guid.NewGuid(), "standard", "Standard", new[]
        {
            new WorkflowStepInput("review", "Review", 1, "Documents.Review")
        }, Now);
        definition.Rename("Updated");
        definition.ReplaceSteps(new[]
        {
            new WorkflowStepInput("review", "Review", 1, "Documents.Review"),
            new WorkflowStepInput("approve", "Approve", 2, "Documents.Approve")
        });
        Assert.Equal("Updated", definition.Name);
        Assert.Equal(2, definition.Steps.Count);
        Assert.Throws<InvalidOperationException>(() => definition.ReplaceSteps(new[]
        {
            new WorkflowStepInput("same", "A", 1, "Documents.Review"),
            new WorkflowStepInput("same", "B", 2, "Documents.Approve")
        }));
    }

    [Fact]
    public void Template_can_be_deactivated()
    {
        var template = new WorkflowTemplate(Guid.NewGuid(), "incoming", "Incoming", Guid.NewGuid(), 1, "{}", Now);
        template.SetActive(false);
        Assert.False(template.IsActive);
    }

    [Fact]
    public void Template_requires_valid_json_and_positive_version()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => new WorkflowTemplate(Guid.NewGuid(), "incoming", "Incoming", Guid.NewGuid(), 1, "not-json", Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkflowTemplate(Guid.NewGuid(), "incoming", "Incoming", Guid.NewGuid(), 0, "{}", Now));
        var template = new WorkflowTemplate(Guid.NewGuid(), "incoming", "Incoming", Guid.NewGuid(), 1, "{}", Now);
        Assert.True(template.IsActive);
    }

    [Fact]
    public void Completed_single_step_workflow_accepts_same_decision_retry()
    {
        var definition = new WorkflowDefinition(Guid.NewGuid(), "one", "One", new[]
            { new WorkflowStepInput("approve", "Approve", 1, "Documents.Approve") }, Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start-one", Now);
        var task = instance.Tasks.Single();
        var actor = Guid.NewGuid();
        Assert.True(instance.Decide(task.Id, true, actor, null, "same-command", definition.Steps.ToList(), Now));
        Assert.Equal(WorkflowInstanceStatus.Completed, instance.Status);
        Assert.False(instance.Decide(task.Id, true, actor, null, "same-command", definition.Steps.ToList(), Now));
    }
}

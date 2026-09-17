using HCS.DocumentService.Documents;
using HCS.DocumentService.Workflows;

namespace HCS.DocumentService.Tests;

public sealed class WorkflowSubmissionDeletionTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void Only_owner_can_delete_an_untouched_submission(
        bool owner, bool signingActivity, bool expected)
    {
        var (instance, _) = Create();
        var submitter = Guid.NewGuid();
        Assert.Equal(expected, WorkflowSubmissionDeletion.CanDelete(DocumentSourceType.Workflow,
            submitter, owner ? submitter : Guid.NewGuid(), [instance], signingActivity));
        Assert.False(WorkflowSubmissionDeletion.CanDelete(DocumentSourceType.Archive,
            submitter, submitter, [instance], false));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void A_decision_blocks_deletion_even_after_resubmission(bool approve, bool returned)
    {
        var (instance, definition) = Create();
        var user = Guid.NewGuid();
        var steps = definition.Steps.OrderBy(x => x.Order).ToList();
        instance.Decide(instance.Tasks.Single().Id, approve, user, null, "decision", steps,
            DateTime.UtcNow, returnStep: returned);
        if (returned) instance.Resubmit(steps, DateTime.UtcNow, "resubmit");
        Assert.False(WorkflowSubmissionDeletion.CanDelete(DocumentSourceType.Workflow,
            user, user, [instance], false));
    }

    private static (WorkflowInstance, WorkflowDefinition) Create()
    {
        var definition = new WorkflowDefinition(Guid.NewGuid(), "test", "Test",
        [
            new WorkflowStepInput("view", "View", 1, "Documents.Approve", "VIEW"),
            new WorkflowStepInput("first", "First", 2, "Documents.Approve", "SIGN", AllowReturn: true),
            new WorkflowStepInput("second", "Second", 3, "Documents.Approve", "SIGN")
        ], DateTime.UtcNow);
        return (new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start", DateTime.UtcNow), definition);
    }
}

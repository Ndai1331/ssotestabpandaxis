using HCS.DocumentService.Documents;
using HCS.DocumentService.Signing;
using HCS.DocumentService.Workflows;
using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Tests;

public sealed class MobileApiGapTests
{
    private static readonly DateTime Now = new(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Decided_tasks_only_include_the_current_users_decisions_in_range()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        await using var db = CreateDb();
        var (definition, mine) = await StartAsync(db, me, other);
        var (_, theirs) = await StartAsync(db, other, me, definition);
        await DecideFirstAsync(db, mine, definition, me, Now.AddHours(1));
        await DecideFirstAsync(db, theirs, definition, other, Now.AddHours(1));

        var all = await SigningAppService.QueryDecidedTasks(db, me, null, null).ToListAsync();
        var task = Assert.Single(all);
        Assert.Equal(me, task.DecidedBy);
        Assert.Equal(ApprovalTaskStatus.Approved, task.Status);
        Assert.Empty(await SigningAppService.QueryDecidedTasks(db, me, Now.AddDays(1), null).ToListAsync());
        Assert.Empty(await SigningAppService.QueryDecidedTasks(db, me, null, Now).ToListAsync());
    }

    [Fact]
    public async Task Pending_action_tasks_only_include_tasks_assigned_to_the_current_user()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        await using var db = CreateDb();
        var (definition, mine) = await StartAsync(db, me, other);
        await StartAsync(db, other, me, definition);

        var pending = await SigningAppService.QueryPendingActionTasks(db, me).ToListAsync();
        var task = Assert.Single(pending);
        Assert.Equal(mine.Id, task.InstanceId);

        await DecideFirstAsync(db, mine, definition, me, Now.AddHours(1));
        Assert.Empty(await SigningAppService.QueryPendingActionTasks(db, me)
            .Where(x => x.InstanceId == mine.Id).ToListAsync());
    }

    [Fact]
    public void History_and_stats_queries_translate_to_postgresql()
    {
        var options = new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=hcs").Options;
        using var db = new DocumentServiceDbContext(options);

        Assert.Contains("DecidedBy", SigningAppService.QueryDecidedTasks(db, Guid.NewGuid(), Now, Now.AddDays(1)).ToQueryString());
        Assert.Contains("AssigneeUserId", SigningAppService.QueryPendingActionTasks(db, Guid.NewGuid()).ToQueryString());
    }

    [Theory]
    [InlineData(ApprovalTaskStatus.Approved, true)]
    [InlineData(ApprovalTaskStatus.Rejected, true)]
    [InlineData(ApprovalTaskStatus.Returned, true)]
    [InlineData(ApprovalTaskStatus.Pending, false)]
    [InlineData(ApprovalTaskStatus.Cancelled, false)]
    public void History_decision_filter_accepts_only_final_decisions(ApprovalTaskStatus status, bool expected) =>
        Assert.Equal(expected, SigningAppService.IsDecision(status));

    [Fact]
    public void Instance_dto_exposes_overdue_flag_and_current_step_code()
    {
        var definition = Definition(Guid.NewGuid(), Guid.NewGuid(), slaDays: 1);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start", Now);

        var onTime = WorkflowAppService.Map(instance, Now);
        Assert.Equal("SIGN1", onTime.CurrentStepCode, ignoreCase: true);
        Assert.False(Assert.Single(onTime.Tasks).IsOverdue);

        var late = WorkflowAppService.Map(instance, Now.AddDays(2));
        Assert.True(Assert.Single(late.Tasks).IsOverdue);
    }

    [Fact]
    public void Completed_instance_has_no_current_step_code()
    {
        var me = Guid.NewGuid();
        var definition = new WorkflowDefinition(Guid.NewGuid(), "single", "Single",
            [new WorkflowStepInput("sign1", "Sign", 1, "Documents.Approve", "SIGN", me)], Now);
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, "start", Now);
        instance.Decide(instance.Tasks.Single().Id, true, me, null, "d1", definition.Steps.ToList(), Now);

        var dto = WorkflowAppService.Map(instance, Now);
        Assert.Null(dto.CurrentStepCode);
        Assert.False(dto.Tasks.Single().IsOverdue);
    }

    [Theory]
    [InlineData(null, WorkflowInstanceScopes.All)]
    [InlineData("", WorkflowInstanceScopes.All)]
    [InlineData("ALL", WorkflowInstanceScopes.All)]
    [InlineData("Mine", WorkflowInstanceScopes.Mine)]
    [InlineData("decidedbyme", WorkflowInstanceScopes.DecidedByMe)]
    public void Instance_scope_is_case_insensitive(string? input, string expected) =>
        Assert.Equal(expected, WorkflowInstanceScopes.Normalize(input));

    [Fact]
    public void Unknown_instance_scope_is_rejected() =>
        Assert.Throws<ArgumentException>(() => WorkflowInstanceScopes.Normalize("everyone"));

    [Fact]
    public void Document_dto_exposes_viewer_state_sent_time_and_processing_method()
    {
        var owner = Guid.NewGuid();
        var viewer = Guid.NewGuid();
        var method = Guid.NewGuid();
        var document = new DocumentAggregate(Guid.NewGuid(), "VB-1", "Title", null, owner, Now, DocumentSourceType.Archive);
        document.SetProcessingMethod(method);
        document.Send(viewer, null, owner, Now.AddHours(1));
        document.RecordAccess("viewed", viewer, Now.AddHours(2));

        var forViewer = DocumentAppService.Map(document, viewer);
        Assert.True(forViewer.IsViewed);
        Assert.Equal(Now.AddHours(1), forViewer.SentAt);
        Assert.Equal(method, forViewer.ProcessingMethodId);
        Assert.False(DocumentAppService.Map(document, owner).IsViewed);

        document.SetProcessingMethod(Guid.Empty);
        Assert.Null(document.ProcessingMethodId);
    }

    private static WorkflowDefinition Definition(Guid firstSigner, Guid secondSigner, int? slaDays = null) =>
        new(Guid.NewGuid(), "two-step", "Two step",
        [
            new WorkflowStepInput("sign1", "First", 1, "Documents.Approve", "SIGN", firstSigner, SlaDays: slaDays),
            new WorkflowStepInput("sign2", "Second", 2, "Documents.Approve", "SIGN", secondSigner, SlaDays: slaDays)
        ], Now);

    private static async Task<(WorkflowDefinition Definition, WorkflowInstance Instance)> StartAsync(
        DocumentServiceDbContext db, Guid firstSigner, Guid secondSigner, WorkflowDefinition? shared = null)
    {
        var definition = shared ?? Definition(firstSigner, secondSigner);
        var overrides = new Dictionary<string, Guid> { ["SIGN1"] = firstSigner, ["SIGN2"] = secondSigner };
        var document = new DocumentAggregate(Guid.NewGuid(), Guid.NewGuid().ToString("N")[..8], "Doc", null,
            firstSigner, Now, DocumentSourceType.Workflow);
        var instance = new WorkflowInstance(Guid.NewGuid(), document.Id, definition, Guid.NewGuid().ToString("N"), Now, overrides);
        if (shared is null) db.WorkflowDefinitions.Add(definition);
        db.Documents.Add(document);
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (definition, instance);
    }

    private static async Task DecideFirstAsync(DocumentServiceDbContext db, WorkflowInstance instance,
        WorkflowDefinition definition, Guid actor, DateTime at)
    {
        var loaded = await db.WorkflowInstances.Include(x => x.Tasks).SingleAsync(x => x.Id == instance.Id);
        var existing = loaded.Tasks.Select(x => x.Id).ToHashSet();
        var pending = loaded.Tasks.Single(x => x.Status == ApprovalTaskStatus.Pending);
        loaded.Decide(pending.Id, true, actor, null, Guid.NewGuid().ToString("N"),
            definition.Steps.OrderBy(x => x.Order).ToList(), at);
        WorkflowAppService.TrackNewApprovalTasks(db, loaded, existing);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
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

using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HCS.WorkManagementService.Tests;

public sealed class WorkAccessScopeTests
{
    [Fact]
    public async Task Project_list_includes_owned_and_assigned_projects_only()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        var owned = new Project(Guid.NewGuid(), "P-OWN", "Owned", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Active", null, me);
        var assigned = new Project(Guid.NewGuid(), "P-ASN", "Assigned", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Active", null, other);
        var hidden = new Project(Guid.NewGuid(), "P-HID", "Hidden", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Active", null, other);
        db.Projects.AddRange(owned, assigned, hidden);
        db.ProjectMembers.Add(new ProjectMember(Guid.NewGuid(), assigned.Id, me, "Member"));
        await db.SaveChangesAsync(ct);

        var visible = await WorkAccessQueries.OwnedOrAssignedProjects(db.Projects, db.ProjectMembers, me)
            .Select(x => x.Code).OrderBy(x => x).ToListAsync(ct);

        Assert.Equal(["P-ASN", "P-OWN"], visible);
    }

    [Fact]
    public async Task Task_list_includes_created_or_assigned_tasks_not_every_project_task()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var project = new Project(Guid.NewGuid(), "P-1", "Project", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Active", null, owner);
        db.Projects.Add(project);
        db.ProjectMembers.Add(new ProjectMember(Guid.NewGuid(), project.Id, member, "Member"));
        var mine = CreateTask(project.Id, "T-MINE", member);
        var assigned = CreateTask(project.Id, "T-ASN", owner);
        var hidden = CreateTask(project.Id, "T-HID", owner);
        db.ProjectTasks.AddRange(mine, assigned, hidden);
        db.ProjectTaskAssignments.Add(new ProjectTaskAssignment(Guid.NewGuid(), assigned.Id, member, "Assignee"));
        await db.SaveChangesAsync(ct);

        var visible = await WorkAccessQueries.CreatedOrAssignedTasks(
                db.ProjectTasks, db.ProjectTaskAssignments, db.Projects, member)
            .Select(x => x.Code).OrderBy(x => x).ToListAsync(ct);

        Assert.Equal(["T-ASN", "T-MINE"], visible);
    }

    [Fact]
    public void Assignee_can_update_but_cannot_delete_or_leave()
    {
        var creator = Guid.NewGuid();
        var assignee = Guid.NewGuid();
        Assert.True(WorkAccessQueries.CanUpdateTask(creator, true, assignee, false));
        Assert.False(WorkAccessQueries.CanDeleteTask(creator, assignee, false, creator));
        Assert.False(WorkAccessQueries.CanManageAssignments(creator, assignee, false, creator));
        Assert.False(WorkAccessQueries.CanManageProject(creator, assignee, false));
        Assert.True(WorkAccessQueries.CanDeleteTask(creator, creator, false));
        Assert.True(WorkAccessQueries.CanManageProject(creator, creator, false));
    }

    [Fact]
    public void New_task_records_the_current_user_as_creator()
    {
        var userId = Guid.NewGuid();
        var task = CreateTask(Guid.NewGuid(), "T-NEW", userId);
        Assert.Equal(userId, task.CreatorId);
        Assert.True(WorkAccessQueries.CanDeleteTask(task.CreatorId, userId, false));
    }

    private static ProjectTask CreateTask(Guid projectId, string code, Guid creatorId)
    {
        var task = new ProjectTask(Guid.NewGuid(), projectId, null, code, code, null,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Normal", "New", 0);
        task.SetCreatedBy(creatorId);
        return task;
    }

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

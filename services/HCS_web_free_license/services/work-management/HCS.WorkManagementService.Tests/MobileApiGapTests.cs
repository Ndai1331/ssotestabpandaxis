using System.Reflection;
using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Controllers;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

namespace HCS.WorkManagementService.Tests;

public sealed class MobileApiGapTests
{
    private static readonly DateTime Day = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Task_file_and_member_role_routes_are_exposed_with_write_policies()
    {
        AssertRoute<HttpPostAttribute>(typeof(ProjectTasksController), nameof(ProjectTasksController.UploadFile), "{id:guid}/files", true);
        AssertRoute<HttpGetAttribute>(typeof(ProjectTasksController), nameof(ProjectTasksController.DownloadFile), "{id:guid}/files/{fileId:guid}", false);
        AssertRoute<HttpDeleteAttribute>(typeof(ProjectTasksController), nameof(ProjectTasksController.DeleteFile), "{id:guid}/files/{fileId:guid}", true);
        AssertRoute<HttpPutAttribute>(typeof(ProjectsController), nameof(ProjectsController.UpdateMemberRole), "{id:guid}/members/{memberId:guid}", true);
    }

    [Fact]
    public async Task Project_filters_match_overlapping_date_ranges_and_owner_department()
    {
        var department = Guid.NewGuid();
        await using var db = CreateDb();
        db.Projects.AddRange(
            Project("before", Day.AddDays(-30), Day.AddDays(-20), department),
            Project("overlap", Day.AddDays(-5), Day.AddDays(5), department),
            Project("other-dept", Day.AddDays(-5), Day.AddDays(5), Guid.NewGuid()),
            Project("after", Day.AddDays(20), Day.AddDays(30), department));
        await db.SaveChangesAsync();

        var codes = await WorkListFilters.ApplyProjectFilters(db.Projects,
                new GetProjectListInput { From = Day, To = Day.AddDays(1), OwnerDepartmentId = department })
            .Select(x => x.Code).ToListAsync();

        Assert.Equal(["overlap"], codes);
    }

    [Fact]
    public async Task Task_filters_support_due_range_priority_root_parent_and_assignee()
    {
        var projectId = Guid.NewGuid();
        var assignee = Guid.NewGuid();
        await using var db = CreateDb();
        var root = NewTask(projectId, "ROOT", null, Day.AddDays(1), "High");
        var child = NewTask(projectId, "CHILD", root.Id, Day.AddDays(2), "Normal");
        var late = NewTask(projectId, "LATE", null, Day.AddDays(40), "High");
        db.ProjectTasks.AddRange(root, child, late);
        db.ProjectTaskAssignments.Add(new ProjectTaskAssignment(Guid.NewGuid(), child.Id, assignee, "Main", "  urgent  "));
        await db.SaveChangesAsync();

        async Task<List<string>> Codes(GetProjectTaskListInput input) =>
            await WorkListFilters.ApplyTaskFilters(db.ProjectTasks, db.ProjectTaskAssignments, input)
                .OrderBy(x => x.Code).Select(x => x.Code).ToListAsync();

        Assert.Equal(["CHILD", "ROOT"], await Codes(new() { From = Day, To = Day.AddDays(7) }));
        Assert.Equal(["LATE", "ROOT"], await Codes(new() { Priority = "High" }));
        Assert.Equal(["LATE", "ROOT"], await Codes(new() { RootOnly = true }));
        Assert.Equal(["CHILD"], await Codes(new() { ParentTaskId = root.Id, RootOnly = true }));
        Assert.Equal(["CHILD"], await Codes(new() { AssigneeUserId = assignee }));

        var assignees = await WorkListFilters.AssigneeUserIdsAsync(db, [root.Id, child.Id], default);
        Assert.Equal([assignee], assignees[child.Id]);
        Assert.False(assignees.ContainsKey(root.Id));
        Assert.Equal("urgent", (await db.ProjectTaskAssignments.SingleAsync()).Note);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(33.3, 33)]
    [InlineData(66.5, 67)]
    [InlineData(100, 100)]
    public void Project_progress_rounds_the_root_task_average(double average, int expected) =>
        Assert.Equal(expected, WorkListFilters.RoundProgress(average));

    [Theory]
    [InlineData(null, TaskDocumentPurposes.Reference)]
    [InlineData("report", TaskDocumentPurposes.Report)]
    [InlineData(" REFERENCE ", TaskDocumentPurposes.Reference)]
    [InlineData("other", null)]
    public void Task_document_purpose_is_normalized(string? input, string? expected) =>
        Assert.Equal(expected, TaskDocumentPurposes.Normalize(input));

    [Fact]
    public void Task_notes_are_trimmed_and_bounded()
    {
        var reference = new ProjectTaskDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "VB-1", Guid.NewGuid(),
            " see page 2 ", ProjectTaskDocument.ReportPurpose);
        Assert.Equal("see page 2", reference.Note);
        Assert.Equal("REPORT", reference.Purpose);
        Assert.Throws<Volo.Abp.BusinessException>(() => new ProjectTaskAssignment(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "Main", new string('x', WorkConsts.NoteLength + 1)));
        Assert.Throws<Volo.Abp.BusinessException>(() => new ProjectTaskDocument(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, null, null, "OTHER"));
    }

    [Fact]
    public void Member_role_can_change_without_changing_member_identity()
    {
        var member = new ProjectMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRoles.Member);
        var id = member.Id;
        member.SetRole(ProjectMemberRoles.Manager);
        Assert.Equal(id, member.Id);
        Assert.Equal("Manager", member.Role);
        Assert.False(ProjectMemberRoles.IsValid("Owner"));
    }

    [Fact]
    public void Task_files_can_be_deleted_by_uploader_task_creator_project_owner_or_admin()
    {
        var uploader = Guid.NewGuid();
        var creator = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        Assert.True(WorkAccessQueries.CanDeleteTaskFile(uploader, uploader, false, creator, owner));
        Assert.True(WorkAccessQueries.CanDeleteTaskFile(uploader, creator, false, creator, owner));
        Assert.True(WorkAccessQueries.CanDeleteTaskFile(uploader, owner, false, creator, owner));
        Assert.True(WorkAccessQueries.CanDeleteTaskFile(uploader, stranger, true, creator, owner));
        Assert.False(WorkAccessQueries.CanDeleteTaskFile(uploader, stranger, false, creator, owner));
    }

    [Fact]
    public void Task_file_contract_never_exposes_internal_blob_name() =>
        Assert.Null(typeof(ProjectTaskFileDto).GetProperty("BlobName"));

    private static void AssertRoute<TAttribute>(Type controller, string method, string template, bool writePolicy)
        where TAttribute : HttpMethodAttribute
    {
        var action = controller.GetMethod(method)!;
        Assert.Equal(template, action.GetCustomAttributes<TAttribute>().Single().Template);
        var policies = action.GetCustomAttributes<AuthorizeAttribute>().Select(x => x.Policy).ToList();
        if (writePolicy) Assert.NotEmpty(policies);
        else Assert.Empty(policies);
    }

    private static Project Project(string code, DateTime start, DateTime end, Guid? department) =>
        new(Guid.NewGuid(), code, code, start, end, "Active", department, Guid.NewGuid());

    private static ProjectTask NewTask(Guid projectId, string code, Guid? parentId, DateTime due, string priority) =>
        new(Guid.NewGuid(), projectId, parentId, code, code, null, Day, due, priority, "New", 0);

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

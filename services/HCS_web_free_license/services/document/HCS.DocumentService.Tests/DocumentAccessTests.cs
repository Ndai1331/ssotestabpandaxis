using System.Security.Claims;
using HCS.DocumentService.Documents;
using Volo.Abp.Authorization;
using Xunit;

namespace HCS.DocumentService.Tests;

public sealed class DocumentAccessTests
{
    [Theory]
    [InlineData(DocumentPermissions.Create)]
    [InlineData(DocumentPermissions.Update)]
    [InlineData(DocumentPermissions.Delete)]
    [InlineData(DocumentPermissions.ManageFiles)]
    public void Mutating_permissions_require_an_explicit_grant(string permission)
    {
        var user = Principal(Guid.NewGuid(), role: "nhanvien");
        Assert.False(DocumentAccess.HasPermission(user, permission));
        ((ClaimsIdentity)user.Identity!).AddClaim(new Claim("permission", permission));
        Assert.True(DocumentAccess.HasPermission(user, permission));
    }

    [Fact]
    public void Archive_browse_requires_view_grant_not_authenticated_fallback()
    {
        var employee = Principal(Guid.NewGuid(), role: "nhanvien");
        Assert.True(DocumentAccess.HasPermission(employee, DocumentPermissions.View));
        Assert.False(DocumentAccess.CanBrowseArchive(employee));
        Assert.Throws<AbpAuthorizationException>(() => DocumentAccess.EnsureCanBrowseArchive(employee));
        Assert.Throws<AbpAuthorizationException>(() =>
            DocumentAccess.EnsureCanCreate(employee, DocumentSourceType.Archive));

        ((ClaimsIdentity)employee.Identity!).AddClaim(new Claim("permission", DocumentPermissions.View));
        Assert.True(DocumentAccess.CanBrowseArchive(employee));
        DocumentAccess.EnsureCanBrowseArchive(employee);
    }

    [Fact]
    public void Update_permission_does_not_grant_delete_or_file_management()
    {
        var user = Principal(Guid.NewGuid(), role: "nhanvien");
        ((ClaimsIdentity)user.Identity!).AddClaim(new Claim("permission", DocumentPermissions.Update));
        Assert.True(DocumentAccess.HasPermission(user, DocumentPermissions.Update));
        Assert.False(DocumentAccess.HasPermission(user, DocumentPermissions.Delete));
        Assert.False(DocumentAccess.HasPermission(user, DocumentPermissions.ManageFiles));
        Assert.True(DocumentAccess.HasPermission(Principal(Guid.NewGuid(), role: "admin"), DocumentPermissions.Delete));
    }

    [Fact]
    public void Workflow_assignee_can_act_without_catalog_permission()
    {
        var userId = Guid.NewGuid();
        var principal = Principal(userId, role: "nhanvien");

        Assert.True(DocumentAccess.CanActOnWorkflowTask(principal, userId, userId));
        DocumentAccess.EnsureCanActOnWorkflowTask(principal, userId, userId);
    }

    [Fact]
    public void Process_decision_requires_workflow_decide_even_for_assignees()
    {
        var employee = Principal(Guid.NewGuid(), role: "nhanvien");
        Assert.Throws<AbpAuthorizationException>(() =>
            DocumentAccess.EnsureCanDecideStep(employee, isSignStep: false, DocumentPermissions.WorkflowDecide));

        ((ClaimsIdentity)employee.Identity!).AddClaim(new Claim("permission", DocumentPermissions.WorkflowDecide));
        DocumentAccess.EnsureCanDecideStep(employee, isSignStep: false, DocumentPermissions.WorkflowDecide);
    }

    [Fact]
    public void Sign_decision_requires_signing_execute_not_workflow_decide()
    {
        var employee = Principal(Guid.NewGuid(), role: "nhanvien");
        ((ClaimsIdentity)employee.Identity!).AddClaim(new Claim("permission", DocumentPermissions.SigningExecute));
        DocumentAccess.EnsureCanDecideStep(employee, isSignStep: true, DocumentPermissions.WorkflowDecide);
        Assert.Throws<AbpAuthorizationException>(() =>
            DocumentAccess.EnsureCanDecideStep(employee, isSignStep: false, DocumentPermissions.WorkflowDecide));
    }

    [Fact]
    public void Other_users_cannot_act_on_an_assigned_step()
    {
        var assignee = Guid.NewGuid();
        var other = Guid.NewGuid();
        var principal = Principal(other, role: "nhanvien");

        Assert.False(DocumentAccess.CanActOnWorkflowTask(principal, other, assignee));
        Assert.Throws<UnauthorizedAccessException>(() =>
            DocumentAccess.EnsureCanActOnWorkflowTask(principal, other, assignee));
    }

    [Fact]
    public void Elevated_users_can_act_on_someone_elses_step()
    {
        var assignee = Guid.NewGuid();
        var leader = Guid.NewGuid();
        var principal = Principal(leader, role: "lanhdao");

        Assert.True(DocumentAccess.CanActOnWorkflowTask(principal, leader, assignee));
    }

    [Fact]
    public void Creator_can_manage_without_loading_history()
    {
        var userId = Guid.NewGuid();
        var document = new DocumentAggregate(Guid.NewGuid(), "CV-100", "Title", null, Guid.NewGuid(), DateTime.UtcNow);
        var principal = Principal(userId, role: "nhanvien");

        Assert.False(DocumentAccess.CanManage(document, userId, principal));
        Assert.True(DocumentAccess.CanManage(document, userId, principal, isCreator: true));
        DocumentAccess.EnsureCanManage(document, userId, principal, isCreator: true);
    }

    private static ClaimsPrincipal Principal(Guid userId, string role) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
                new Claim(ClaimTypes.Role, role)
            ],
            authenticationType: "test"));
}

using System.Security.Claims;
using HCS.DocumentService.Documents;
using Xunit;

namespace HCS.DocumentService.Tests;

public sealed class DocumentAccessTests
{
    [Fact]
    public void Workflow_assignee_can_act_without_catalog_permission()
    {
        var userId = Guid.NewGuid();
        var principal = Principal(userId, role: "nhanvien");

        Assert.True(DocumentAccess.CanActOnWorkflowTask(principal, userId, userId));
        DocumentAccess.EnsureCanActOnWorkflowTask(principal, userId, userId);
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

    private static ClaimsPrincipal Principal(Guid userId, string role) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
                new Claim(ClaimTypes.Role, role)
            ],
            authenticationType: "test"));
}

using HCS.Permissions;
using Xunit;

namespace HCS;

public sealed class HCSPermissionMenuMapTests
{
    [Theory]
    [InlineData("WorkManagement.Dashboard", HCSPermissions.MenuGroups.Workspace)]
    [InlineData("Documents.View", HCSPermissions.MenuGroups.Documents)]
    [InlineData("Documents.Signing.Execute", HCSPermissions.MenuGroups.Documents)]
    [InlineData("Documents.Signing.Report", HCSPermissions.MenuGroups.Catalogs)]
    [InlineData("Documents.Workflow.View", HCSPermissions.MenuGroups.Workflows)]
    [InlineData("Documents.Workflow.Start", HCSPermissions.MenuGroups.Workflows)]
    [InlineData("WorkManagement.Projects", HCSPermissions.MenuGroups.ProjectsAndTasks)]
    [InlineData("WorkManagement.ProjectTasks", HCSPermissions.MenuGroups.ProjectsAndTasks)]
    [InlineData("WorkManagement.Calendar", HCSPermissions.MenuGroups.Calendar)]
    [InlineData("WorkManagement.Events", HCSPermissions.MenuGroups.Events)]
    [InlineData("WorkManagement.Surveys", HCSPermissions.MenuGroups.Surveys)]
    [InlineData("WorkManagement.SurveyManagement", HCSPermissions.MenuGroups.Surveys)]
    [InlineData("WorkManagement.Reports", HCSPermissions.MenuGroups.Catalogs)]
    [InlineData("HCS.Catalogs.ICD10", HCSPermissions.MenuGroups.Catalogs)]
    [InlineData("HCS.Organization.Departments", HCSPermissions.MenuGroups.Catalogs)]
    [InlineData("Documents.Signing.Configure", HCSPermissions.MenuGroups.Catalogs)]
    [InlineData("Collaboration.Social", HCSPermissions.MenuGroups.Social)]
    [InlineData("WorkManagement.EmployeeRatings", HCSPermissions.MenuGroups.Social)]
    [InlineData("WorkManagement.EmployeeRatings.Management", HCSPermissions.MenuGroups.Social)]
    [InlineData("WorkManagement.EmployeeRatings.Dashboard", HCSPermissions.MenuGroups.Social)]
    [InlineData("HCS.Languages", HCSPermissions.MenuGroups.Administration)]
    [InlineData("HCS.AuditViewer", HCSPermissions.MenuGroups.Administration)]
    [InlineData("HCS.ServiceLogs", HCSPermissions.MenuGroups.Administration)]
    [InlineData("HCS.SystemBranding.Update", HCSPermissions.MenuGroups.Administration)]
    [InlineData("HCS.Organization.UserMappings", HCSPermissions.MenuGroups.Administration)]
    [InlineData("Collaboration.Administration", HCSPermissions.MenuGroups.Administration)]
    [InlineData("AbpIdentity.Users", HCSPermissions.MenuGroups.Administration)]
    public void ResolveGroupName_Maps_To_Top_Menu(string permissionName, string expectedGroup)
    {
        Assert.Equal(expectedGroup, HCSPermissionMenuMap.ResolveGroupName(permissionName));
    }
}

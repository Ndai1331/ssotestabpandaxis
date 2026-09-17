using System;
using System.Collections.Generic;

namespace HCS.Permissions;

public static class HCSPermissionMenuMap
{
    public readonly record struct MenuGroup(string Name, string DisplayNameKey);

    public static readonly IReadOnlyList<MenuGroup> Groups =
    [
        new(HCSPermissions.MenuGroups.Workspace, "Menu:Workspace"),
        new(HCSPermissions.MenuGroups.Documents, "Menu:Documents"),
        new(HCSPermissions.MenuGroups.Workflows, "Menu:Workflows"),
        new(HCSPermissions.MenuGroups.ProjectsAndTasks, "Menu:ProjectsAndTasks"),
        new(HCSPermissions.MenuGroups.Calendar, "Menu:CalendarEvents"),
        new(HCSPermissions.MenuGroups.Events, "Menu:EventManagement"),
        new(HCSPermissions.MenuGroups.Surveys, "Menu:Surveys"),
        new(HCSPermissions.MenuGroups.Catalogs, "Menu:Catalogs"),
        new(HCSPermissions.MenuGroups.Social, "Menu:Social"),
        new(HCSPermissions.MenuGroups.Administration, "Menu:Administration")
    ];

    public static string? ResolveGroupName(string? permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            return null;
        }

        if (IsOrChild(permissionName, "Documents.Workflow"))
        {
            return HCSPermissions.MenuGroups.Workflows;
        }

        if (IsOrChild(permissionName, HCSPermissions.Documents.SigningConfigure)
            || IsOrChild(permissionName, HCSPermissions.Documents.SigningReport))
        {
            return HCSPermissions.MenuGroups.Catalogs;
        }

        if (IsOrChild(permissionName, HCSPermissions.Documents.Default))
        {
            return HCSPermissions.MenuGroups.Documents;
        }

        if (IsOrChild(permissionName, HCSPermissions.WorkManagement.Dashboard))
        {
            return HCSPermissions.MenuGroups.Workspace;
        }

        if (IsOrChild(permissionName, HCSPermissions.WorkManagement.Projects)
            || IsOrChild(permissionName, HCSPermissions.WorkManagement.Tasks))
        {
            return HCSPermissions.MenuGroups.ProjectsAndTasks;
        }

        if (IsOrChild(permissionName, HCSPermissions.WorkManagement.Calendar))
        {
            return HCSPermissions.MenuGroups.Calendar;
        }

        if (IsOrChild(permissionName, HCSPermissions.WorkManagement.Events))
        {
            return HCSPermissions.MenuGroups.Events;
        }

        if (IsOrChild(permissionName, HCSPermissions.WorkManagement.Surveys)
            || IsOrChild(permissionName, HCSPermissions.WorkManagement.SurveyManagement))
        {
            return HCSPermissions.MenuGroups.Surveys;
        }

        if (IsOrChild(permissionName, HCSPermissions.WorkManagement.Reports)
            || IsOrChild(permissionName, HCSPermissions.Catalogs.Default)
            || (IsOrChild(permissionName, HCSPermissions.Organization.Default)
                && !IsOrChild(permissionName, HCSPermissions.Organization.UserMappings)))
        {
            return HCSPermissions.MenuGroups.Catalogs;
        }

        if (IsOrChild(permissionName, HCSPermissions.WorkManagement.EmployeeRatings)
            || (IsOrChild(permissionName, HCSPermissions.Collaboration.Default)
                && !IsOrChild(permissionName, HCSPermissions.Collaboration.Administration)))
        {
            return HCSPermissions.MenuGroups.Social;
        }

        if (IsAdministration(permissionName))
        {
            return HCSPermissions.MenuGroups.Administration;
        }

        return null;
    }

    private static bool IsAdministration(string permissionName) =>
        IsOrChild(permissionName, HCSPermissions.Languages.Default)
        || IsOrChild(permissionName, HCSPermissions.AuditViewer.Default)
        || IsOrChild(permissionName, HCSPermissions.ServiceLogs.Default)
        || IsOrChild(permissionName, HCSPermissions.SystemBranding.Default)
        || IsOrChild(permissionName, HCSPermissions.Organization.UserMappings)
        || IsOrChild(permissionName, HCSPermissions.Collaboration.Administration)
        || IsOrChild(permissionName, "Identity")
        || IsOrChild(permissionName, "AbpIdentity")
        || IsOrChild(permissionName, "FeatureManagement")
        || IsOrChild(permissionName, "AbpFeatureManagement")
        || IsOrChild(permissionName, "SettingManagement")
        || IsOrChild(permissionName, "AbpSettingManagement")
        || IsOrChild(permissionName, "PermissionManagement")
        || IsOrChild(permissionName, "AbpPermissionManagement")
        || IsOrChild(permissionName, "OpenIddict")
        || IsOrChild(permissionName, "AbpAccount")
        || IsOrChild(permissionName, "Saas")
        || IsOrChild(permissionName, "AuditLogging");

    private static bool IsOrChild(string permissionName, string root) =>
        string.Equals(permissionName, root, StringComparison.Ordinal)
        || permissionName.StartsWith(root + ".", StringComparison.Ordinal);
}

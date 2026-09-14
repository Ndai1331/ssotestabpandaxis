using HCS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace HCS.Permissions;

public class HCSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var workspace = context.AddGroup(HCSPermissions.MenuGroups.Workspace, L("Menu:Workspace"));
        workspace.AddPermission(HCSPermissions.WorkManagement.Dashboard, L("Permission:WorkManagement.Dashboard"));

        var documents = context.AddGroup(HCSPermissions.MenuGroups.Documents, L("Menu:Documents"));
        documents.AddPermission(HCSPermissions.Documents.View, L("Permission:Documents.View"));
        documents.AddPermission(HCSPermissions.Documents.Create, L("Permission:Documents.Create"));
        documents.AddPermission(HCSPermissions.Documents.Update, L("Permission:Documents.Update"));
        documents.AddPermission(HCSPermissions.Documents.Assign, L("Permission:Documents.Assign"));
        documents.AddPermission(HCSPermissions.Documents.ManageFiles, L("Permission:Documents.ManageFiles"));
        documents.AddPermission(HCSPermissions.Documents.SigningExecute, L("Permission:Documents.Signing.Execute"));
        documents.AddPermission(HCSPermissions.Documents.SigningReport, L("Permission:Documents.Signing.Report"));

        var workflows = context.AddGroup(HCSPermissions.MenuGroups.Workflows, L("Menu:Workflows"));
        workflows.AddPermission(HCSPermissions.Documents.WorkflowView, L("Permission:Documents.Workflow.View"));
        workflows.AddPermission(HCSPermissions.Documents.WorkflowManage, L("Permission:Documents.Workflow.Manage"));
        workflows.AddPermission(HCSPermissions.Documents.WorkflowStart, L("Permission:Documents.Workflow.Start"));
        workflows.AddPermission(HCSPermissions.Documents.WorkflowDecide, L("Permission:Documents.Workflow.Decide"));

        var projects = context.AddGroup(HCSPermissions.MenuGroups.ProjectsAndTasks, L("Menu:ProjectsAndTasks"));
        projects.AddPermission(HCSPermissions.WorkManagement.Projects, L("Permission:WorkManagement.Projects"));
        projects.AddPermission(HCSPermissions.WorkManagement.Tasks, L("Permission:WorkManagement.ProjectTasks"));

        var calendar = context.AddGroup(HCSPermissions.MenuGroups.Calendar, L("Menu:CalendarEvents"));
        calendar.AddPermission(HCSPermissions.WorkManagement.Calendar, L("Permission:WorkManagement.Calendar"));

        var events = context.AddGroup(HCSPermissions.MenuGroups.Events, L("Menu:EventManagement"));
        events.AddPermission(HCSPermissions.WorkManagement.Events, L("Permission:WorkManagement.Events"));

        var surveys = context.AddGroup(HCSPermissions.MenuGroups.Surveys, L("Menu:Surveys"));
        surveys.AddPermission(HCSPermissions.WorkManagement.Surveys, L("Permission:WorkManagement.Surveys"));

        var catalogs = context.AddGroup(HCSPermissions.MenuGroups.Catalogs, L("Menu:Catalogs"));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.MasterData, L("Permission:Catalogs.MasterData")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.DocumentTypes, L("Permission:Catalogs.DocumentTypes")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.Sectors, L("Permission:Catalogs.Sectors")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.UrgencyLevels, L("Permission:Catalogs.UrgencyLevels")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.ConfidentialityLevels, L("Permission:Catalogs.ConfidentialityLevels")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.ProcessingMethods, L("Permission:Catalogs.ProcessingMethods")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.DocumentStatuses, L("Permission:Catalogs.DocumentStatuses")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.SigningMethods, L("Permission:Catalogs.SigningMethods")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.EventTypes, L("Permission:Catalogs.EventTypes")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.Icd10, L("Permission:Catalogs.ICD10")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.BloodPressure, L("Permission:Catalogs.BloodPressure")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.BloodGlucose, L("Permission:Catalogs.BloodGlucose")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.Bmi, L("Permission:Catalogs.BMI")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.Countries, L("Permission:Catalogs.Countries")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.Provinces, L("Permission:Catalogs.Provinces")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Catalogs.Communes, L("Permission:Catalogs.Communes")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Organization.Departments, L("Permission:Organization.Departments")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Organization.Units, L("Permission:Organization.Units")));
        AddCrud(catalogs.AddPermission(HCSPermissions.Organization.Positions, L("Permission:Organization.Positions")));
        catalogs.AddPermission(HCSPermissions.Organization.MasterData, L("Permission:Organization.MasterData"));
        catalogs.AddPermission(HCSPermissions.WorkManagement.SurveyManagement, L("Permission:WorkManagement.SurveyManagement"));
        catalogs.AddPermission(HCSPermissions.WorkManagement.Reports, L("Permission:WorkManagement.Reports"));
        catalogs.AddPermission(HCSPermissions.Documents.SigningConfigure, L("Permission:Documents.Signing.Configure"));

        var social = context.AddGroup(HCSPermissions.MenuGroups.Social, L("Menu:Social"));
        social.AddPermission(HCSPermissions.Collaboration.Chat, L("Permission:Collaboration.Chat"));
        social.AddPermission(HCSPermissions.Collaboration.Social, L("Permission:Collaboration.Social"));
        social.AddPermission(HCSPermissions.Collaboration.Notifications, L("Permission:Collaboration.Notifications"));
        social.AddPermission(HCSPermissions.WorkManagement.EmployeeRatings, L("Permission:WorkManagement.EmployeeRatings"));
        social.AddPermission(HCSPermissions.WorkManagement.EmployeeRatingsManagement, L("Permission:WorkManagement.EmployeeRatings.Management"));
        social.AddPermission(HCSPermissions.WorkManagement.EmployeeRatingsDashboard, L("Permission:WorkManagement.EmployeeRatings.Dashboard"));

        var administration = context.AddGroup(HCSPermissions.MenuGroups.Administration, L("Menu:Administration"));
        var languages = administration.AddPermission(HCSPermissions.Languages.Default, L("Permission:Languages"));
        languages.AddChild(HCSPermissions.Languages.Create, L("Permission:Languages.Create"));
        languages.AddChild(HCSPermissions.Languages.Update, L("Permission:Languages.Update"));
        languages.AddChild(HCSPermissions.Languages.Delete, L("Permission:Languages.Delete"));
        languages.AddChild(HCSPermissions.Languages.ManageTexts, L("Permission:Languages.ManageTexts"));
        administration.AddPermission(HCSPermissions.AuditViewer.Default, L("Permission:AuditViewer"));
        administration.AddPermission(HCSPermissions.SystemBranding.Update, L("Permission:SystemBranding.Update"));
        administration.AddPermission(HCSPermissions.Organization.UserMappings, L("Permission:Organization.UserMappings"));
        administration.AddPermission(HCSPermissions.Collaboration.Administration, L("Permission:Collaboration.Administration"));
    }

    private static void AddCrud(PermissionDefinition permission)
    {
        permission.AddChild(HcsCrudPermissions.Create(permission.Name), L("Permission:Create"));
        permission.AddChild(HcsCrudPermissions.Update(permission.Name), L("Permission:Update"));
        permission.AddChild(HcsCrudPermissions.Delete(permission.Name), L("Permission:Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<HCSResource>(name);
    }
}

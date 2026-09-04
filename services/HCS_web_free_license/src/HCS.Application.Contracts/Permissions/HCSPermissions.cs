namespace HCS.Permissions;

public static class HCSPermissions
{
    public const string GroupName = "HCS";

    public static class Languages
    {
        public const string Default = GroupName + ".Languages";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManageTexts = Default + ".ManageTexts";
    }

    public static class AuditViewer
    {
        public const string Default = GroupName + ".AuditViewer";
    }

    public static class Organization
    {
        public const string Default = HCSOrganizationPermissions.Group;
        public const string Departments = HCSOrganizationPermissions.Departments;
        public const string Units = HCSOrganizationPermissions.Units;
        public const string Positions = HCSOrganizationPermissions.Positions;
        public const string MasterData = HCSOrganizationPermissions.MasterData;
        public const string UserMappings = HCSOrganizationPermissions.UserMappings;

        public static readonly string[] AdministrationPermissions = HCSOrganizationPermissions.AdministrationPermissions;
    }

    public static class Catalogs
    {
        public const string Default = HCSCatalogPermissions.Group;
        public const string MasterData = HCSCatalogPermissions.MasterData;
        public const string DocumentTypes = HCSCatalogPermissions.DocumentTypes;
        public const string Sectors = HCSCatalogPermissions.Sectors;
        public const string UrgencyLevels = HCSCatalogPermissions.UrgencyLevels;
        public const string ConfidentialityLevels = HCSCatalogPermissions.ConfidentialityLevels;
        public const string ProcessingMethods = HCSCatalogPermissions.ProcessingMethods;
        public const string DocumentStatuses = HCSCatalogPermissions.DocumentStatuses;
        public const string SigningMethods = HCSCatalogPermissions.SigningMethods;
        public const string EventTypes = HCSCatalogPermissions.EventTypes;
        public const string Icd10 = HCSCatalogPermissions.Icd10;
        public const string BloodPressure = HCSCatalogPermissions.BloodPressure;
        public const string BloodGlucose = HCSCatalogPermissions.BloodGlucose;
        public const string Bmi = HCSCatalogPermissions.Bmi;
        public const string Countries = HCSCatalogPermissions.Countries;
        public const string Provinces = HCSCatalogPermissions.Provinces;
        public const string Communes = HCSCatalogPermissions.Communes;
    }

    // These permission names are consumed by the standalone Work Management API.
    // They live in the shared authorization catalog so roles can be managed in
    // the central ABP Roles UI and emitted into BFF access tokens.
    public static class WorkManagement
    {
        public const string Default = "WorkManagement";
        public const string Projects = Default + ".Projects";
        public const string Tasks = Default + ".ProjectTasks";
        public const string Calendar = Default + ".Calendar";
        public const string Surveys = Default + ".Surveys";
        public const string SurveyManagement = Default + ".SurveyManagement";
        public const string Reports = Default + ".Reports";
        public const string Dashboard = Default + ".Dashboard";

        public static readonly string[] All =
        [
            Projects,
            Tasks,
            Calendar,
            Surveys,
            SurveyManagement,
            Reports,
            Dashboard
        ];
    }

    public static class Documents
    {
        public const string Default = "Documents";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Assign = Default + ".Assign";
        public const string ManageFiles = Default + ".ManageFiles";
        public const string WorkflowView = Default + ".Workflow.View";
        public const string WorkflowManage = Default + ".Workflow.Manage";
        public const string WorkflowStart = Default + ".Workflow.Start";
        public const string WorkflowDecide = Default + ".Workflow.Decide";
        public const string SigningConfigure = Default + ".Signing.Configure";
        public const string SigningExecute = Default + ".Signing.Execute";
        public const string SigningReport = Default + ".Signing.Report";

        public static readonly string[] All =
        [
            View,
            Create,
            Update,
            Assign,
            ManageFiles,
            WorkflowView,
            WorkflowManage,
            WorkflowStart,
            WorkflowDecide,
            SigningConfigure,
            SigningExecute,
            SigningReport
        ];
    }

    public static class Collaboration
    {
        public const string Default = "Collaboration";
        public const string Chat = Default + ".Chat";
        public const string Social = Default + ".Social";
        public const string Notifications = Default + ".Notifications";
        public const string Administration = Default + ".Administration";
    }
}

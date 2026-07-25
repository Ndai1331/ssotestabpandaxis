namespace hanhchinhso.DocumentService.Permissions;

public static class DocumentServicePermissions
{
    public const string GroupName = "HanhChinhSo.Document";

    public static class Documents
    {
        public const string Default = GroupName + ".Documents";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Files
    {
        public const string Default = GroupName + ".Files";
        public const string Upload = Default + ".Upload";
        public const string Download = Default + ".Download";
        public const string Delete = Default + ".Delete";
    }

    public static class WorkflowDefinitions
    {
        public const string Default = GroupName + ".WorkflowDefinitions";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Workflows
    {
        public const string Default = GroupName + ".Workflows";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class WorkflowTemplates
    {
        public const string Default = GroupName + ".WorkflowTemplates";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class WorkflowStepTemplates
    {
        public const string Default = GroupName + ".WorkflowStepTemplates";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class WorkflowStepAssignments
    {
        public const string Default = GroupName + ".WorkflowStepAssignments";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class WorkflowRuntime
    {
        public const string Default = GroupName + ".WorkflowRuntime";
        public const string Submit = Default + ".Submit";
        public const string SubmitAll = Default + ".SubmitAll";
        public const string Act = Default + ".Act";
        public const string MarkOverdue = Default + ".MarkOverdue";
    }

    public static class SignatureSettings
    {
        public const string Default = GroupName + ".SignatureSettings";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class UserSignatures
    {
        public const string Default = GroupName + ".UserSignatures";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManageAll = Default + ".ManageAll";
        public const string RevokeCredential = Default + ".RevokeCredential";
    }

    public static class SigningAssets
    {
        public const string Default = GroupName + ".SigningAssets";
        public const string Upload = Default + ".Upload";
        public const string Download = Default + ".Download";
        public const string Delete = Default + ".Delete";
        public const string ManageLayouts = Default + ".ManageLayouts";
    }

    public static class SigningExecution
    {
        public const string Default = GroupName + ".SigningExecution";
        public const string Execute = Default + ".Execute";
    }
}

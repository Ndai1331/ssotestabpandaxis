using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace hanhchinhso.DocumentService.Permissions;

public class DocumentServicePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(DocumentServicePermissions.GroupName, L("Permission:Document"));
        AddCrud(group, DocumentServicePermissions.Documents.Default, DocumentServicePermissions.Documents.Create,
            DocumentServicePermissions.Documents.Update, DocumentServicePermissions.Documents.Delete,
            "Permission:Documents");
        var files = group.AddPermission(DocumentServicePermissions.Files.Default, L("Permission:DocumentFiles"));
        files.AddChild(DocumentServicePermissions.Files.Upload, L("Permission:Upload"));
        files.AddChild(DocumentServicePermissions.Files.Download, L("Permission:Download"));
        files.AddChild(DocumentServicePermissions.Files.Delete, L("Permission:Delete"));
        AddCrud(group, DocumentServicePermissions.WorkflowDefinitions.Default,
            DocumentServicePermissions.WorkflowDefinitions.Create,
            DocumentServicePermissions.WorkflowDefinitions.Update,
            DocumentServicePermissions.WorkflowDefinitions.Delete,
            "Permission:WorkflowDefinitions");
        AddCrud(group, DocumentServicePermissions.Workflows.Default,
            DocumentServicePermissions.Workflows.Create,
            DocumentServicePermissions.Workflows.Update,
            DocumentServicePermissions.Workflows.Delete,
            "Permission:Workflows");
        AddCrud(group, DocumentServicePermissions.WorkflowTemplates.Default,
            DocumentServicePermissions.WorkflowTemplates.Create,
            DocumentServicePermissions.WorkflowTemplates.Update,
            DocumentServicePermissions.WorkflowTemplates.Delete,
            "Permission:WorkflowTemplates");
        AddCrud(group, DocumentServicePermissions.WorkflowStepTemplates.Default,
            DocumentServicePermissions.WorkflowStepTemplates.Create,
            DocumentServicePermissions.WorkflowStepTemplates.Update,
            DocumentServicePermissions.WorkflowStepTemplates.Delete,
            "Permission:WorkflowStepTemplates");
        AddCrud(group, DocumentServicePermissions.WorkflowStepAssignments.Default,
            DocumentServicePermissions.WorkflowStepAssignments.Create,
            DocumentServicePermissions.WorkflowStepAssignments.Update,
            DocumentServicePermissions.WorkflowStepAssignments.Delete,
            "Permission:WorkflowStepAssignments");
        var runtime = group.AddPermission(
            DocumentServicePermissions.WorkflowRuntime.Default,
            L("Permission:WorkflowRuntime"));
        runtime.AddChild(
            DocumentServicePermissions.WorkflowRuntime.Submit,
            L("Permission:SubmitWorkflow"));
        runtime.AddChild(
            DocumentServicePermissions.WorkflowRuntime.SubmitAll,
            L("Permission:SubmitWorkflowAll"));
        runtime.AddChild(
            DocumentServicePermissions.WorkflowRuntime.Act,
            L("Permission:WorkflowAct"));
        runtime.AddChild(
            DocumentServicePermissions.WorkflowRuntime.MarkOverdue,
            L("Permission:WorkflowMarkOverdue"));
        AddCrud(group, DocumentServicePermissions.SignatureSettings.Default,
            DocumentServicePermissions.SignatureSettings.Create,
            DocumentServicePermissions.SignatureSettings.Update,
            DocumentServicePermissions.SignatureSettings.Delete,
            "Permission:SignatureSettings");
        var userSignatures = group.AddPermission(
            DocumentServicePermissions.UserSignatures.Default,
            L("Permission:UserSignatures"));
        userSignatures.AddChild(
            DocumentServicePermissions.UserSignatures.Create,
            L("Permission:Create"));
        userSignatures.AddChild(
            DocumentServicePermissions.UserSignatures.Update,
            L("Permission:Update"));
        userSignatures.AddChild(
            DocumentServicePermissions.UserSignatures.Delete,
            L("Permission:Delete"));
        userSignatures.AddChild(
            DocumentServicePermissions.UserSignatures.ManageAll,
            L("Permission:ManageAllUserSignatures"));
        userSignatures.AddChild(
            DocumentServicePermissions.UserSignatures.RevokeCredential,
            L("Permission:RevokeSignatureCredential"));
        var signingAssets = group.AddPermission(
            DocumentServicePermissions.SigningAssets.Default,
            L("Permission:SigningAssets"));
        signingAssets.AddChild(
            DocumentServicePermissions.SigningAssets.Upload,
            L("Permission:Upload"));
        signingAssets.AddChild(
            DocumentServicePermissions.SigningAssets.Download,
            L("Permission:Download"));
        signingAssets.AddChild(
            DocumentServicePermissions.SigningAssets.Delete,
            L("Permission:Delete"));
        signingAssets.AddChild(
            DocumentServicePermissions.SigningAssets.ManageLayouts,
            L("Permission:ManageSigningLayouts"));
        var signingExecution = group.AddPermission(
            DocumentServicePermissions.SigningExecution.Default,
            L("Permission:SigningExecution"));
        signingExecution.AddChild(
            DocumentServicePermissions.SigningExecution.Execute,
            L("Permission:ExecuteSigning"));
    }

    private static void AddCrud(PermissionGroupDefinition group, string name, string create, string update,
        string delete, string displayName)
    {
        var permission = group.AddPermission(name, L(displayName));
        permission.AddChild(create, L("Permission:Create"));
        permission.AddChild(update, L("Permission:Update"));
        permission.AddChild(delete, L("Permission:Delete"));
    }

    private static LocalizableString L(string name) =>
        LocalizableString.Create<Localization.DocumentServiceResource>(name);
}

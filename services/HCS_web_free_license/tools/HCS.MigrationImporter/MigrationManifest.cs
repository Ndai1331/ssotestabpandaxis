namespace HCS.MigrationImporter;

public static class MigrationManifest
{
    public static TableMigrationSpec UserMappingSource { get; } = T("AbpUsers", TargetDatabase.Identity, "AbpUsers", ["Id"], ["CreatorId", "LastModifierId", "DeleterId"]);

    // The allowlist contains target-shaped tables. Source tables without a corresponding
    // Community table are retained in legacy_migration by the archive pass.
    public static IReadOnlyList<TableMigrationSpec> Tables { get; } =
    [
        T("AbpUsers", TargetDatabase.Identity, "AbpUsers", ["Id"], ["CreatorId", "LastModifierId", "DeleterId"]),
        T("AbpRoles", TargetDatabase.Identity, "AbpRoles", ["Id"]),
        T("AbpUserRoles", TargetDatabase.Identity, "AbpUserRoles", ["UserId", "RoleId"], ["UserId"]),
        T("AbpRoleClaims", TargetDatabase.Identity, "AbpRoleClaims", ["Id"]),
        T("AbpPermissionGrants", TargetDatabase.Identity, "AbpPermissionGrants", ["Id"]),
        T("AbpPermissions", TargetDatabase.Identity, "AbpPermissions", ["Id"]),
        T("AbpPermissionGroups", TargetDatabase.Identity, "AbpPermissionGroups", ["Id"]),
        T("AbpClaimTypes", TargetDatabase.Identity, "AbpClaimTypes", ["Id"]),
        T("AbpOrganizationUnits", TargetDatabase.Identity, "AbpOrganizationUnits", ["Id"], ["CreatorId", "LastModifierId", "DeleterId"]),
        T("AbpOrganizationUnitRoles", TargetDatabase.Identity, "AbpOrganizationUnitRoles", ["RoleId", "OrganizationUnitId"]),
        T("AbpUserOrganizationUnits", TargetDatabase.Identity, "AbpUserOrganizationUnits", ["UserId", "OrganizationUnitId"], ["UserId", "CreatorId"]),
        T("AbpUserClaims", TargetDatabase.Identity, "AbpUserClaims", ["Id"], ["UserId"]),
        T("AbpFeatureGroups", TargetDatabase.Identity, "AbpFeatureGroups", ["Id"]),
        T("AbpFeatures", TargetDatabase.Identity, "AbpFeatures", ["Id"]),
        T("AbpFeatureValues", TargetDatabase.Identity, "AbpFeatureValues", ["Id"]),
        T("AbpSettingDefinitions", TargetDatabase.Identity, "AbpSettingDefinitions", ["Id"]),
        T("AbpSettings", TargetDatabase.Identity, "AbpSettings", ["Id"]),
        T("AbpAuditLogs", TargetDatabase.Identity, "AbpAuditLogs", ["Id"], ["UserId"]),
        T("AbpAuditLogActions", TargetDatabase.Identity, "AbpAuditLogActions", ["Id"]),
        T("AbpEntityChanges", TargetDatabase.Identity, "AbpEntityChanges", ["Id"]),
        T("AbpEntityPropertyChanges", TargetDatabase.Identity, "AbpEntityPropertyChanges", ["Id"]),
        T("AbpSecurityLogs", TargetDatabase.Identity, "AbpSecurityLogs", ["Id"], ["UserId"]),
        T("AbpSessions", TargetDatabase.Identity, "AbpSessions", ["Id"], ["UserId"]),
        T("AbpLanguages", TargetDatabase.Identity, "HcsLanguages", ["Id"]),
        T("AbpLanguageTexts", TargetDatabase.Identity, "HcsLanguageTexts", ["Id"]),
        T("AppDepartments", TargetDatabase.Organization, "Departments", ["Id"], ["ManagerId"]),
        T("AppUnits", TargetDatabase.Organization, "Units", ["Id"], relationships: [R("DepartmentId", "Departments")]),
        T("AppPositions", TargetDatabase.Organization, "Positions", ["Id"]),
        T("AppMasterDatas", TargetDatabase.Organization, "MasterDataItems", ["Id"]),
        T("AppUserDepartments", TargetDatabase.Organization, "UserOrganizationMappings", ["Id"], ["UserId"], relationships: [R("DepartmentId", "Departments"), R("UnitId", "Units"), R("PositionId", "Positions")], archiveUniqueConflicts: true),
        T("AppDocuments", TargetDatabase.Document, "Documents", ["Id"], ["CreatorId", "FromUserId", "ReceiverUserId"]),
        T("AppDocumentFiles", TargetDatabase.Document, "DocumentFiles", ["Id"], null, ["Path"], [R("DocumentId", "Documents")], archiveUniqueConflicts: true),
        T("AppDocumentHistories", TargetDatabase.Document, "DocumentHistories", ["Id"], ["FromUser", "ToUser", "CreatorId"], relationships: [R("DocumentId", "Documents")]),
        T("AppDocumentAssignments", TargetDatabase.Document, "DocumentAssignments", ["Id"], ["ReceiverUserId", "CreatorId"], relationships: [R("DocumentId", "Documents")], archiveUniqueConflicts: true),
        T("AppWorkflowDefinitions", TargetDatabase.Document, "WorkflowDefinitions", ["Id"]),
        T("AppWorkflowTemplates", TargetDatabase.Document, "WorkflowTemplates", ["Id"]),
        T("AppWorkflowStepTemplates", TargetDatabase.Document, "WorkflowSteps", ["Id"]),
        T("AppDocumentWorkflowInstances", TargetDatabase.Document, "WorkflowInstances", ["Id"], ["CreatorId"]),
        // Signing credentials are a global catalog. CreatorId remains source audit data,
        // not ownership, so it must never be used as a target uniqueness scope.
        T("AppSignatureSettings", TargetDatabase.Document, "SigningCredentials", ["Id"], archiveUniqueConflicts: true),
        T("AppUserSignatures", TargetDatabase.Document, "UserSignatures", ["Id"], ["IdentityUserId"], ["SignatureImage"]),
        T("AppProjects", TargetDatabase.Work, "Projects", ["Id"], ["CreatorId"]),
        T("AppProjectMembers", TargetDatabase.Work, "ProjectMembers", ["Id"], ["UserId"], relationships: [R("ProjectId", "Projects")], archiveUniqueConflicts: true),
        T("AppProjectTasks", TargetDatabase.Work, "ProjectTasks", ["Id"], ["AssigneeId"], relationships: [R("ProjectId", "Projects")]),
        T("AppProjectTaskAssignments", TargetDatabase.Work, "ProjectTaskAssignments", ["Id"], ["UserId", "AssigneeId"], relationships: [R("ProjectTaskId", "ProjectTasks")]),
        T("AppProjectTaskDocuments", TargetDatabase.Work, "ProjectTaskDocuments", ["Id"], relationships: [R("ProjectTaskId", "ProjectTasks")]),
        T("AppCalendarEvents", TargetDatabase.Work, "CalendarEvents", ["Id"], ["CreatorId"]),
        T("AppCalendarEventParticipants", TargetDatabase.Work, "CalendarEventParticipants", ["Id"], ["IdentityUserId"], relationships: [R("CalendarEventId", "CalendarEvents")], archiveUniqueConflicts: true),
        T("AppSurveyLocations", TargetDatabase.Work, "SurveyLocations", ["Id"]),
        T("AppSurveySessions", TargetDatabase.Work, "SurveySessions", ["Id"], ["CreatorId"]),
        T("AppSurveyCriterias", TargetDatabase.Work, "SurveyCriteria", ["Id"]),
        T("AppSurveyResults", TargetDatabase.Work, "SurveyResults", ["Id"], ["UserId", "CreatorId"]),
        T("AppSurveyFiles", TargetDatabase.Work, "SurveyFiles", ["Id"], null, ["FilePath"]),
        T("AppReports", TargetDatabase.Work, "ReportReadModels", ["Id"]),
        T("ChatConversations", TargetDatabase.Collaboration, "CollaborationConversations", ["Id"]),
        T("ChatConversationMembers", TargetDatabase.Collaboration, "CollaborationConversationMembers", ["Id"], ["UserId"], relationships: [R("ConversationId", "CollaborationConversations")]),
        T("ChatMessages", TargetDatabase.Collaboration, "CollaborationMessages", ["Id"], ["CreatorId", "PinnedByUserId"], relationships: [R("ConversationId", "CollaborationConversations")]),
        T("ChatMessageFiles", TargetDatabase.Collaboration, "CollaborationAttachments", ["Id"], null, ["Path"], [R("MessageId", "CollaborationMessages")]),
        T("ChatUserMessages", TargetDatabase.Collaboration, "CollaborationInbox", ["Id"], ["UserId"]),
        T("AppNotifications", TargetDatabase.Collaboration, "CollaborationNotifications", ["Id"], ["CreatorId"]),
        T("AppNotificationReceivers", TargetDatabase.Collaboration, "CollaborationNotificationReceivers", ["Id"], ["IdentityUserId"]),
        T("AppUserPushDeviceTokens", TargetDatabase.Collaboration, "CollaborationPushDeviceTokens", ["Id"], ["UserId"], archiveUniqueConflicts: true)
    ];

    public static readonly string[] ExcludedNameFragments =
    ["Saas", "Tenant", "Gdpr", "TextTemplate", "FileManagement", "Fm", "Form", "Frm", "OpenIddict"];

    public static IReadOnlyList<TableMigrationSpec> Select(IReadOnlySet<string>? requested)
    {
        if (requested is null || requested.Count == 0) return Tables;
        var unknown = requested.Except(Tables.Select(x => x.SourceTable), StringComparer.OrdinalIgnoreCase).ToArray();
        if (unknown.Length > 0) throw new InvalidOperationException($"Tables are not allowlisted: {string.Join(", ", unknown)}");
        return Tables.Where(x => requested.Contains(x.SourceTable)).ToArray();
    }

    public static void EnsureAllowed(string table)
    {
        if (ExcludedNameFragments.Any(x => table.Contains(x, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Excluded commercial/module table rejected: {table}");
        if (!Tables.Append(UserMappingSource).Any(x => x.SourceTable.Equals(table, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Table is not in the explicit migration allowlist: {table}");
    }

    private static TableMigrationSpec T(string source, TargetDatabase db, string target, string[] keys,
        string[]? users = null, string[]? blobs = null, RelationshipSpec[]? relationships = null,
        bool archiveUniqueConflicts = false)
        => new(source, db, target, keys, users, blobs, relationships, SchemaFor(db), archiveUniqueConflicts);
    private static RelationshipSpec R(string column, string table) => new(column, table);

    private static string SchemaFor(TargetDatabase database) => database switch
    {
        TargetDatabase.Organization => "hcs_organization",
        TargetDatabase.Document => "document",
        TargetDatabase.Work => "hcs_work",
        _ => "public"
    };
}

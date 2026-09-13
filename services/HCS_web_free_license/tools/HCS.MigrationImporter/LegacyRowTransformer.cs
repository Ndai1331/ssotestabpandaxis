using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HCS.MigrationImporter;

public static class LegacyRowTransformer
{
    private static readonly IReadOnlyDictionary<string, string> MasterDataTypeMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LOAI_VB"] = "DocumentType",
            ["LINH_VUC_VB"] = "Sector",
            ["MUC_DO_KHAN"] = "UrgencyLevel",
            ["MUC_DO_MAT"] = "ConfidentialityLevel",
            ["HINH_THUC_XL"] = "ProcessingMethod",
            ["TRANG_THAI_VB"] = "DocumentStatus",
            ["LOAI_KY"] = "SigningMethod",
            ["LOAI_SU_KIEN"] = "EventType"
        };

    public static SourceRow Transform(TableMigrationSpec table, SourceRow source,
        IReadOnlyDictionary<string, Guid?> userMap, MigrationLookup lookup)
    {
        var values = table.SourceTable switch
        {
            "AbpUsers" => IdentityUser(source.Values),
            "AbpLanguages" => Language(source.Values),
            "AbpLanguageTexts" => LanguageText(source.Values),
            "AppDepartments" => Department(source.Values),
            "AppUnits" => Unit(source.Values, lookup),
            "AppPositions" => Position(source.Values, lookup),
            "AppMasterDatas" => MasterData(source.Values),
            "AppUserDepartments" => UserOrganizationMapping(source.Values),
            "AppDocuments" => Document(source.Values, lookup),
            "AppDocumentFiles" => DocumentFile(source.Values),
            "AppDocumentHistories" => DocumentHistory(source.Values),
            "AppDocumentAssignments" => DocumentAssignment(source.Values),
            "AppWorkflowDefinitions" => WorkflowDefinition(source.Values),
            "AppWorkflowTemplates" => WorkflowTemplate(source.Values, lookup),
            "AppWorkflowStepTemplates" => WorkflowStep(source.Values, userMap, lookup),
            "AppDocumentWorkflowInstances" => WorkflowInstance(source.Values, lookup),
            "AppSignatureSettings" => SigningCredential(source.Values),
            "AppUserSignatures" => UserSignature(source.Values),
            "AppProjects" => Project(source.Values, lookup),
            "AppProjectMembers" => ProjectMember(source.Values),
            "AppProjectTasks" => ProjectTask(source.Values, lookup),
            "AppProjectTaskAssignments" => ProjectTaskAssignment(source.Values),
            "AppProjectTaskDocuments" => ProjectTaskDocument(source.Values),
            "AppCalendarEvents" => CalendarEvent(source.Values),
            "AppCalendarEventParticipants" => CalendarParticipant(source.Values),
            "AppSurveySessions" => SurveySession(source.Values),
            "AppSurveyCriterias" => SurveyCriteria(source.Values),
            "AppSurveyLocations" => SurveyLocation(source.Values),
            "AppSurveyResults" => SurveyResult(source.Values),
            "AppSurveyFiles" => SurveyFile(source.Values),
            "AppReports" => Report(source.Values),
            "ChatConversations" => Conversation(source.Values),
            "ChatConversationMembers" => ConversationMember(source.Values),
            "ChatMessages" => ChatMessage(source.Values),
            "ChatMessageFiles" => ChatAttachment(source.Values, lookup),
            "ChatUserMessages" => InboxMessage(source.Values),
            "AppNotifications" => Notification(source.Values),
            "AppNotificationReceivers" => NotificationReceiver(source.Values),
            "AppUserPushDeviceTokens" => PushDeviceToken(source.Values),
            _ => ProjectCommon(source.Values)
        };

        RemapTargetUsers(values, table, userMap);
        return new SourceRow(source.Table, values);
    }

    private static JsonObject IdentityUser(JsonObject source)
    {
        var id = Guid(source, "Id");
        return Object(
            ("Id", id), ("TenantId", null), ("UserName", Required(source, "UserName", id)),
            ("NormalizedUserName", Required(source, "NormalizedUserName", id).ToUpperInvariant()),
            ("Name", Text(source, "Name")), ("Surname", Text(source, "Surname")),
            ("Email", Required(source, "Email", id)),
            ("NormalizedEmail", Required(source, "NormalizedEmail", id).ToUpperInvariant()),
            ("EmailConfirmed", Bool(source, "EmailConfirmed") ?? true), ("PasswordHash", null),
            ("SecurityStamp", StableStamp(id)), ("IsExternal", true),
            ("PhoneNumber", Text(source, "PhoneNumber")),
            ("PhoneNumberConfirmed", Bool(source, "PhoneNumberConfirmed") ?? false),
            ("IsActive", Bool(source, "IsActive") ?? true),
            ("TwoFactorEnabled", Bool(source, "TwoFactorEnabled") ?? false),
            ("LockoutEnd", Node(source, "LockoutEnd")), ("LockoutEnabled", Bool(source, "LockoutEnabled") ?? true),
            ("AccessFailedCount", Int(source, "AccessFailedCount") ?? 0),
            ("ShouldChangePasswordOnNextLogin", false), ("EntityVersion", Int(source, "EntityVersion") ?? 0),
            ("LastPasswordChangeTime", null), ("LastSignInTime", null), ("Leaved", false),
            ("ExtraProperties", Text(source, "ExtraProperties") ?? "{}"),
            ("ConcurrencyStamp", Text(source, "ConcurrencyStamp") ?? StableStamp(id)),
            ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch),
            ("CreatorId", Guid(source, "CreatorId")), ("LastModificationTime", Date(source, "LastModificationTime")),
            ("LastModifierId", Guid(source, "LastModifierId")), ("IsDeleted", Bool(source, "IsDeleted") ?? false),
            ("DeleterId", Guid(source, "DeleterId")), ("DeletionTime", Date(source, "DeletionTime")));
    }

    private static JsonObject Language(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("CultureName", Text(source, "CultureName") ?? "vi"),
        ("DisplayName", Text(source, "DisplayName") ?? Text(source, "CultureName") ?? "Language"),
        ("IsEnabled", Bool(source, "IsEnabled") ?? true), ("IsDefault", Bool(source, "IsDefault") ?? false),
        ("ExtraProperties", Text(source, "ExtraProperties") ?? "{}"),
        ("ConcurrencyStamp", Text(source, "ConcurrencyStamp") ?? StableStamp(Guid(source, "Id"))),
        ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch), ("CreatorId", Guid(source, "CreatorId")),
        ("LastModificationTime", Date(source, "LastModificationTime")), ("LastModifierId", Guid(source, "LastModifierId")),
        ("IsDeleted", Bool(source, "IsDeleted") ?? false), ("DeleterId", Guid(source, "DeleterId")),
        ("DeletionTime", Date(source, "DeletionTime")));

    private static JsonObject LanguageText(JsonObject source) => ProjectCommon(source,
        "Id", "ResourceName", "CultureName", "Name", "Value", "ExtraProperties", "ConcurrencyStamp",
        "CreationTime", "CreatorId", "LastModificationTime", "LastModifierId", "IsDeleted", "DeleterId", "DeletionTime");

    private static JsonObject Department(JsonObject source) => Coded(source,
        ("ParentId", Guid(source, "ParentId")));

    private static JsonObject Unit(JsonObject source, MigrationLookup lookup) => Coded(source,
        ("DepartmentId", Guid(source, "DepartmentId") ?? lookup.FallbackDepartmentIdForUnits));

    private static JsonObject Position(JsonObject source, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var code = Required(source, "Code", id);
        if (lookup.DuplicatePositionCodes.Contains(code)) code = $"{code}-{Short(id)}";
        return Coded(source, ("Code", code),
            ("SignOrder", Int(source, "SignOrder") ?? 0), ("SortOrder", Int(source, "SortOrder") ?? 0));
    }

    private static JsonObject MasterData(JsonObject source)
    {
        var legacyType = Required(source, "Type", Guid(source, "Id"));
        var type = MasterDataTypeMap.TryGetValue(legacyType, out var mappedType)
            ? mappedType
            : legacyType;
        return Coded(source, ("Type", type));
    }

    private static JsonObject Coded(JsonObject source, params (string Name, object? Value)[] extra)
    {
        var result = Object(
            ("Id", Guid(source, "Id")), ("ParentId", null), ("DepartmentId", null), ("Type", null),
            ("SignOrder", null), ("SortOrder", Int(source, "SortOrder") ?? 0),
            ("Code", Required(source, "Code", Guid(source, "Id"))),
            ("Name", Required(source, "Name", Guid(source, "Id"))),
            ("IsActive", Bool(source, "IsActive") ?? !(Bool(source, "IsDeleted") ?? false)));
        AddAudit(result, source, includeFullAudit: false);
        foreach (var item in extra) result[item.Name] = ToNode(item.Value);
        return result;
    }

    private static JsonObject UserOrganizationMapping(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("UserId", Guid(source, "UserId")),
        ("DepartmentId", Guid(source, "DepartmentId")), ("UnitId", null), ("PositionId", null),
        ("IsPrimary", Bool(source, "IsPrimary") ?? false),
        ("ExtraProperties", Text(source, "ExtraProperties") ?? "{}"),
        ("ConcurrencyStamp", Text(source, "ConcurrencyStamp") ?? StableStamp(Guid(source, "Id"))),
        ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch),
        ("CreatorId", Guid(source, "CreatorId")), ("LastModificationTime", Date(source, "LastModificationTime")),
        ("LastModifierId", Guid(source, "LastModifierId")));

    private static JsonObject Document(JsonObject source, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var number = Text(source, "No");
        if (string.IsNullOrWhiteSpace(number)) number = $"LEGACY-{id:N}";
        if (lookup.DuplicateDocumentNumbers.Contains(number)) number = $"{number}-{Short(id)}";
        return Object(
            ("Id", id), ("Number", Limit(number, 64)), ("Title", Required(source, "Title", id)),
            ("Description", Text(source, "Description")),
            ("Status", DocumentStatus(Text(source, "CurrentStatus"))),
            ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch),
            ("ConfidentialityId", Guid(source, "SecrecyLevelId")), ("DocumentTypeId", Guid(source, "TypeId")),
            ("SectorId", null), ("UrgencyId", Guid(source, "UrgencyLevelId")),
            ("SourceType", Int(source, "SourceType") ?? 0), ("ParentDocumentId", Guid(source, "ParentDocumentId")),
            ("FromUserId", Guid(source, "FromUserId")), ("OrganizationUnitId", Guid(source, "OrganizationUnitId")),
            // Kept in the transformed object for reconciliation/tests; target store filters non-target columns.
            ("CreatorId", Guid(source, "CreatorId")));
    }

    private static JsonObject DocumentFile(JsonObject source)
    {
        var id = Guid(source, "Id");
        var path = Text(source, "Path") ?? $"legacy/{id:N}";
        var fileName = Text(source, "Name") ?? Path.GetFileName(path);
        return Object(
            ("Id", id), ("DocumentId", Guid(source, "DocumentId")), ("FileName", Limit(fileName, 256)),
            ("ContentType", ContentType(fileName)), ("Size", 0L), ("Sha256", Sha256(source, path)),
            ("BlobName", Limit(path, 512)), ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch),
            ("IsPendingDeletion", false), ("PairedFileId", Guid(source, "SourceDocxFileId") ?? Guid(source, "DerivedPdfFileId")));
    }

    private static JsonObject DocumentHistory(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("DocumentId", Guid(source, "DocumentId")),
        ("Action", Limit(Text(source, "Action") ?? "LEGACY", 128)),
        ("ActorUserId", Guid(source, "FromUser") ?? Guid(source, "CreatorId")),
        ("Detail", Limit(Text(source, "Comment"), 2000)),
        ("OccurredAt", Date(source, "CreationTime") ?? DateTime.UnixEpoch));

    private static JsonObject DocumentAssignment(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("DocumentId", Guid(source, "DocumentId")),
        ("AssigneeUserId", Guid(source, "ReceiverUserId")),
        ("Responsibility", Limit(Text(source, "ActionType") ?? "VIEW", 128)),
        ("AssignedAt", Date(source, "AssignedAt", "CreationTime") ?? DateTime.UnixEpoch),
        ("IsCurrent", Bool(source, "IsCurrent") ?? false), ("StepCode", null));

    private static JsonObject WorkflowDefinition(JsonObject source)
    {
        var id = Guid(source, "Id");
        return Object(
            ("Id", id), ("Code", Required(source, "Code", id)), ("Name", Required(source, "Name", id)),
            ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch), ("SignMode", "SEQUENTIAL"),
            ("KindId", null), ("Description", Limit(Text(source, "Description"), 2000)),
            ("IsActive", (Bool(source, "IsActive") ?? true) && !(Bool(source, "IsDeleted") ?? false)));
    }

    private static JsonObject WorkflowTemplate(JsonObject source, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var definitionId = Guid(source, "WorkflowId");
        if (definitionId.HasValue && lookup.WorkflowDefinitionByWorkflowId.TryGetValue(definitionId.Value, out var mapped))
            definitionId = mapped;
        var code = Required(source, "Code", id);
        if (lookup.DuplicateWorkflowTemplateCodes.Contains(code)) code = $"{code}-{Short(id)}";
        return Object(
            ("Id", id), ("Code", code), ("Name", Required(source, "Name", id)),
            ("DefinitionId", definitionId), ("Version", 1),
            ("TemplateJson", Text(source, "ContentSchema") ?? "{}"),
            ("IsActive", !(Bool(source, "IsDeleted") ?? false)),
            ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch),
            ("PdfFileId", null), ("PdfFileName", FileName(source, "PdfTemplatePath")),
            ("PdfContentType", ContentType(FileName(source, "PdfTemplatePath"))),
            ("PdfBlobName", Text(source, "PdfTemplatePath")), ("WordFileId", null),
            ("WordFileName", FileName(source, "WordTemplatePath")),
            ("WordContentType", ContentType(FileName(source, "WordTemplatePath"))),
            ("WordBlobName", Text(source, "WordTemplatePath")),
            ("OutputFormat", Limit(Text(source, "OutputFormat") ?? "PDF", 16)));
    }

    private static JsonObject WorkflowStep(JsonObject source, IReadOnlyDictionary<string, Guid?> userMap, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var templateId = Guid(source, "WorkflowTemplateId");
        var definitionId = templateId.HasValue && lookup.WorkflowDefinitionByTemplateId.TryGetValue(templateId.Value, out var mapped)
            ? mapped : templateId;
        var assignment = id.HasValue ? lookup.WorkflowAssignmentByStepId.GetValueOrDefault(id.Value) : null;
        var originalOrder = Int(source, "Order") ?? 0;
        var order = id.HasValue ? lookup.WorkflowStepOrderById.GetValueOrDefault(id.Value, originalOrder) : originalOrder;
        var userId = assignment?.DefaultUserId;
        var userIds = RemapGuidArray(assignment?.DefaultUserIdsJson, userMap);
        if (userId.HasValue && userMap.TryGetValue(userId.Value.ToString(), out var remapped) && remapped.HasValue) userId = remapped;
        return Object(
            ("Id", id), ("DefinitionId", definitionId), ("Code", $"STEP-{order:000}-{Short(id)}"),
            ("Name", Required(source, "Name", id)), ("Order", order),
            ("RequiredPermission", "Documents.Workflow.Decide"),
            ("Type", NormalizeStepType(Text(source, "Type"))), ("AssigneeUserId", userId),
            ("AssigneeType", NormalizeAssigneeType(assignment?.AssigneeType)), ("RoleId", assignment?.RoleId),
            ("UserIdsJson", userIds), ("DepartmentIdsJson", assignment?.OrganizationUnitIdsJson ?? "[]"),
            ("SlaDays", Math.Max(0, Int(source, "SLADays") ?? 0)),
            ("AllowReturn", Bool(source, "AllowReturn") ?? false));
    }

    private static JsonObject WorkflowInstance(JsonObject source, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var workflowId = Guid(source, "WorkflowId");
        var definitionId = workflowId.HasValue && lookup.WorkflowDefinitionByWorkflowId.TryGetValue(workflowId.Value, out var mapped)
            ? mapped : workflowId;
        var currentStepId = Guid(source, "CurrentStepId");
        return Object(
            ("Id", id), ("DocumentId", Guid(source, "DocumentId")), ("DefinitionId", definitionId),
            ("Status", WorkflowStatus(Text(source, "Status"))),
            ("CurrentStep", currentStepId.HasValue ? lookup.WorkflowStepOrderById.GetValueOrDefault(currentStepId.Value) : 0),
            ("IdempotencyKey", $"legacy-{id:N}"),
            ("CreationTime", Date(source, "CreationTime", "StartedAt") ?? DateTime.UnixEpoch),
            ("ViewScopesJson", Text(source, "ViewStepScopesJson") ?? "{}"));
    }

    private static JsonObject SigningCredential(JsonObject source)
    {
        var id = Guid(source, "Id");
        return Object(
            // AppSignatureSettings is a global catalog in the legacy application.
            // CreatorId is audit metadata and must not become credential ownership.
            ("Id", id), ("UserId", null),
            ("Kind", SigningKind(Text(source, "ProviderType"), Text(source, "DefaultSignType"))),
            ("Endpoint", Limit(Text(source, "ApiEndpoint") ?? "", 1024)), ("ProtectedSecret", ""),
            ("UpdatedAt", Date(source, "LastModificationTime", "CreationTime") ?? DateTime.UnixEpoch),
            ("AllowDigitalSign", Bool(source, "AllowDigitalSign") ?? false),
            ("AllowElectronicSign", Bool(source, "AllowElectronicSign") ?? false),
            ("ApiTimeoutSeconds", Math.Clamp(Int(source, "ApiTimeout") ?? 30, 5, 600)),
            ("LayoutImageBase64", null), ("ProviderCode", Limit(Text(source, "ProviderCode") ?? "", 256)),
            ("RequireOtp", Bool(source, "RequireOtp") ?? false),
            ("SignHeight", Math.Clamp(Int(source, "SignHeight") ?? 70, 20, 1000)),
            ("SignWidth", Math.Clamp(Int(source, "SignWidth") ?? 150, 40, 1000)),
            ("ProviderType", Limit(Text(source, "ProviderType") ?? "", 64)),
            ("DefaultSignType", Limit(Text(source, "DefaultSignType") ?? "", 64)),
            ("SignedFileSuffix", Limit(Text(source, "SignedFileSuffix") ?? "signed", 128)),
            ("KeepOriginalFile", Bool(source, "KeepOriginalFile") ?? false),
            ("OverwriteSignedFile", Bool(source, "OverwriteSignedFile") ?? false),
            ("EnableSignLog", Bool(source, "EnableSignLog") ?? false),
            ("IsActive", Bool(source, "IsActive") ?? true),
            ("IsDeleted", Bool(source, "IsDeleted") ?? false),
            ("LegacyLayoutImagePath", Limit(Text(source, "LayoutImg"), 512)));
    }

    private static JsonObject UserSignature(JsonObject source)
    {
        var id = Guid(source, "Id");
        var blob = Text(source, "SignatureImage") ?? $"legacy/signatures/{id:N}";
        var fileName = Path.GetFileName(blob.Replace('\\', '/'));
        return Object(
            ("Id", id), ("UserId", Guid(source, "IdentityUserId")), ("FileName", Limit(fileName, 256)),
            ("ContentType", ContentType(fileName)), ("BlobName", Limit(blob, 512)), ("Size", 1L),
            ("IsDefault", false), ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch),
            ("Type", string.Equals(Text(source, "SignType"), "DIGITAL", StringComparison.OrdinalIgnoreCase) ? 1 : 0),
            ("IsActive", Bool(source, "IsActive") ?? false), ("ProtectedSecret", null),
            ("ProviderCode", Limit(Text(source, "ProviderCode") ?? "", 256)), ("SealImageBase64", null),
            ("TokenRef", Limit(Text(source, "TokenRef") ?? "", 256)),
            ("ValidFrom", Date(source, "ValidFrom")), ("ValidTo", Date(source, "ValidTo")));
    }

    private static JsonObject Project(JsonObject source, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var code = Required(source, "Code", id);
        if (lookup.DuplicateProjectCodes.Contains(code)) code = $"{code}-{Short(id)}";
        return Audited(source, Object(
            ("Id", id), ("Code", code), ("Name", Required(source, "Name", id)),
            ("Description", Text(source, "Description")),
            ("StartDate", Date(source, "StartDate") ?? DateTime.UnixEpoch),
            ("EndDate", Date(source, "EndDate") ?? Date(source, "StartDate") ?? DateTime.UnixEpoch),
            ("Status", Text(source, "Status") ?? "PLANNING"), ("OwnerDepartmentId", Guid(source, "OwnerDepartmentId")),
            ("OwnerUserId", Guid(source, "OwnerId") ?? Guid(source, "CreatorId") ?? Guid(source, "Id"))));
    }

    private static JsonObject ProjectMember(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("ProjectId", Guid(source, "ProjectId")), ("UserId", Guid(source, "UserId")),
        ("Role", Limit(Text(source, "MemberRole") ?? "Member", 128)), ("IsActive", !(Bool(source, "IsDeleted") ?? false)));

    private static JsonObject ProjectTask(JsonObject source, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var projectId = Guid(source, "ProjectId");
        var parentTaskId = Guid(source, "ParentTaskId");
        if (!parentTaskId.HasValue && projectId.HasValue)
        {
            var parentCode = Text(source, "ParentTaskId");
            if (parentCode is not null && lookup.ProjectTaskIdByProjectCode.TryGetValue($"{projectId}|{parentCode}", out var mappedParent))
                parentTaskId = mappedParent;
        }
        var code = Required(source, "Code", id);
        if (projectId.HasValue && lookup.DuplicateProjectTaskCodes.Contains($"{projectId}|{code}"))
            code = $"{code}-{Short(id)}";
        return Audited(source, Object(
            ("Id", id), ("ProjectId", projectId), ("ParentTaskId", parentTaskId), ("Code", code),
            ("Title", Required(source, "Title", id)), ("Description", Text(source, "Description")),
            ("StartDate", Date(source, "StartDate") ?? DateTime.UnixEpoch),
            ("DueDate", Date(source, "DueDate") ?? Date(source, "StartDate") ?? DateTime.UnixEpoch),
            ("Priority", Text(source, "Priority") ?? "NORMAL"), ("Status", Text(source, "Status") ?? "TODO"),
            ("ProgressPercent", Math.Clamp(Int(source, "ProgressPercent") ?? 0, 0, 100))));
    }

    private static JsonObject ProjectTaskAssignment(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("ProjectTaskId", Guid(source, "ProjectTaskId")),
        ("UserId", Guid(source, "UserId")), ("AssignmentType", Limit(Text(source, "AssignmentRole") ?? "ASSIGNEE", 128)));

    private static JsonObject ProjectTaskDocument(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("ProjectTaskId", Guid(source, "ProjectTaskId")),
        ("DocumentId", Guid(source, "DocumentId")), ("DocumentCode", null));

    private static JsonObject CalendarEvent(JsonObject source) => Audited(source, Object(
        ("Id", Guid(source, "Id")), ("Title", Required(source, "Title", Guid(source, "Id"))),
        ("Description", Text(source, "Description")), ("StartTime", Date(source, "StartTime") ?? DateTime.UnixEpoch),
        ("EndTime", Date(source, "EndTime") ?? Date(source, "StartTime") ?? DateTime.UnixEpoch),
        ("AllDay", Bool(source, "AllDay") ?? false), ("EventType", Text(source, "EventType") ?? "LEGACY"),
        ("Location", Text(source, "Location")), ("RelatedType", Text(source, "RelatedType") ?? "LEGACY"),
        ("RelatedId", Text(source, "RelatedId")), ("Visibility", Text(source, "Visibility") ?? "PRIVATE"),
        ("OwnerUserId", Guid(source, "OwnerId") ?? Guid(source, "CreatorId") ?? Guid(source, "Id"))));

    private static JsonObject CalendarParticipant(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("CalendarEventId", Guid(source, "CalendarEventId")),
        ("UserId", Guid(source, "IdentityUserId")));

    private static JsonObject SurveySession(JsonObject source)
    {
        var id = Guid(source, "Id");
        var start = Date(source, "SurveyTime", "CreationTime") ?? DateTime.UnixEpoch;
        return Audited(source, Object(
            ("Id", id), ("Code", $"SURVEY-{Short(id)}"),
            ("Name", Limit(Text(source, "SessionDisplay") ?? "Legacy survey", 256)),
            ("StartsAt", start), ("EndsAt", start.AddHours(1)), ("Status", "Completed"),
            ("LocationId", Guid(source, "SurveyLocationId")), ("OwnerUserId", Guid(source, "CreatorId") ?? id),
            ("IsPublic", true), ("FullName", Text(source, "FullName")), ("PhoneNumber", Text(source, "PhoneNumber")),
            ("PatientCode", Text(source, "PatientCode")), ("SurveyTime", Date(source, "SurveyTime")),
            ("DeviceType", Text(source, "DeviceType")), ("Note", Text(source, "Note")),
            ("SessionDisplay", Text(source, "SessionDisplay"))));
    }

    private static JsonObject SurveyCriteria(JsonObject source) => Audited(source, Object(
        ("Id", Guid(source, "Id")), ("Code", Required(source, "Code", Guid(source, "Id"))),
        ("Name", Required(source, "Name", Guid(source, "Id"))), ("SortOrder", Int(source, "DisplayOrder") ?? 0),
        ("IsActive", Bool(source, "IsActive") ?? true), ("LocationId", Guid(source, "SurveyLocationId")),
        ("Image", Text(source, "Image"))));

    private static JsonObject SurveyLocation(JsonObject source) => Audited(source, Object(
        ("Id", Guid(source, "Id")), ("Code", Required(source, "Code", Guid(source, "Id"))),
        ("Name", Required(source, "Name", Guid(source, "Id"))), ("OrganizationUnitId", null),
        ("IsActive", Bool(source, "IsActive") ?? true), ("Description", Text(source, "Description"))));

    private static JsonObject SurveyResult(JsonObject source) => Audited(source, Object(
        ("Id", Guid(source, "Id")), ("SessionId", Guid(source, "SurveySessionId")),
        ("CriteriaId", Guid(source, "SurveyCriteriaId")), ("RespondentUserId", Guid(source, "UserId")),
        ("Score", Int(source, "Rating") ?? 0), ("Comment", Text(source, "Comment"))));

    private static JsonObject SurveyFile(JsonObject source)
    {
        var id = Guid(source, "Id");
        var blob = Text(source, "FilePath") ?? $"legacy/survey/{id:N}";
        var fileName = Text(source, "FileName") ?? Path.GetFileName(blob);
        return Object(
            ("Id", id), ("SessionId", Guid(source, "SurveySessionId")), ("BlobName", Limit(blob, 512)),
            ("FileName", Limit(fileName, 256)), ("ContentType", Limit(Text(source, "MimeType") ?? ContentType(fileName), 128)),
            ("Size", Math.Max(0L, Long(source, "FileSize") ?? 0)),
            ("UploadedByUserId", Guid(source, "CreatorId") ?? id));
    }

    private static JsonObject Report(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("Dimension", "legacy"), ("Key", Guid(source, "Id")?.ToString()),
        ("Label", Limit(Text(source, "Name") ?? "Legacy report", 256)),
        ("Value", Int(source, "SortOrder") ?? 0), ("RefreshedAt", Date(source, "CreationTime") ?? DateTime.UnixEpoch));

    private static JsonObject Conversation(JsonObject source)
    {
        var id = Guid(source, "Id");
        var projectId = Guid(source, "ProjectId");
        var taskId = Guid(source, "TaskId");
        var type = taskId.HasValue ? 3 : projectId.HasValue ? 2 : 1;
        return Audited(source, Object(
        ("Id", id), ("Type", type), ("Name", Text(source, "Name")),
        ("Description", Text(source, "Description")), ("ProjectId", taskId.HasValue ? null : projectId),
        ("TaskId", taskId), ("LastMessage", Text(source, "LastMessage")),
        ("LastMessageAt", Date(source, "LastMessageDate")), ("DirectUserHighId", null), ("DirectUserLowId", null),
        ("ExtraProperties", "{}"), ("ConcurrencyStamp", StableStamp(id)),
        ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch)));
    }

    private static JsonObject ConversationMember(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("ConversationId", Guid(source, "ConversationId")),
        ("UserId", Guid(source, "UserId")), ("Role", string.Equals(Text(source, "Role"), "Admin", StringComparison.OrdinalIgnoreCase) ? 1 : 0),
        ("UnreadCount", Int(source, "UnreadMessageCount") ?? 0), ("IsPinned", Bool(source, "IsPinned") ?? false),
        ("LastReadAt", null), ("CreationTime", Date(source, "JoinedDate") ?? DateTime.UnixEpoch), ("CreatorId", null));

    private static JsonObject ChatMessage(JsonObject source) => Audited(source, Object(
        ("Id", Guid(source, "Id")), ("ConversationId", Guid(source, "ConversationId")),
        ("SenderUserId", Guid(source, "CreatorId")), ("ClientMessageId", null),
        ("Text", Limit(Text(source, "Text") ?? "", 4000)), ("ReplyToMessageId", Guid(source, "ReplyToMessageId")),
        ("ForwardedFromMessageId", Guid(source, "ForwardedFromMessageId")),
        ("IsPinned", Bool(source, "IsPinned") ?? false), ("PinnedAt", Date(source, "PinnedDate")),
        ("PinnedByUserId", Guid(source, "PinnedByUserId")), ("IsDeleted", false)));

    private static JsonObject ChatAttachment(JsonObject source, MigrationLookup lookup)
    {
        var id = Guid(source, "Id");
        var messageId = Guid(source, "MessageId");
        Guid? conversationId = messageId.HasValue ? lookup.MessageConversationById.GetValueOrDefault(messageId.Value) : null;
        var fileName = Text(source, "FileName") ?? "legacy-file";
        return Object(
            ("Id", id), ("ConversationId", conversationId), ("UploadedByUserId", Guid(source, "CreatorId") ?? id),
            ("MessageId", messageId), ("BlobName", Limit(Text(source, "FilePath") ?? $"legacy/chat/{id:N}", 512)),
            ("FileName", Limit(fileName, 256)), ("ContentType", Limit(Text(source, "ContentType") ?? ContentType(fileName), 128)),
            ("Size", Math.Max(0L, Long(source, "FileSize") ?? 0)), ("Kind", AttachmentKind(fileName)),
            ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch), ("CreatorId", Guid(source, "CreatorId")));
    }

    private static JsonObject InboxMessage(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("EventName", "Legacy.ChatUserMessage"),
        ("ProcessedAt", Date(source, "ReadTime") ?? DateTime.UnixEpoch));

    private static JsonObject Notification(JsonObject source) => Audited(source, Object(
        ("Id", Guid(source, "Id")), ("Title", Limit(Text(source, "Title") ?? "Legacy notification", 256)),
        ("Body", Limit(Text(source, "Content") ?? "", 2000)),
        ("Link", Text(source, "RelatedId")), ("Status", 0)));

    private static JsonObject NotificationReceiver(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("NotificationId", Guid(source, "NotificationId")),
        ("UserId", Guid(source, "IdentityUserId")), ("IsRead", Bool(source, "IsRead") ?? false),
        ("ReadAt", Date(source, "ReadAt")), ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch),
        ("CreatorId", Guid(source, "CreatorId")));

    private static JsonObject PushDeviceToken(JsonObject source) => Object(
        ("Id", Guid(source, "Id")), ("UserId", Guid(source, "UserId")),
        ("Token", Limit(Text(source, "FcmToken") ?? "legacy-token", 2048)),
        ("Platform", Limit(Text(source, "Platform") ?? "unknown", 128)),
        ("IsActive", Bool(source, "IsActive") ?? true), ("ExtraProperties", Text(source, "ExtraProperties") ?? "{}"),
        ("ConcurrencyStamp", Text(source, "ConcurrencyStamp") ?? StableStamp(Guid(source, "Id"))),
        ("CreationTime", Date(source, "CreationTime") ?? DateTime.UnixEpoch), ("CreatorId", Guid(source, "CreatorId")));

    private static JsonObject ProjectCommon(JsonObject source, params string[]? columns)
    {
        if (columns is null || columns.Length == 0) return (JsonObject)source.DeepClone();
        var result = new JsonObject();
        foreach (var column in columns)
            if (source[column] is not null) result[column] = source[column]!.DeepClone();
        return result;
    }

    private static JsonObject Audited(JsonObject source, JsonObject result)
    {
        AddAudit(result, source, includeFullAudit: true);
        return result;
    }

    private static void AddAudit(JsonObject result, JsonObject source, bool includeFullAudit)
    {
        foreach (var name in new[] { "ExtraProperties", "ConcurrencyStamp", "CreationTime", "CreatorId", "LastModificationTime", "LastModifierId" })
            if (source[name] is not null) result[name] = source[name]!.DeepClone();
        if (!includeFullAudit) return;
        foreach (var name in new[] { "IsDeleted", "DeleterId", "DeletionTime" })
            if (source[name] is not null) result[name] = source[name]!.DeepClone();
    }

    private static void RemapTargetUsers(JsonObject values, TableMigrationSpec table,
        IReadOnlyDictionary<string, Guid?> userMap)
    {
        foreach (var name in values.Select(x => x.Key).ToArray())
        {
            if (!name.Contains("User", StringComparison.OrdinalIgnoreCase)
                && name is not ("CreatorId" or "LastModifierId" or "DeleterId")) continue;
            if (values[name] is not JsonValue) continue;
            var text = MigrationLookup.TextValue(values, name);
            if (text is not null && userMap.TryGetValue(text, out var mapped) && mapped.HasValue) values[name] = mapped.Value;
        }
    }

    private static string RemapGuidArray(string? json, IReadOnlyDictionary<string, Guid?> userMap)
    {
        if (string.IsNullOrWhiteSpace(json)) return "[]";
        try
        {
            var array = JsonNode.Parse(json) as JsonArray;
            if (array is null) return "[]";
            var result = new JsonArray();
            foreach (var item in array)
            {
                if (!System.Guid.TryParse(item?.ToString(), out var legacy)) continue;
                result.Add(userMap.TryGetValue(legacy.ToString(), out var mapped) && mapped.HasValue ? mapped.Value : legacy);
            }
            return result.ToJsonString();
        }
        catch (JsonException) { return "[]"; }
    }

    private static JsonObject Object(params (string Name, object? Value)[] fields)
    {
        var result = new JsonObject();
        foreach (var (name, value) in fields) result[name] = ToNode(value);
        return result;
    }

    private static JsonNode? Node(JsonObject source, params string[] names) => names.Select(x => source[x]).FirstOrDefault(x => x is not null)?.DeepClone();
    private static string? Text(JsonObject source, params string[] names) => MigrationLookup.TextValue(source, names);
    private static Guid? Guid(JsonObject source, params string[] names) => MigrationLookup.GuidValue(source, names);
    private static int? Int(JsonObject source, params string[] names) => MigrationLookup.IntValue(source, names);
    private static long? Long(JsonObject source, params string[] names) => long.TryParse(Text(source, names), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static bool? Bool(JsonObject source, params string[] names) => MigrationLookup.BoolValue(source, names);

    private static DateTime? Date(JsonObject source, params string[] names)
    {
        var node = names.Select(x => source[x]).FirstOrDefault(x => x is not null);
        if (node is JsonValue value)
        {
            if (value.TryGetValue<DateTime>(out var date)) return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            if (value.TryGetValue<DateTimeOffset>(out var offset)) return offset.UtcDateTime;
        }
        return DateTime.TryParse(Text(source, names), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) ? parsed : null;
    }

    private static object? ToNodeValue(JsonNode? node) => node?.DeepClone();
    private static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node.DeepClone(),
        Guid guid => JsonValue.Create(guid),
        DateTime date => JsonValue.Create(DateTime.SpecifyKind(date, DateTimeKind.Utc)),
        DateTimeOffset offset => JsonValue.Create(offset),
        bool boolean => JsonValue.Create(boolean),
        int integer => JsonValue.Create(integer),
        long longValue => JsonValue.Create(longValue),
        decimal decimalValue => JsonValue.Create(decimalValue),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture))
    };

    private static string Required(JsonObject source, string name, Guid? id)
    {
        var value = Text(source, name)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? $"LEGACY-{Short(id)}" : value;
    }

    private static string? Limit(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= max ? value : value[..max];
    private static string Short(Guid? id) => id.HasValue ? id.Value.ToString("N")[..8] : "VALUE";
    private static string StableStamp(Guid? id) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"hcs-legacy:{id}"))).ToLowerInvariant()[..32];

    private static string Sha256(JsonObject source, string path)
    {
        var hash = Text(source, "Hash");
        if (!string.IsNullOrWhiteSpace(hash))
        {
            try
            {
                var bytes = Convert.FromBase64String(hash);
                if (bytes.Length == 32) return Convert.ToHexString(bytes).ToLowerInvariant();
            }
            catch (FormatException) { }
            if (hash.Length == 64 && hash.All(Uri.IsHexDigit)) return hash.ToLowerInvariant();
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"legacy-file:{path}"))).ToLowerInvariant();
    }

    private static string FileName(JsonObject source, string pathColumn) =>
        Path.GetFileName((Text(source, pathColumn) ?? "legacy-template").Replace('\\', '/'));

    private static string ContentType(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf", ".doc" => "application/msword", ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".gif" => "image/gif", ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private static int DocumentStatus(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "SUBMITTED" or "IN_PROGRESS" => 1,
        "IN_REVIEW" or "REVIEW" => 2,
        "APPROVED" or "COMPLETED" => 3,
        "REJECTED" => 4,
        "ARCHIVED" => 5,
        _ => 0
    };

    private static int WorkflowStatus(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "COMPLETED" or "DONE" => 1, "REJECTED" => 2, "CANCELLED" => 3, "RETURNED" => 4, _ => 0
    };

    private static int SigningKind(string? provider, string? signType) => provider?.Trim().ToUpperInvariant() switch
    {
        "REMOTE_CA" => 1, "HSM" => 2, "USB_TOKEN" => 3, _ => signType?.Equals("DIGITAL", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0
    };

    private static string NormalizeStepType(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "SIGN" => "SIGN", "VIEW" => "VIEW", _ => "PROCESS"
    };

    private static string NormalizeAssigneeType(string? value) => value?.Trim() switch
    {
        "RoleInSubmitterOu" => "RoleInSubmitterOu", "ScopedAssignee" => "ScopedAssignee", _ => "SpecificUser"
    };

    private static int AttachmentKind(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" => 1,
        ".mp4" or ".mov" or ".avi" => 2,
        ".mp3" or ".wav" or ".m4a" => 3,
        _ => 0
    };
}

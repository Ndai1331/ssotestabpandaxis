using System.Text.Json.Nodes;

namespace HCS.MigrationImporter;

public sealed class MigrationLookup
{
    public Dictionary<Guid, Guid> WorkflowDefinitionByWorkflowId { get; } = [];
    public Dictionary<Guid, Guid> WorkflowDefinitionByTemplateId { get; } = [];
    public Dictionary<Guid, int> WorkflowStepOrderById { get; } = [];
    public Dictionary<Guid, WorkflowAssignmentLookup> WorkflowAssignmentByStepId { get; } = [];
    public Dictionary<Guid, Guid> MessageConversationById { get; } = [];
    public Dictionary<string, HashSet<string>> TargetIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DuplicateDocumentNumbers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DuplicateProjectCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DuplicateWorkflowTemplateCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DuplicateProjectTaskCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DuplicatePositionCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Guid? FallbackDepartmentIdForUnits { get; private set; }
    public Dictionary<string, Guid> ProjectTaskIdByProjectCode { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<MigrationLookup> LoadAsync(ISourceSnapshot snapshot, CancellationToken cancellationToken)
    {
        var lookup = new MigrationLookup();
        if (snapshot is not IRawSourceSnapshot raw) return lookup;

        foreach (var table in MigrationManifest.Tables)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await foreach (var row in raw.ReadRawAsync(table.SourceTable, cancellationToken))
            {
                var id = TextValue(row.Values, "Id");
                if (id is not null) ids.Add(id);
            }
            lookup.TargetIds[$"{table.TargetDatabase}:{table.TargetTable}"] = ids;
        }

        var workflows = await ReadAsync(raw, "AppWorkflows", cancellationToken);
        foreach (var row in workflows)
        {
            var id = GuidValue(row.Values, "Id");
            var definitionId = GuidValue(row.Values, "WorkflowDefinitionId");
            if (id.HasValue && definitionId.HasValue) lookup.WorkflowDefinitionByWorkflowId[id.Value] = definitionId.Value;
        }

        var templates = await ReadAsync(raw, "AppWorkflowTemplates", cancellationToken);
        var templateCodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in templates)
        {
            var templateId = GuidValue(row.Values, "Id");
            var workflowId = GuidValue(row.Values, "WorkflowId");
            if (templateId.HasValue && workflowId.HasValue
                && lookup.WorkflowDefinitionByWorkflowId.TryGetValue(workflowId.Value, out var definitionId))
                lookup.WorkflowDefinitionByTemplateId[templateId.Value] = definitionId;

            var code = TextValue(row.Values, "Code");
            if (string.IsNullOrWhiteSpace(code)) continue;
            templateCodes[code] = templateCodes.TryGetValue(code, out var count) ? count + 1 : 1;
        }

        var steps = await ReadAsync(raw, "AppWorkflowStepTemplates", cancellationToken);
        foreach (var row in steps)
        {
            var id = GuidValue(row.Values, "Id");
            if (id.HasValue) lookup.WorkflowStepOrderById[id.Value] = IntValue(row.Values, "Order") ?? 0;
        }
        foreach (var group in steps
                     .Select(row => new
                     {
                         Row = row,
                         Id = GuidValue(row.Values, "Id"),
                         DefinitionId = GuidValue(row.Values, "WorkflowTemplateId") is { } templateId
                             ? (Guid?)lookup.WorkflowDefinitionByTemplateId.GetValueOrDefault(templateId)
                             : null,
                         Order = IntValue(row.Values, "Order") ?? 0
                     })
                     .Where(x => x.Id.HasValue && x.DefinitionId.HasValue)
                     .GroupBy(x => (x.DefinitionId!.Value, x.Order)))
        {
            var index = 0;
            foreach (var item in group.OrderBy(x => x.Id))
                lookup.WorkflowStepOrderById[item.Id!.Value] = group.Count() == 1
                    ? item.Order
                    : checked(Math.Max(0, item.Order) * 1000 + ++index);
        }

        foreach (var row in await ReadAsync(raw, "AppWorkflowStepAssignments", cancellationToken))
        {
            var stepId = GuidValue(row.Values, "StepId");
            if (!stepId.HasValue) continue;
            var value = new WorkflowAssignmentLookup(
                TextValue(row.Values, "AssigneeType"), GuidValue(row.Values, "RoleId"),
                GuidValue(row.Values, "DefaultUserId"), TextValue(row.Values, "DefaultUserIdsJson"),
                TextValue(row.Values, "OrganizationUnitIdsJson"),
                BoolValue(row.Values, "IsPrimary") ?? false);
            if (!lookup.WorkflowAssignmentByStepId.TryGetValue(stepId.Value, out var current)
                || (!current.IsPrimary && value.IsPrimary))
                lookup.WorkflowAssignmentByStepId[stepId.Value] = value;
        }

        foreach (var row in await ReadAsync(raw, "ChatMessages", cancellationToken))
        {
            var messageId = GuidValue(row.Values, "Id");
            var conversationId = GuidValue(row.Values, "ConversationId");
            if (messageId.HasValue && conversationId.HasValue) lookup.MessageConversationById[messageId.Value] = conversationId.Value;
        }

        var documentNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in await ReadAsync(raw, "AppDocuments", cancellationToken))
        {
            var number = TextValue(row.Values, "No");
            if (string.IsNullOrWhiteSpace(number)) continue;
            documentNumbers[number] = documentNumbers.TryGetValue(number, out var count) ? count + 1 : 1;
        }
        foreach (var pair in documentNumbers.Where(x => x.Value > 1)) lookup.DuplicateDocumentNumbers.Add(pair.Key);

        var projects = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in await ReadAsync(raw, "AppProjects", cancellationToken))
        {
            var code = TextValue(row.Values, "Code");
            if (!string.IsNullOrWhiteSpace(code))
                projects[code] = projects.TryGetValue(code, out var count) ? count + 1 : 1;
        }
        foreach (var pair in projects.Where(x => x.Value > 1)) lookup.DuplicateProjectCodes.Add(pair.Key);

        foreach (var pair in templateCodes.Where(x => x.Value > 1)) lookup.DuplicateWorkflowTemplateCodes.Add(pair.Key);

        var tasks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in await ReadAsync(raw, "AppProjectTasks", cancellationToken))
        {
            var projectId = TextValue(row.Values, "ProjectId");
            var code = TextValue(row.Values, "Code");
            if (!string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(code))
            {
                var key = $"{projectId}|{code}";
                tasks[key] = tasks.TryGetValue(key, out var count) ? count + 1 : 1;
            }
        }
        foreach (var pair in tasks.Where(x => x.Value > 1)) lookup.DuplicateProjectTaskCodes.Add(pair.Key);

        foreach (var row in await ReadAsync(raw, "AppProjectTasks", cancellationToken))
        {
            var id = GuidValue(row.Values, "Id");
            var projectId = TextValue(row.Values, "ProjectId");
            var code = TextValue(row.Values, "Code");
            if (id.HasValue && !string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(code))
            {
                var key = $"{projectId}|{code}";
                if (!lookup.ProjectTaskIdByProjectCode.TryGetValue(key, out var current)
                    || id.Value.CompareTo(current) < 0)
                    lookup.ProjectTaskIdByProjectCode[key] = id.Value;
            }
        }

        var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in await ReadAsync(raw, "AppPositions", cancellationToken))
        {
            var code = TextValue(row.Values, "Code");
            if (!string.IsNullOrWhiteSpace(code))
                positions[code] = positions.TryGetValue(code, out var count) ? count + 1 : 1;
        }
        foreach (var pair in positions.Where(x => x.Value > 1)) lookup.DuplicatePositionCodes.Add(pair.Key);

        var departmentRoots = await ReadAsync(raw, "AppDepartments", cancellationToken);
        lookup.FallbackDepartmentIdForUnits = departmentRoots
            .Where(x => GuidValue(x.Values, "ParentId") is null)
            .OrderBy(x => TextValue(x.Values, "Code"), StringComparer.OrdinalIgnoreCase)
            .Select(x => GuidValue(x.Values, "Id"))
            .FirstOrDefault(x => x.HasValue);

        return lookup;
    }

    private static async Task<List<RawSourceRow>> ReadAsync(IRawSourceSnapshot raw, string table,
        CancellationToken cancellationToken)
    {
        var rows = new List<RawSourceRow>();
        await foreach (var row in raw.ReadRawAsync(table, cancellationToken)) rows.Add(row);
        return rows;
    }

    internal static Guid? GuidValue(JsonObject values, params string[] names)
    {
        var text = TextValue(values, names);
        return Guid.TryParse(text, out var value) ? value : null;
    }

    internal static string? TextValue(JsonObject values, params string[] names)
    {
        foreach (var name in names)
        {
            if (values[name] is not JsonValue value || value.TryGetValue<bool>(out _)) continue;
            if (value.TryGetValue<string>(out var text)) return string.IsNullOrWhiteSpace(text) ? null : text;
            if (value.TryGetValue<Guid>(out var guid)) return guid.ToString();
            return value.ToString();
        }
        return null;
    }

    internal static bool? BoolValue(JsonObject values, params string[] names)
    {
        foreach (var name in names)
        {
            if (values[name] is JsonValue value && value.TryGetValue<bool>(out var result)) return result;
            if (bool.TryParse(TextValue(values, name), out result)) return result;
        }
        return null;
    }

    internal static int? IntValue(JsonObject values, params string[] names)
    {
        foreach (var name in names)
        {
            if (values[name] is JsonValue value && value.TryGetValue<int>(out var result)) return result;
            if (int.TryParse(TextValue(values, name), out result)) return result;
        }
        return null;
    }
}

public sealed record WorkflowAssignmentLookup(string? AssigneeType, Guid? RoleId, Guid? DefaultUserId,
    string? DefaultUserIdsJson, string? OrganizationUnitIdsJson, bool IsPrimary);

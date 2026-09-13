using System.Text.Json.Nodes;

namespace HCS.MigrationImporter;

public enum TargetDatabase { Identity, Organization, Document, Work, Collaboration }

public sealed record TableMigrationSpec(
    string SourceTable,
    TargetDatabase TargetDatabase,
    string TargetTable,
    IReadOnlyList<string> KeyColumns,
    IReadOnlyList<string>? UserIdColumns = null,
    IReadOnlyList<string>? BlobReferenceColumns = null,
    IReadOnlyList<RelationshipSpec>? Relationships = null,
    string TargetSchema = "public",
    bool ArchiveUniqueConflicts = false);

public sealed record RelationshipSpec(string Column, string ReferencedTable, string ReferencedColumn = "Id");
public sealed record SourceRow(string Table, JsonObject Values);
public sealed record KeycloakUser(Guid Id, string? Email, string? UserName, bool EmailVerified = true);
public sealed record BlobReference(string Bucket, string ObjectName, string Table, string RowKey);
public sealed record Checkpoint(string Table, string RowKey, string Checksum, DateTimeOffset CompletedAt);
public sealed record RawSourceRow(string Table, string RowKey, JsonObject Values);

public sealed class ReconciliationReport
{
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAt { get; set; }
    public bool DryRun { get; init; }
    public List<TableResult> Tables { get; } = [];
    public List<UserIssue> DuplicateUsers { get; } = [];
    public List<UserIssue> MissingUsers { get; } = [];
    public List<UserIssue> UnmatchedUsers { get; } = [];
    public List<RelationshipIssue> RelationshipIssues { get; } = [];
    public List<BlobIssue> BlobIssues { get; } = [];
    public List<ArchiveTableResult> ArchivedTables { get; } = [];
    public bool LegacyIdentityPreserved { get; set; }
}

public sealed record TableResult(string Table, long SourceRows, long UpsertedRows, long SkippedRows, string Checksum);
public sealed record UserIssue(string Table, string RowKey, string Column, string LegacyValue, string Reason);
public sealed record RelationshipIssue(string Table, string RowKey, string Column, string ReferencedTable, string Value);
public sealed record BlobIssue(string Table, string RowKey, string Bucket, string ObjectName, string Reason);
public sealed record ArchiveTableResult(string Table, TargetDatabase Database, long Rows);

public sealed record ImportOptions(bool DryRun, string OutputDirectory, IReadOnlySet<string>? RequestedTables = null,
    bool ArchiveSource = true);

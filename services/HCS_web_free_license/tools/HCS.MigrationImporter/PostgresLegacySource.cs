using System.Data;
using System.Text.Json.Nodes;
using Npgsql;

namespace HCS.MigrationImporter;

public sealed class PostgresLegacySource(string connectionString) : ILegacySource
{
    public async Task<ISourceSnapshot> OpenReadOnlySnapshotAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new Snapshot(connection, transaction);
    }

    private sealed class Snapshot(NpgsqlConnection connection, NpgsqlTransaction transaction) : ISourceSnapshot, IRawSourceSnapshot
    {
        public async IAsyncEnumerable<SourceRow> ReadAsync(TableMigrationSpec table,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            MigrationManifest.EnsureAllowed(table.SourceTable);
            var hierarchyColumn = table.SourceTable switch
            {
                "AbpOrganizationUnits" or "AppDepartments" => "ParentId",
                "AppProjectTasks" => "ParentTaskId",
                _ => null
            };
            var hierarchyReference = table.SourceTable == "AppProjectTasks" ? "Code" : "Id";
            var hierarchyScope = table.SourceTable == "AppProjectTasks"
                ? $" AND child.{Quote("ProjectId")} = hierarchy.{Quote("ProjectId")}"
                  + $" AND hierarchy.{Quote("Id")} = (SELECT parent.{Quote("Id")} FROM {Quote(table.SourceTable)} parent"
                  + $" WHERE parent.{Quote("ProjectId")} = child.{Quote("ProjectId")}"
                  + $" AND parent.{Quote("Code")} = child.{Quote(hierarchyColumn!)} ORDER BY parent.{Quote("Id")} LIMIT 1)"
                : "";
            var sql = hierarchyColumn is null
                ? $"SELECT * FROM {Quote(table.SourceTable)} ORDER BY {string.Join(", ", table.KeyColumns.Select(Quote))}"
                : $"""
                    WITH RECURSIVE hierarchy AS (
                      SELECT source.*, 0 AS "__migration_depth"
                      FROM {Quote(table.SourceTable)} source
                      WHERE source.{Quote(hierarchyColumn)} IS NULL
                      UNION ALL
                      SELECT child.*, hierarchy."__migration_depth" + 1
                      FROM {Quote(table.SourceTable)} child
                      JOIN hierarchy ON child.{Quote(hierarchyColumn)}::text = hierarchy.{Quote(hierarchyReference)}::text{hierarchyScope}
                    )
                    SELECT * FROM hierarchy
                    ORDER BY "__migration_depth", {string.Join(", ", table.KeyColumns.Select(Quote))}
                    """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var values = new JsonObject();
                for (var i = 0; i < reader.FieldCount; i++)
                    values[reader.GetName(i)] = ToJsonValue(reader.IsDBNull(i) ? null : reader.GetValue(i));
                yield return new SourceRow(table.SourceTable, values);
            }
        }

        public async Task<long> CountAsync(string table, CancellationToken cancellationToken)
        {
            MigrationManifest.EnsureAllowed(table);
            await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {Quote(table)}", connection, transaction);
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        }

        public async Task<IReadOnlyList<string>> ListTablesAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                ORDER BY table_name
                """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var tables = new List<string>();
            while (await reader.ReadAsync(cancellationToken)) tables.Add(reader.GetString(0));
            return tables;
        }

        public async IAsyncEnumerable<RawSourceRow> ReadRawAsync(string table,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var sql = $"SELECT ctid::text AS \"__legacy_ctid\", * FROM {Quote(table)} ORDER BY ctid";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var values = new JsonObject();
                for (var i = 1; i < reader.FieldCount; i++)
                    values[reader.GetName(i)] = ToJsonValue(reader.IsDBNull(i) ? null : reader.GetValue(i));
                yield return new RawSourceRow(table, reader.GetString(0), values);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }

        private static JsonNode? ToJsonValue(object? value) => value switch
        {
            null => null,
            Guid x => JsonValue.Create(x),
            DateTime x => JsonValue.Create(x.ToUniversalTime()),
            DateTimeOffset x => JsonValue.Create(x),
            byte[] x => JsonValue.Create(Convert.ToBase64String(x)),
            bool x => JsonValue.Create(x),
            short x => JsonValue.Create(x),
            int x => JsonValue.Create(x),
            long x => JsonValue.Create(x),
            decimal x => JsonValue.Create(x),
            double x => JsonValue.Create(x),
            float x => JsonValue.Create(x),
            _ => JsonValue.Create(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))
        };
    }

    internal static string Quote(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Any(x => !(char.IsLetterOrDigit(x) || x == '_')))
            throw new InvalidOperationException($"Unsafe PostgreSQL identifier: {identifier}");
        return $"\"{identifier}\"";
    }

    internal static string QuoteQualified(string schema, string table) => $"{Quote(schema)}.{Quote(table)}";
}

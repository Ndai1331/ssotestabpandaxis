using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;

namespace HCS.MigrationImporter;

public sealed class PostgresTargetStore(IReadOnlyDictionary<TargetDatabase, string> connectionStrings)
    : ITargetStore, ILegacyArchiveStore
{
    public async Task<Checkpoint?> GetCheckpointAsync(TargetDatabase database, string table, string rowKey,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(database, cancellationToken);
        await EnsureCheckpointTableAsync(connection, null, cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT checksum, completed_at FROM hcs_migration_checkpoints WHERE source_table = @table AND row_key = @key",
            connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("key", rowKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new Checkpoint(table, rowKey, reader.GetString(0), reader.GetFieldValue<DateTimeOffset>(1))
            : null;
    }

    public async Task UpsertAsync(TableMigrationSpec table, SourceRow row, string rowKey, string checksum,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(table.TargetDatabase, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureCheckpointTableAsync(connection, transaction, cancellationToken);

        var targetColumns = await ReadColumnsAsync(connection, transaction, table.TargetSchema, table.TargetTable,
            cancellationToken);
        if (targetColumns.Count == 0)
            throw new InvalidOperationException($"Target table does not exist: {table.TargetSchema}.{table.TargetTable}");

        var values = row.Values
            .Where(x => targetColumns.ContainsKey(x.Key))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var column in targetColumns.Values.Where(x => x.Required))
        {
            if (!values.TryGetValue(column.Name, out var value) || !IsNullOrEmpty(value)) continue;
            if (!string.IsNullOrWhiteSpace(column.DefaultExpression)) values.Remove(column.Name);
            else values[column.Name] = DefaultValue(column);
        }

        var columns = values.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var keys = table.KeyColumns.Where(targetColumns.ContainsKey).ToArray();
        if (keys.Length == 0 || keys.Any(x => !values.ContainsKey(x)))
            throw new InvalidOperationException($"Target key is not present for {table.TargetSchema}.{table.TargetTable}: {rowKey}");

        var quotedColumns = string.Join(", ", columns.Select(PostgresLegacySource.Quote));
        var parameters = string.Join(", ", columns.Select((_, i) => $"@p{i}"));
        var updates = columns.Except(keys, StringComparer.OrdinalIgnoreCase).ToArray();
        var conflict = updates.Length == 0
            ? "DO NOTHING"
            : "DO UPDATE SET " + string.Join(", ", updates.Select(x =>
                $"{PostgresLegacySource.Quote(x)} = EXCLUDED.{PostgresLegacySource.Quote(x)}"));
        var sql = $"INSERT INTO {PostgresLegacySource.QuoteQualified(table.TargetSchema, table.TargetTable)} ({quotedColumns}) VALUES ({parameters}) ON CONFLICT ({string.Join(", ", keys.Select(PostgresLegacySource.Quote))}) {conflict}";

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            for (var i = 0; i < columns.Length; i++) AddParameter(command, $"p{i}", values[columns[i]], targetColumns[columns[i]]);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var checkpoint = new NpgsqlCommand("""
            INSERT INTO hcs_migration_checkpoints (source_table, row_key, checksum, completed_at)
            VALUES (@table, @key, @checksum, now())
            ON CONFLICT (source_table, row_key) DO UPDATE SET checksum = EXCLUDED.checksum, completed_at = EXCLUDED.completed_at
            """, connection, transaction))
        {
            checkpoint.Parameters.AddWithValue("table", table.SourceTable);
            checkpoint.Parameters.AddWithValue("key", rowKey);
            checkpoint.Parameters.AddWithValue("checksum", checksum);
            await checkpoint.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(TargetDatabase database, string table, string column, string value,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(database, cancellationToken);
        var sql = $"SELECT EXISTS(SELECT 1 FROM {PostgresLegacySource.QuoteQualified(SchemaFor(database), table)} WHERE {PostgresLegacySource.Quote(column)}::text = @value)";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Text) { Value = value });
        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    public async Task ArchiveAsync(TargetDatabase database, RawSourceRow row, string checksum,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(database, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureArchiveTableAsync(connection, transaction, cancellationToken);
        await using var command = new NpgsqlCommand("""
            INSERT INTO "legacy_migration"."source_rows" (source_table, row_key, checksum, payload, archived_at)
            VALUES (@table, @key, @checksum, @payload, now())
            ON CONFLICT (source_table, row_key) DO UPDATE SET checksum = EXCLUDED.checksum,
              payload = EXCLUDED.payload, archived_at = EXCLUDED.archived_at
            """, connection, transaction);
        command.Parameters.AddWithValue("table", row.Table);
        command.Parameters.AddWithValue("key", row.RowKey);
        command.Parameters.AddWithValue("checksum", checksum);
        command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = row.Values.ToJsonString() });
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<NpgsqlConnection> OpenAsync(TargetDatabase database, CancellationToken cancellationToken)
    {
        if (!connectionStrings.TryGetValue(database, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing target connection string for {database}");
        var connection = new NpgsqlConnection(value);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<Dictionary<string, ColumnInfo>> ReadColumnsAsync(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string schema, string table, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT column_name, data_type, udt_name, is_nullable, column_default
            FROM information_schema.columns
            WHERE table_schema = @schema AND table_name = @table
            ORDER BY ordinal_position
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            result[reader.GetString(0)] = new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3) == "NO", reader.IsDBNull(4) ? null : reader.GetString(4));
        }
        return result;
    }

    private static void AddParameter(NpgsqlCommand command, string name, JsonNode? node, ColumnInfo column)
    {
        if (column.UdtName == "jsonb")
        {
            command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
            { Value = node is null ? DBNull.Value : ToJsonText(node) });
            return;
        }
        command.Parameters.Add(new NpgsqlParameter(name, ValueFor(node, column)));
    }

    private static object ValueFor(JsonNode? node, ColumnInfo column)
    {
        if (node is null) return DBNull.Value;
        var text = node is JsonValue value && value.TryGetValue<string>(out var stringValue)
            ? stringValue : node.ToJsonString().Trim('"');
        if (string.IsNullOrWhiteSpace(text)) return DBNull.Value;

        try
        {
            return column.UdtName switch
            {
                "uuid" => Guid.Parse(text),
                "bool" => bool.Parse(text),
                "int2" => short.Parse(text, CultureInfo.InvariantCulture),
                "int4" => int.Parse(text, CultureInfo.InvariantCulture),
                "int8" => long.Parse(text, CultureInfo.InvariantCulture),
                "numeric" or "decimal" => decimal.Parse(text, CultureInfo.InvariantCulture),
                "date" => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                "timestamp" or "timestamptz" => DateTime.SpecifyKind(DateTime.Parse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal), DateTimeKind.Utc),
                _ => node is JsonObject or JsonArray ? node.ToJsonString() : text
            };
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException($"Cannot convert migration value for {column.Name} ({column.UdtName}): {text}", exception);
        }
    }

    private static bool IsNullOrEmpty(JsonNode? node)
        => node is null || node is JsonValue value && value.TryGetValue<string>(out var text) && string.IsNullOrWhiteSpace(text);

    private static JsonNode? DefaultValue(ColumnInfo column) => column.UdtName switch
    {
        "uuid" => Guid.Empty,
        "bool" => false,
        "int2" or "int4" or "int8" => 0,
        "numeric" or "decimal" => 0m,
        "jsonb" => "{}",
        "timestamp" or "timestamptz" or "date" => DateTime.UnixEpoch,
        _ when column.Name.Equals("ExtraProperties", StringComparison.OrdinalIgnoreCase) => "{}",
        _ when column.Name.Equals("ConcurrencyStamp", StringComparison.OrdinalIgnoreCase) => "migration-default",
        _ => "MIGRATION-DEFAULT"
    };

    private static string ToJsonText(JsonNode node)
    {
        var text = node is JsonValue value && value.TryGetValue<string>(out var stringValue)
            ? stringValue : node.ToJsonString();
        try
        {
            JsonNode.Parse(text);
            return text;
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(text);
        }
    }

    private static async Task EnsureCheckpointTableAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS hcs_migration_checkpoints (
              source_table varchar(128) NOT NULL,
              row_key varchar(1000) NOT NULL,
              checksum char(64) NOT NULL,
              completed_at timestamptz NOT NULL,
              PRIMARY KEY (source_table, row_key)
            )
            """, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureArchiveTableAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            CREATE SCHEMA IF NOT EXISTS "legacy_migration";
            CREATE TABLE IF NOT EXISTS "legacy_migration"."source_rows" (
              source_table varchar(256) NOT NULL,
              row_key varchar(256) NOT NULL,
              checksum char(64) NOT NULL,
              payload jsonb NOT NULL,
              archived_at timestamptz NOT NULL,
              PRIMARY KEY (source_table, row_key)
            )
            """, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string SchemaFor(TargetDatabase database) => database switch
    {
        TargetDatabase.Organization => "hcs_organization",
        TargetDatabase.Document => "document",
        TargetDatabase.Work => "hcs_work",
        _ => "public"
    };

    private sealed record ColumnInfo(string Name, string DataType, string UdtName, bool Required, string? DefaultExpression);
}

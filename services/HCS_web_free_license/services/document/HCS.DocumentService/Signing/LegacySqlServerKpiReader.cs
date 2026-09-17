using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace HCS.DocumentService.Signing;

public sealed class SigningKpiSourceResult
{
    public SigningKpiMetricsDto Overall { get; set; } = new();
    public List<SigningKpiGroupRowDto> Groups { get; set; } = [];
    public bool Available { get; set; }
    public string? Error { get; set; }
}

public interface ILegacySqlServerKpiReader
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    Task<SigningKpiSourceResult> GetAsync(GetSigningKpiInput input, CancellationToken cancellationToken = default);

    Task<(bool Available, string? Error, List<SigningKpiDetailRowDto> Rows)> GetDetailRowsAsync(
        GetSigningKpiInput input,
        int maxRows = 10_000,
        CancellationToken cancellationToken = default);
}

public sealed class LegacySqlServerKpiReader(
    IOptions<LegacySigningReportOptions> options,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContext,
    IConfiguration configuration,
    ILogger<LegacySqlServerKpiReader> logger) : ILegacySqlServerKpiReader
{
    private const string Source = "QLDH_MSSQL";
    private readonly LegacySigningReportOptions _options = options.Value;

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var fromPlatform = await TryGetFromPlatformAsync(cancellationToken);
        return fromPlatform?.Enabled ?? true;
    }

    public async Task<SigningKpiSourceResult> GetAsync(
        GetSigningKpiInput input,
        CancellationToken cancellationToken = default)
    {
        var connectionString = await ResolveConnectionStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new SigningKpiSourceResult
            {
                Available = false,
                Error = "SQL Server connection string is not configured."
            };
        }

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            var overall = await QueryOverallAsync(conn, input, cancellationToken);
            var groups = await QueryGroupsAsync(conn, input, cancellationToken);
            return new SigningKpiSourceResult
            {
                Available = true,
                Overall = overall,
                Groups = groups
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Legacy SQL Server KPI query failed");
            return new SigningKpiSourceResult
            {
                Available = false,
                Error = exception.Message
            };
        }
    }

    public async Task<(bool Available, string? Error, List<SigningKpiDetailRowDto> Rows)> GetDetailRowsAsync(
        GetSigningKpiInput input,
        int maxRows = 10_000,
        CancellationToken cancellationToken = default)
    {
        var connectionString = await ResolveConnectionStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, "SQL Server connection string is not configured.", []);
        }

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            const string sql = """
                SELECT TOP (@MaxRows)
                    a.Code AS Code,
                    a.Name AS Title,
                    g.Code AS GroupCode,
                    g.Name AS GroupName,
                    u.UserName AS SubmitterName,
                    a.ApprovalDate AS SubmittedAt,
                    a.PreferredDate AS DeadlineAt,
                    a.SignedDate AS CompletedAt,
                    a.Status AS Status,
                    a.MainContent AS Note,
                    CASE
                        WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.ApprovalDate IS NOT NULL
                        THEN CAST(DATEDIFF(SECOND, a.ApprovalDate, a.SignedDate) AS FLOAT) / 3600.0
                        ELSE NULL END AS ProcessingHours,
                    CASE
                        WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                             AND a.SignedDate <= a.PreferredDate THEN 1
                        WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                             AND a.SignedDate > a.PreferredDate THEN 0
                        ELSE NULL END AS IsOnTime,
                    (
                        SELECT STRING_AGG(CONCAT(CAST(s.[Index] AS varchar(10)), ': ', ISNULL(su.UserName, '?')), ' | ')
                               WITHIN GROUP (ORDER BY s.[Index])
                        FROM SignatureApprovalSigners s
                        LEFT JOIN Users su ON su.Id = s.UserId AND su.IsDelete = 0
                        WHERE s.ApprovalId = a.Id AND s.IsDeleted = 0
                    ) AS SignerChain
                FROM SignatureApprovals a
                LEFT JOIN SignGroups g ON g.Id = a.SignGroupId AND g.IsDeleted = 0
                LEFT JOIN Users u ON u.Id = a.UserId AND u.IsDelete = 0
                WHERE a.IsDelete = 0
                  AND (@SourceYear IS NULL OR YEAR(a.CreatedDate) = @SourceYear)
                  AND (@From IS NULL OR a.ApprovalDate >= @From)
                  AND (@To IS NULL OR a.ApprovalDate <= @To)
                ORDER BY a.ApprovalDate DESC;
                """;

            await using var cmd = CreateCommand(conn, sql, input);
            cmd.Parameters.Add(new SqlParameter("@MaxRows", SqlDbType.Int) { Value = maxRows });
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var rows = new List<SigningKpiDetailRowDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var status = reader["Status"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Status"]);
                var (code, label) = MapLegacyStatus(status);
                bool? onTime = null;
                if (reader["IsOnTime"] != DBNull.Value)
                    onTime = Convert.ToInt32(reader["IsOnTime"]) == 1;

                rows.Add(new SigningKpiDetailRowDto
                {
                    Source = Source,
                    Code = reader["Code"] as string,
                    Title = reader["Title"] as string,
                    GroupCode = reader["GroupCode"] as string,
                    GroupName = reader["GroupName"] as string,
                    SubmitterName = reader["SubmitterName"] as string,
                    SubmittedAt = reader["SubmittedAt"] == DBNull.Value ? null : (DateTime?)reader["SubmittedAt"],
                    DeadlineAt = reader["DeadlineAt"] == DBNull.Value ? null : (DateTime?)reader["DeadlineAt"],
                    CompletedAt = reader["CompletedAt"] == DBNull.Value ? null : (DateTime?)reader["CompletedAt"],
                    StatusCode = code,
                    StatusLabel = label,
                    IsOnTime = onTime,
                    ProcessingHours = reader["ProcessingHours"] == DBNull.Value
                        ? null
                        : Math.Round(Convert.ToDouble(reader["ProcessingHours"]), 2),
                    SignerChain = reader["SignerChain"] as string,
                    Note = reader["Note"] as string
                });
            }

            return (true, null, rows);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Legacy SQL Server detail export query failed");
            return (false, exception.Message, []);
        }
    }

    internal static (string Code, string Label) MapLegacyStatus(int status) => status switch
    {
        1 => ("NEW", "Mới"),
        2 => ("IN_PROGRESS", "Đang xử lý"),
        3 => ("COMPLETED", "Đã phê duyệt"),
        4 => ("REJECTED", "Từ chối"),
        5 => ("CANCELLED", "Hủy"),
        _ => (status.ToString(), status.ToString())
    };

    private async Task<string?> ResolveConnectionStringAsync(CancellationToken cancellationToken)
    {
        var fromSettings = await TryGetFromPlatformAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromSettings?.ConnectionString))
            return fromSettings.ConnectionString;

        return string.IsNullOrWhiteSpace(_options.SqlServerConnectionString)
            ? null
            : _options.SqlServerConnectionString;
    }

    private async Task<PlatformConnectionResponse?> TryGetFromPlatformAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration["Services:Platform:BaseUrl"]))
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/internal/legacy-sql/connection-string");
            var token = httpContext.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.TryAddWithoutValidation("Authorization", token);

            using var response = await httpClientFactory.CreateClient("HCS.Platform")
                .SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<PlatformConnectionResponse>(
                JsonSerializerOptions,
                cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            logger.LogWarning(exception, "Could not load SQL connection string from system settings.");
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record PlatformConnectionResponse(bool? Enabled, string? ConnectionString);

    private async Task<SigningKpiMetricsDto> QueryOverallAsync(
        SqlConnection conn, GetSigningKpiInput input, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COUNT(*) AS TotalCount,
                SUM(CASE WHEN a.Status = 1 THEN 1 ELSE 0 END) AS NewCount,
                SUM(CASE WHEN a.Status = 2 THEN 1 ELSE 0 END) AS InProgressCount,
                SUM(CASE WHEN a.Status = 3 THEN 1 ELSE 0 END) AS CompletedCount,
                SUM(CASE WHEN a.Status = 4 THEN 1 ELSE 0 END) AS RejectedCount,
                SUM(CASE WHEN a.Status = 5 THEN 1 ELSE 0 END) AS CancelledCount,
                AVG(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.ApprovalDate IS NOT NULL
                    THEN CAST(DATEDIFF(SECOND, a.ApprovalDate, a.SignedDate) AS FLOAT) / 3600.0
                    ELSE NULL END) AS AvgHours,
                SUM(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                         AND a.SignedDate <= a.PreferredDate THEN 1 ELSE 0 END) AS OnTimeCount,
                SUM(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                         AND a.SignedDate > a.PreferredDate THEN 1 ELSE 0 END) AS LateCount,
                SUM(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                    THEN 1 ELSE 0 END) AS CompletedWithDeadlineCount
            FROM SignatureApprovals a
            WHERE a.IsDelete = 0
              AND (@SourceYear IS NULL OR YEAR(a.CreatedDate) = @SourceYear)
              AND (@From IS NULL OR a.ApprovalDate >= @From)
              AND (@To IS NULL OR a.ApprovalDate <= @To);
            """;

        await using var cmd = CreateCommand(conn, sql, input);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? MapMetrics(reader)
            : new SigningKpiMetricsDto();
    }

    private async Task<List<SigningKpiGroupRowDto>> QueryGroupsAsync(
        SqlConnection conn, GetSigningKpiInput input, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                g.Code AS GroupCode,
                g.Name AS GroupName,
                COUNT(*) AS TotalCount,
                SUM(CASE WHEN a.Status = 1 THEN 1 ELSE 0 END) AS NewCount,
                SUM(CASE WHEN a.Status = 2 THEN 1 ELSE 0 END) AS InProgressCount,
                SUM(CASE WHEN a.Status = 3 THEN 1 ELSE 0 END) AS CompletedCount,
                SUM(CASE WHEN a.Status = 4 THEN 1 ELSE 0 END) AS RejectedCount,
                SUM(CASE WHEN a.Status = 5 THEN 1 ELSE 0 END) AS CancelledCount,
                AVG(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.ApprovalDate IS NOT NULL
                    THEN CAST(DATEDIFF(SECOND, a.ApprovalDate, a.SignedDate) AS FLOAT) / 3600.0
                    ELSE NULL END) AS AvgHours,
                SUM(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                         AND a.SignedDate <= a.PreferredDate THEN 1 ELSE 0 END) AS OnTimeCount,
                SUM(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                         AND a.SignedDate > a.PreferredDate THEN 1 ELSE 0 END) AS LateCount,
                SUM(CASE
                    WHEN a.Status = 3 AND a.SignedDate IS NOT NULL AND a.PreferredDate IS NOT NULL
                    THEN 1 ELSE 0 END) AS CompletedWithDeadlineCount
            FROM SignatureApprovals a
            LEFT JOIN SignGroups g ON g.Id = a.SignGroupId AND g.IsDeleted = 0
            WHERE a.IsDelete = 0
              AND (@SourceYear IS NULL OR YEAR(a.CreatedDate) = @SourceYear)
              AND (@From IS NULL OR a.ApprovalDate >= @From)
              AND (@To IS NULL OR a.ApprovalDate <= @To)
            GROUP BY g.Code, g.Name
            ORDER BY COUNT(*) DESC;
            """;

        await using var cmd = CreateCommand(conn, sql, input);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<SigningKpiGroupRowDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SigningKpiGroupRowDto
            {
                Source = Source,
                GroupCode = reader["GroupCode"] as string,
                GroupName = reader["GroupName"] as string ?? "Không nhóm",
                Metrics = MapMetrics(reader)
            });
        }

        return rows;
    }

    private SqlCommand CreateCommand(SqlConnection conn, string sql, GetSigningKpiInput input)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = Math.Max(5, _options.CommandTimeoutSeconds);
        cmd.Parameters.Add(new SqlParameter("@SourceYear", SqlDbType.Int)
        {
            Value = input.SourceYear.HasValue ? input.SourceYear.Value : DBNull.Value
        });
        cmd.Parameters.Add(new SqlParameter("@From", SqlDbType.DateTime2)
        {
            Value = input.SubmittedFrom.HasValue ? input.SubmittedFrom.Value : DBNull.Value
        });
        cmd.Parameters.Add(new SqlParameter("@To", SqlDbType.DateTime2)
        {
            Value = input.SubmittedTo.HasValue ? input.SubmittedTo.Value : DBNull.Value
        });
        return cmd;
    }

    private static SigningKpiMetricsDto MapMetrics(IDataRecord reader)
    {
        var dto = new SigningKpiMetricsDto
        {
            TotalCount = ReadLong(reader, "TotalCount"),
            NewCount = ReadLong(reader, "NewCount"),
            InProgressCount = ReadLong(reader, "InProgressCount"),
            CompletedCount = ReadLong(reader, "CompletedCount"),
            RejectedCount = ReadLong(reader, "RejectedCount"),
            CancelledCount = ReadLong(reader, "CancelledCount"),
            AverageProcessingHours = ReadNullableDouble(reader, "AvgHours"),
            OnTimeCount = ReadLong(reader, "OnTimeCount"),
            LateCount = ReadLong(reader, "LateCount"),
            CompletedWithDeadlineCount = ReadLong(reader, "CompletedWithDeadlineCount")
        };
        SigningKpiReportCalculator.FinalizeRates(dto);
        return dto;
    }

    private static long ReadLong(IDataRecord reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static double? ReadNullableDouble(IDataRecord reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDouble(reader.GetValue(ordinal));
    }
}

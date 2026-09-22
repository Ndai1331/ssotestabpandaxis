using HCS.DocumentService.Documents;
using HCS.DocumentService.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HCS.DocumentService.Signing;

public sealed class SigningKpiReportService(
    DocumentServiceDbContext db,
    ILegacySqlServerKpiReader legacyReader,
    ILogger<SigningKpiReportService> logger) : ISigningKpiReportService
{
    private const string HcsSource = "QLDH_PGSQL";

    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default) =>
        legacyReader.IsEnabledAsync(cancellationToken);

    public async Task<SigningKpiReportDto> GetAsync(GetSigningKpiInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        var hcsTask = LoadHcsAsync(input, cancellationToken);
        var legacyTask = legacyReader.GetAsync(input, cancellationToken);
        await Task.WhenAll(hcsTask, legacyTask);

        var hcs = await hcsTask;
        var legacy = await legacyTask;
        var combined = SigningKpiReportCalculator.Merge(
            legacy.Available ? legacy.Overall : new SigningKpiMetricsDto(),
            hcs.Available ? hcs.Overall : new SigningKpiMetricsDto());

        var groups = new List<SigningKpiGroupRowDto>();
        if (legacy.Available) groups.AddRange(legacy.Groups);
        if (hcs.Available) groups.AddRange(hcs.Groups);
        groups.Add(new SigningKpiGroupRowDto
        {
            Source = "Tổng hợp",
            GroupCode = "Tất cả",
            GroupName = "Tổng hợp",
            Metrics = combined
        });

        return new SigningKpiReportDto
        {
            Combined = combined,
            Legacy = legacy.Available ? legacy.Overall : new SigningKpiMetricsDto(),
            Hcs = hcs.Available ? hcs.Overall : new SigningKpiMetricsDto(),
            Groups = groups,
            PieSlices = SigningKpiReportCalculator.BuildPie(combined),
            LegacyAvailable = legacy.Available,
            LegacyError = legacy.Error,
            HcsAvailable = hcs.Available,
            HcsError = hcs.Error
        };
    }

    public async Task<IReadOnlyList<SigningKpiDetailRowDto>> GetDetailsAsync(GetSigningKpiInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        var hcsTask = LoadRowsAsync(input, 10_000, cancellationToken);
        var legacyTask = legacyReader.GetDetailRowsAsync(input, 10_000, cancellationToken);
        await Task.WhenAll(hcsTask, legacyTask);

        var rows = new List<SigningKpiDetailRowDto>();
        var legacy = await legacyTask;
        if (legacy.Available) rows.AddRange(legacy.Rows);
        rows.AddRange(await hcsTask);
        return rows
            .OrderByDescending(x => x.SubmittedAt ?? DateTime.MinValue)
            .ThenBy(x => x.Source)
            .ToList();
    }

    private async Task<SigningKpiSourceResult> LoadHcsAsync(GetSigningKpiInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await LoadRowsAsync(input, null, cancellationToken);
            var metrics = SigningKpiReportCalculator.Calculate(rows);
            var groups = rows.GroupBy(x => new { x.GroupCode, x.GroupName })
                .OrderBy(x => x.Key.GroupName)
                .Select(x => new SigningKpiGroupRowDto
                {
                    Source = HcsSource,
                    GroupCode = x.Key.GroupCode,
                    GroupName = x.Key.GroupName,
                    Metrics = SigningKpiReportCalculator.Calculate(x.ToList())
                }).ToList();

            return new SigningKpiSourceResult
            {
                Available = true,
                Overall = metrics,
                Groups = groups
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "HCS signing KPI query failed");
            return new SigningKpiSourceResult
            {
                Available = false,
                Error = "Không thể đọc dữ liệu KPI ký số. Hãy kiểm tra kết nối Document service."
            };
        }
    }

    private async Task<List<SigningKpiDetailRowDto>> LoadRowsAsync(GetSigningKpiInput input, int? maxRows,
        CancellationToken cancellationToken)
    {
        var query = db.WorkflowInstances.AsNoTracking()
            .Include(x => x.Tasks)
            .Where(x => db.Documents.Any(document => document.Id == x.DocumentId &&
                document.SourceType == DocumentSourceType.Workflow));

        if (input.SourceYear is { } year)
        {
            var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddYears(1);
            query = query.Where(x => x.CreationTime >= start && x.CreationTime < end);
        }
        if (input.SubmittedFrom is { } from)
        {
            var fromUtc = ToUtc(from);
            query = query.Where(x => x.CreationTime >= fromUtc);
        }
        if (input.SubmittedTo is { } to)
        {
            var toUtc = ToUtc(to);
            query = query.Where(x => x.CreationTime <= toUtc);
        }
        query = query.OrderByDescending(x => x.CreationTime);
        if (maxRows is { } limit) query = query.Take(limit);

        var instances = await query.ToListAsync(cancellationToken);
        var documentIds = instances.Select(x => x.DocumentId).Distinct().ToArray();
        var documents = await db.Documents.AsNoTracking()
            .Where(x => documentIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number, x.Title, x.FromUserId })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var definitionIds = instances.Select(x => x.DefinitionId).Distinct().ToArray();
        var definitions = await db.WorkflowDefinitions.AsNoTracking()
            .Where(x => definitionIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var rows = new List<SigningKpiDetailRowDto>(instances.Count);
        foreach (var instance in instances)
        {
            if (!documents.TryGetValue(instance.DocumentId, out var document)) continue;
            definitions.TryGetValue(instance.DefinitionId, out var definition);
            var groupCode = definition?.Code ?? "UNKNOWN";
            var groupName = definition?.Name ?? "Quy trình không xác định";
            var tasks = instance.Tasks.OrderBy(x => x.CreationTime).ToList();
            var completedAt = instance.Status == WorkflowInstanceStatus.Completed
                ? tasks.Select(x => x.DecidedAt).Where(x => x.HasValue).Select(x => x!.Value)
                    .DefaultIfEmpty().Max()
                : (DateTime?)null;
            if (completedAt == DateTime.MinValue) completedAt = null;
            var deadlines = tasks.Select(x => x.DueAt).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            DateTime? deadlineAt = deadlines.Count == 0 ? null : deadlines.Max();
            var hours = completedAt.HasValue
                ? Math.Round((completedAt.Value - instance.CreationTime).TotalHours, 2)
                : (double?)null;
            if (hours is < 0) hours = null;

            rows.Add(new SigningKpiDetailRowDto
            {
                Source = HcsSource,
                Code = document.Number,
                Title = document.Title,
                GroupCode = groupCode,
                GroupName = groupName,
                SubmitterId = document.FromUserId,
                SubmittedAt = instance.CreationTime,
                DeadlineAt = deadlineAt,
                CompletedAt = completedAt,
                StatusCode = instance.Status.ToString(),
                StatusLabel = SigningKpiReportCalculator.StatusLabel(instance.Status),
                IsOnTime = completedAt is { } completed && deadlineAt is { } deadline
                    ? completed <= deadline
                    : null,
                ProcessingHours = hours,
                SignerChain = string.Join(" | ", tasks.Select(x => x.StepCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            });
        }
        return rows;
    }

    private static void Validate(GetSigningKpiInput input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (input.SourceYear is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(input.SourceYear), "Source year must be between 2000 and 2100.");
        if (input.SubmittedFrom.HasValue && input.SubmittedTo.HasValue &&
            ToUtc(input.SubmittedFrom.Value) > ToUtc(input.SubmittedTo.Value))
            throw new ArgumentException("SubmittedFrom must be earlier than SubmittedTo.");
    }

    /// <summary>
    /// Npgsql timestamptz rejects Unspecified/Local DateTime. Date pickers bind Unspecified;
    /// treat that calendar value as UTC so the selected day does not shift.
    /// </summary>
    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

internal static class SigningKpiReportCalculator
{
    public static SigningKpiMetricsDto Calculate(IReadOnlyCollection<SigningKpiDetailRowDto> rows)
    {
        var completed = rows.Where(x => x.StatusCode is nameof(WorkflowInstanceStatus.Completed) or "COMPLETED").ToList();
        var hours = completed.Where(x => x.ProcessingHours is >= 0).Select(x => x.ProcessingHours!.Value).ToList();
        var metrics = new SigningKpiMetricsDto
        {
            TotalCount = rows.Count,
            InProgressCount = rows.Count(x => x.StatusCode is nameof(WorkflowInstanceStatus.Running)
                or nameof(WorkflowInstanceStatus.Returned) or "IN_PROGRESS"),
            NewCount = rows.Count(x => x.StatusCode == "NEW"),
            CompletedCount = completed.Count,
            RejectedCount = rows.Count(x => x.StatusCode is nameof(WorkflowInstanceStatus.Rejected) or "REJECTED"),
            CancelledCount = rows.Count(x => x.StatusCode is nameof(WorkflowInstanceStatus.Cancelled) or "CANCELLED"),
            AverageProcessingHours = hours.Count == 0 ? null : Math.Round(hours.Average(), 2),
            OnTimeCount = completed.Count(x => x.IsOnTime == true),
            LateCount = completed.Count(x => x.IsOnTime == false),
            CompletedWithDeadlineCount = completed.Count(x => x.DeadlineAt.HasValue)
        };
        FinalizeRates(metrics);
        return metrics;
    }

    public static SigningKpiMetricsDto Merge(SigningKpiMetricsDto a, SigningKpiMetricsDto b)
    {
        var merged = new SigningKpiMetricsDto
        {
            TotalCount = a.TotalCount + b.TotalCount,
            NewCount = a.NewCount + b.NewCount,
            InProgressCount = a.InProgressCount + b.InProgressCount,
            CompletedCount = a.CompletedCount + b.CompletedCount,
            RejectedCount = a.RejectedCount + b.RejectedCount,
            CancelledCount = a.CancelledCount + b.CancelledCount,
            OnTimeCount = a.OnTimeCount + b.OnTimeCount,
            LateCount = a.LateCount + b.LateCount,
            CompletedWithDeadlineCount = a.CompletedWithDeadlineCount + b.CompletedWithDeadlineCount
        };

        double weightedSum = 0;
        long weightedCount = 0;
        if (a.AverageProcessingHours.HasValue && a.CompletedCount > 0)
        {
            weightedSum += a.AverageProcessingHours.Value * a.CompletedCount;
            weightedCount += a.CompletedCount;
        }
        if (b.AverageProcessingHours.HasValue && b.CompletedCount > 0)
        {
            weightedSum += b.AverageProcessingHours.Value * b.CompletedCount;
            weightedCount += b.CompletedCount;
        }

        merged.AverageProcessingHours = weightedCount == 0 ? null : weightedSum / weightedCount;
        FinalizeRates(merged);
        return merged;
    }

    public static void FinalizeRates(SigningKpiMetricsDto dto)
    {
        dto.OnTimeRatePercent = Percent(dto.OnTimeCount, dto.CompletedWithDeadlineCount);
        dto.CompletedRatePercent = Percent(dto.CompletedCount, dto.TotalCount);
        dto.InProgressRatePercent = Percent(dto.ProcessingIncludingNewCount, dto.TotalCount);
        dto.RejectedRatePercent = Percent(dto.RejectedCount, dto.TotalCount);
        dto.CancelledRatePercent = Percent(dto.CancelledCount, dto.TotalCount);
        if (dto.AverageProcessingHours.HasValue)
            dto.AverageProcessingHours = Math.Round(dto.AverageProcessingHours.Value, 2);
    }

    public static List<SigningKpiPieSliceDto> BuildPie(SigningKpiMetricsDto metrics) => new List<SigningKpiPieSliceDto>
    {
        new() { Label = "Đã phê duyệt", Value = metrics.CompletedCount, Color = "#1D9E75" },
        new() { Label = "Từ chối", Value = metrics.RejectedCount, Color = "#DC2626" },
        new() { Label = "Hủy", Value = metrics.CancelledCount, Color = "#9333EA" },
        new() { Label = "Đang xử lý", Value = metrics.ProcessingIncludingNewCount, Color = "#2563EB" }
    }.Where(x => x.Value > 0).ToList();

    public static string StatusLabel(WorkflowInstanceStatus status) => status switch
    {
        WorkflowInstanceStatus.Running => "Đang xử lý",
        WorkflowInstanceStatus.Returned => "Trả lại",
        WorkflowInstanceStatus.Completed => "Đã hoàn tất",
        WorkflowInstanceStatus.Rejected => "Từ chối",
        WorkflowInstanceStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    private static double? Percent(long value, long total) =>
        total == 0 ? null : Math.Round(value * 100d / total, 2);
}

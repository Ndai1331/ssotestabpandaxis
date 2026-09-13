using HCS.DocumentService.Documents;
using HCS.DocumentService.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HCS.DocumentService.Signing;

public sealed class SigningKpiReportService(DocumentServiceDbContext db, ILogger<SigningKpiReportService> logger) : ISigningKpiReportService
{
    private const string Source = "HCS";

    public async Task<SigningKpiReportDto> GetAsync(GetSigningKpiInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        try
        {
            var rows = await LoadRowsAsync(input, null, cancellationToken);
            var metrics = SigningKpiReportCalculator.Calculate(rows);
            var groups = rows.GroupBy(x => new { x.GroupCode, x.GroupName })
                .OrderBy(x => x.Key.GroupName)
                .Select(x => new SigningKpiGroupRowDto
                {
                    Source = Source,
                    GroupCode = x.Key.GroupCode,
                    GroupName = x.Key.GroupName,
                    Metrics = SigningKpiReportCalculator.Calculate(x.ToList())
                }).ToList();

            groups.Add(new SigningKpiGroupRowDto
            {
                Source = "Combined",
                GroupCode = Source,
                GroupName = "Tất cả quy trình trình ký",
                Metrics = metrics
            });

            return new SigningKpiReportDto
            {
                Combined = metrics,
                Hcs = metrics,
                Groups = groups,
                PieSlices = SigningKpiReportCalculator.BuildPie(metrics),
                HcsAvailable = true
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "HCS signing KPI query failed");
            return new SigningKpiReportDto
            {
                HcsAvailable = false,
                HcsError = "Không thể đọc dữ liệu KPI ký số. Hãy kiểm tra kết nối Document service."
            };
        }
    }

    public async Task<IReadOnlyList<SigningKpiDetailRowDto>> GetDetailsAsync(GetSigningKpiInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        return await LoadRowsAsync(input, 10_000, cancellationToken);
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
            var start = new DateTime(year, 1, 1);
            query = query.Where(x => x.CreationTime >= start && x.CreationTime < start.AddYears(1));
        }
        if (input.SubmittedFrom is { } from)
            query = query.Where(x => x.CreationTime >= from);
        if (input.SubmittedTo is { } to)
            query = query.Where(x => x.CreationTime <= to);
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
                Source = Source,
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
            input.SubmittedFrom > input.SubmittedTo)
            throw new ArgumentException("SubmittedFrom must be earlier than SubmittedTo.");
    }
}

internal static class SigningKpiReportCalculator
{
    public static SigningKpiMetricsDto Calculate(IReadOnlyCollection<SigningKpiDetailRowDto> rows)
    {
        var completed = rows.Where(x => x.StatusCode == nameof(WorkflowInstanceStatus.Completed)).ToList();
        var hours = completed.Where(x => x.ProcessingHours is >= 0).Select(x => x.ProcessingHours!.Value).ToList();
        var metrics = new SigningKpiMetricsDto
        {
            TotalCount = rows.Count,
            InProgressCount = rows.Count(x => x.StatusCode is nameof(WorkflowInstanceStatus.Running) or nameof(WorkflowInstanceStatus.Returned)),
            CompletedCount = completed.Count,
            RejectedCount = rows.Count(x => x.StatusCode == nameof(WorkflowInstanceStatus.Rejected)),
            CancelledCount = rows.Count(x => x.StatusCode == nameof(WorkflowInstanceStatus.Cancelled)),
            AverageProcessingHours = hours.Count == 0 ? null : Math.Round(hours.Average(), 2),
            OnTimeCount = completed.Count(x => x.IsOnTime == true),
            LateCount = completed.Count(x => x.IsOnTime == false),
            CompletedWithDeadlineCount = completed.Count(x => x.DeadlineAt.HasValue)
        };

        metrics.OnTimeRatePercent = Percent(metrics.OnTimeCount, metrics.CompletedWithDeadlineCount);
        metrics.CompletedRatePercent = Percent(metrics.CompletedCount, metrics.TotalCount);
        metrics.InProgressRatePercent = Percent(metrics.ProcessingIncludingNewCount, metrics.TotalCount);
        metrics.RejectedRatePercent = Percent(metrics.RejectedCount, metrics.TotalCount);
        metrics.CancelledRatePercent = Percent(metrics.CancelledCount, metrics.TotalCount);
        return metrics;
    }

    public static List<SigningKpiPieSliceDto> BuildPie(SigningKpiMetricsDto metrics) => new List<SigningKpiPieSliceDto>
    {
        new() { Label = "Đang xử lý", Value = metrics.ProcessingIncludingNewCount, Color = "#3498db" },
        new() { Label = "Đã hoàn tất", Value = metrics.CompletedCount, Color = "#2ecc71" },
        new() { Label = "Từ chối", Value = metrics.RejectedCount, Color = "#e74c3c" },
        new() { Label = "Đã hủy", Value = metrics.CancelledCount, Color = "#9333ea" }
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

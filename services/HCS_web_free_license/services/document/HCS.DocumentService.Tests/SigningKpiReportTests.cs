using HCS.DocumentService.Signing;
using HCS.DocumentService.Workflows;

namespace HCS.DocumentService.Tests;

public sealed class SigningKpiReportTests
{
    [Fact]
    public void Calculates_workflow_metrics_and_rates()
    {
        var rows = new List<SigningKpiDetailRowDto>
        {
            Row(nameof(WorkflowInstanceStatus.Completed), 4, true),
            Row(nameof(WorkflowInstanceStatus.Completed), 2, false),
            Row(nameof(WorkflowInstanceStatus.Running)),
            Row(nameof(WorkflowInstanceStatus.Returned)),
            Row(nameof(WorkflowInstanceStatus.Rejected)),
            Row(nameof(WorkflowInstanceStatus.Cancelled))
        };

        var metrics = SigningKpiReportCalculator.Calculate(rows);

        Assert.Equal(6, metrics.TotalCount);
        Assert.Equal(2, metrics.CompletedCount);
        Assert.Equal(2, metrics.ProcessingIncludingNewCount);
        Assert.Equal(1, metrics.RejectedCount);
        Assert.Equal(1, metrics.CancelledCount);
        Assert.Equal(2, metrics.CompletedWithDeadlineCount);
        Assert.Equal(1, metrics.OnTimeCount);
        Assert.Equal(50, metrics.OnTimeRatePercent);
        Assert.Equal(33.33, metrics.CompletedRatePercent);
        Assert.Equal(3, metrics.AverageProcessingHours);
    }

    [Fact]
    public void Merges_legacy_and_hcs_metrics()
    {
        var legacy = new SigningKpiMetricsDto
        {
            TotalCount = 10,
            NewCount = 2,
            InProgressCount = 3,
            CompletedCount = 4,
            RejectedCount = 1,
            AverageProcessingHours = 5,
            OnTimeCount = 3,
            LateCount = 1,
            CompletedWithDeadlineCount = 4
        };
        SigningKpiReportCalculator.FinalizeRates(legacy);

        var hcs = new SigningKpiMetricsDto
        {
            TotalCount = 6,
            InProgressCount = 2,
            CompletedCount = 3,
            CancelledCount = 1,
            AverageProcessingHours = 3,
            OnTimeCount = 2,
            LateCount = 1,
            CompletedWithDeadlineCount = 3
        };
        SigningKpiReportCalculator.FinalizeRates(hcs);

        var merged = SigningKpiReportCalculator.Merge(legacy, hcs);

        Assert.Equal(16, merged.TotalCount);
        Assert.Equal(7, merged.CompletedCount);
        Assert.Equal(2, merged.NewCount);
        Assert.Equal(5, merged.InProgressCount);
        Assert.Equal(7, merged.ProcessingIncludingNewCount);
        Assert.Equal(1, merged.RejectedCount);
        Assert.Equal(1, merged.CancelledCount);
        Assert.Equal(5, merged.OnTimeCount);
        Assert.Equal(7, merged.CompletedWithDeadlineCount);
        Assert.Equal(Math.Round((5d * 4 + 3d * 3) / 7, 2), merged.AverageProcessingHours);
        Assert.Equal(Math.Round(100d * 5 / 7, 2), merged.OnTimeRatePercent);
    }

    [Theory]
    [InlineData(1, "NEW", "Mới")]
    [InlineData(2, "IN_PROGRESS", "Đang xử lý")]
    [InlineData(3, "COMPLETED", "Đã phê duyệt")]
    [InlineData(4, "REJECTED", "Từ chối")]
    [InlineData(5, "CANCELLED", "Hủy")]
    public void Maps_legacy_sql_status(int status, string code, string label)
    {
        var mapped = LegacySqlServerKpiReader.MapLegacyStatus(status);
        Assert.Equal(code, mapped.Code);
        Assert.Equal(label, mapped.Label);
    }

    private static SigningKpiDetailRowDto Row(string status, double? hours = null, bool? onTime = null) =>
        new()
        {
            StatusCode = status,
            ProcessingHours = hours,
            IsOnTime = onTime,
            DeadlineAt = onTime.HasValue ? DateTime.UtcNow : null
        };
}

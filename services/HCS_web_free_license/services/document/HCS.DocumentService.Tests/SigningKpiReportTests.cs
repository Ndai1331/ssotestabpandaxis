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

    private static SigningKpiDetailRowDto Row(string status, double? hours = null, bool? onTime = null) =>
        new()
        {
            StatusCode = status,
            ProcessingHours = hours,
            IsOnTime = onTime,
            DeadlineAt = onTime.HasValue ? DateTime.UtcNow : null
        };
}

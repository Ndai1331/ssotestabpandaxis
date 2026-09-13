namespace HCS.DocumentService.Signing;

public sealed class GetSigningKpiInput
{
    public int? SourceYear { get; set; }
    public DateTime? SubmittedFrom { get; set; }
    public DateTime? SubmittedTo { get; set; }
}

public sealed class SigningKpiMetricsDto
{
    public long TotalCount { get; set; }
    public long NewCount { get; set; }
    public long InProgressCount { get; set; }
    public long CompletedCount { get; set; }
    public long RejectedCount { get; set; }
    public long CancelledCount { get; set; }
    public double? AverageProcessingHours { get; set; }
    public long OnTimeCount { get; set; }
    public long LateCount { get; set; }
    public long CompletedWithDeadlineCount { get; set; }
    public double? OnTimeRatePercent { get; set; }
    public double? CompletedRatePercent { get; set; }
    public double? InProgressRatePercent { get; set; }
    public double? RejectedRatePercent { get; set; }
    public double? CancelledRatePercent { get; set; }

    public long ProcessingIncludingNewCount => InProgressCount + NewCount;
}

public sealed class SigningKpiGroupRowDto
{
    public string Source { get; set; } = string.Empty;
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public SigningKpiMetricsDto Metrics { get; set; } = new();
}

public sealed class SigningKpiPieSliceDto
{
    public string Label { get; set; } = string.Empty;
    public long Value { get; set; }
    public string Color { get; set; } = string.Empty;
}

public sealed class SigningKpiReportDto
{
    public SigningKpiMetricsDto Combined { get; set; } = new();
    public SigningKpiMetricsDto Hcs { get; set; } = new();
    public List<SigningKpiGroupRowDto> Groups { get; set; } = [];
    public List<SigningKpiPieSliceDto> PieSlices { get; set; } = [];
    public bool HcsAvailable { get; set; }
    public string? HcsError { get; set; }
}

public sealed class SigningKpiDetailRowDto
{
    public string Source { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public Guid? SubmitterId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusLabel { get; set; }
    public bool? IsOnTime { get; set; }
    public double? ProcessingHours { get; set; }
    public string? SignerChain { get; set; }
    public string? Note { get; set; }
}

public interface ISigningKpiReportService
{
    Task<SigningKpiReportDto> GetAsync(GetSigningKpiInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SigningKpiDetailRowDto>> GetDetailsAsync(GetSigningKpiInput input,
        CancellationToken cancellationToken = default);
}

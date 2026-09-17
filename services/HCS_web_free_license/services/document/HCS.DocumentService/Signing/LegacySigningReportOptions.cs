namespace HCS.DocumentService.Signing;

public sealed class LegacySigningReportOptions
{
    public const string SectionName = "LegacySigningReport";

    /// <summary>
    /// SQL Server connection string for the internal SignatureApprovals database.
    /// Prefer environment configuration over committing credentials.
    /// </summary>
    public string? SqlServerConnectionString { get; set; }

    public int CommandTimeoutSeconds { get; set; } = 30;
}

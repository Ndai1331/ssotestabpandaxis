namespace HCS.Logging;

public class SeqOptions
{
    public string? ServerUrl { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 15;
}

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace HCS.Logging;

public static class HcsSeqLoggerConfiguration
{
    public const string ApplicationPropertyName = "Application";

    public static LoggerConfiguration WriteToHcsSeq(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string application)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(application);

        loggerConfiguration.Enrich.WithProperty(ApplicationPropertyName, application.Trim());

        var serverUrl = configuration["Seq:ServerUrl"];
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            return loggerConfiguration;
        }

        var apiKey = configuration["Seq:ApiKey"];
        loggerConfiguration.WriteTo.Seq(
            serverUrl.Trim(),
            restrictedToMinimumLevel: ParseLevel(configuration["Seq:MinimumLevel"]),
            apiKey: string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim());

        return loggerConfiguration;
    }

    private static LogEventLevel ParseLevel(string? value) =>
        Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level)
            ? level
            : LogEventLevel.Information;
}

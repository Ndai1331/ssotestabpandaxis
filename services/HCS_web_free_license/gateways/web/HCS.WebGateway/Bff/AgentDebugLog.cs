using System.Net.Http.Json;
using System.Text.Json;

namespace HCS.WebGateway;

internal static class AgentDebugLog
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMilliseconds(750)
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static Task WriteAsync(string hypothesisId, string location, string message, object data)
    {
        // #region agent log
        var payload = new
        {
            sessionId = "8e8299",
            runId = "post-fix",
            hypothesisId,
            location,
            message,
            data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        return HttpClient.PostAsJsonAsync(
                "http://host.docker.internal:7329/ingest/4bfbbb3c-06ec-4e7a-b999-a4ff03898d4f",
                payload,
                JsonOptions)
            .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
        // #endregion
    }
}

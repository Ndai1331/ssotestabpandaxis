using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace HCS.Logging;

public class SeqServiceLogQuery(
    IHttpClientFactory httpClientFactory,
    IOptions<SeqOptions> options,
    ILogger<SeqServiceLogQuery> logger) : IServiceLogQuery, ITransientDependency
{
    public const string HttpClientName = "HcsSeq";

    public async Task<IReadOnlyList<ServiceLogDto>> GetEventsAsync(
        string filter,
        int count,
        string? afterId,
        CancellationToken cancellationToken = default)
    {
        var serverUrl = options.Value.ServerUrl;
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new UserFriendlyException("Seq is not configured.");
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
        {
            throw new UserFriendlyException("Seq is not configured.");
        }

        var query = $"count={count}&render=true";
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query += "&filter=" + Uri.EscapeDataString(filter);
        }

        if (!string.IsNullOrWhiteSpace(afterId))
        {
            query += "&afterId=" + Uri.EscapeDataString(afterId);
        }

        try
        {
            using var response = await client.GetAsync("api/events?" + query, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Seq events request failed with {Status}: {Body}", (int)response.StatusCode, Truncate(body));
                throw new UserFriendlyException("Unable to read service logs from Seq.");
            }

            return ParseEvents(body);
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Seq events request failed.");
            throw new UserFriendlyException("Unable to read service logs from Seq.");
        }
    }

    internal static IReadOnlyList<ServiceLogDto> ParseEvents(string json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        if (!document.RootElement.TryGetProperty("Events", out var events) || events.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<ServiceLogDto>();
        foreach (var element in events.EnumerateArray())
        {
            var properties = ReadProperties(element);
            items.Add(new ServiceLogDto
            {
                Id = ReadString(element, "Id") ?? string.Empty,
                Timestamp = ReadTimestamp(element),
                Level = ReadString(element, "Level") ?? "Information",
                Application = ReadProperty(properties, "Application"),
                SourceContext = ReadProperty(properties, "SourceContext"),
                Message = ReadString(element, "RenderedMessage")
                    ?? ReadString(element, "MessageTemplate")
                    ?? string.Empty,
                Exception = ReadString(element, "Exception"),
                CorrelationId = ReadProperty(properties, "CorrelationId")
                    ?? ReadProperty(properties, "RequestId")
                    ?? ReadProperty(properties, "TraceId"),
                Properties = properties
            });
        }

        return items;
    }

    private static Dictionary<string, string> ReadProperties(JsonElement element)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!element.TryGetProperty("Properties", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return properties;
        }

        foreach (var property in list.EnumerateArray())
        {
            var name = ReadString(property, "Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var value = property.TryGetProperty("Value", out var raw) ? RenderValue(raw) : null;
            if (!string.IsNullOrWhiteSpace(value))
            {
                properties[name] = value;
            }
        }

        return properties;
    }

    private static DateTime ReadTimestamp(JsonElement element)
    {
        if (!element.TryGetProperty("Timestamp", out var timestamp))
        {
            return DateTime.UtcNow;
        }

        if (timestamp.ValueKind == JsonValueKind.String && DateTime.TryParse(timestamp.GetString(), out var parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        return DateTime.UtcNow;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => property.ToString()
        };
    }

    private static string? ReadProperty(IReadOnlyDictionary<string, string> properties, string name) =>
        properties.TryGetValue(name, out var value) ? value : null;

    private static string? RenderValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => value.GetRawText()
        };

    private static string Truncate(string value) =>
        value.Length <= 500 ? value : value[..500];
}

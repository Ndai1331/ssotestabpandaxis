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
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var payload = json.Trim();
        try
        {
            using var document = JsonDocument.Parse(payload);
            return ParseDocument(document.RootElement);
        }
        catch (JsonException)
        {
            return ParseNdjson(payload);
        }
    }

    private static IReadOnlyList<ServiceLogDto> ParseNdjson(string json)
    {
        var items = new List<ServiceLogDto>();
        foreach (var line in json.Split('\n'))
        {
            var value = line.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(value);
                items.AddRange(ParseDocument(document.RootElement));
            }
            catch (JsonException)
            {
            }
        }

        return items;
    }

    private static IReadOnlyList<ServiceLogDto> ParseDocument(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return ParseEventArray(root);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (TryGetProperty(root, "Events", out var events) && events.ValueKind == JsonValueKind.Array)
        {
            return ParseEventArray(events);
        }

        return IsEventObject(root) ? [ParseEvent(root)] : [];
    }

    private static bool IsEventObject(JsonElement element) =>
        TryGetProperty(element, "Id", out _) ||
        TryGetProperty(element, "@i", out _) ||
        TryGetProperty(element, "Timestamp", out _) ||
        TryGetProperty(element, "@t", out _) ||
        TryGetProperty(element, "RenderedMessage", out _) ||
        TryGetProperty(element, "MessageTemplate", out _) ||
        TryGetProperty(element, "@mt", out _) ||
        TryGetProperty(element, "@m", out _);

    private static List<ServiceLogDto> ParseEventArray(JsonElement events)
    {
        var items = new List<ServiceLogDto>();
        foreach (var element in events.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                items.Add(ParseEvent(element));
            }
        }

        return items;
    }

    private static ServiceLogDto ParseEvent(JsonElement element)
    {
        var properties = ReadProperties(element);
        return new ServiceLogDto
        {
            Id = ReadString(element, "Id", "@i") ?? string.Empty,
            Timestamp = ReadTimestamp(element),
            Level = ReadString(element, "Level", "@l") ?? "Information",
            Application = ReadProperty(properties, "Application"),
            SourceContext = ReadProperty(properties, "SourceContext"),
            Message = ReadString(element, "RenderedMessage", "MessageTemplate", "@m", "@mt")
                ?? string.Empty,
            Exception = ReadString(element, "Exception", "@x"),
            CorrelationId = ReadProperty(properties, "CorrelationId")
                ?? ReadProperty(properties, "RequestId")
                ?? ReadProperty(properties, "TraceId"),
            Properties = properties
        };
    }

    private static Dictionary<string, string> ReadProperties(JsonElement element)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (TryGetProperty(element, "Properties", out var list))
        {
            if (list.ValueKind == JsonValueKind.Array)
            {
                foreach (var property in list.EnumerateArray())
                {
                    var name = ReadString(property, "Name");
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var value = TryGetProperty(property, "Value", out var raw) ? RenderValue(raw) : null;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        properties[name] = value;
                    }
                }
            }
            else if (list.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in list.EnumerateObject())
                {
                    var value = RenderValue(property.Value);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        properties[property.Name] = value;
                    }
                }
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.StartsWith('@') ||
                property.Name.Equals("Properties", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("Links", StringComparison.OrdinalIgnoreCase) ||
                properties.ContainsKey(property.Name))
            {
                continue;
            }

            var value = RenderValue(property.Value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                properties[property.Name] = value;
            }
        }

        return properties;
    }

    private static DateTime ReadTimestamp(JsonElement element)
    {
        if (!TryGetProperty(element, "Timestamp", out var timestamp) &&
            !TryGetProperty(element, "@t", out timestamp))
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

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => property.ToString()
            };
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
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

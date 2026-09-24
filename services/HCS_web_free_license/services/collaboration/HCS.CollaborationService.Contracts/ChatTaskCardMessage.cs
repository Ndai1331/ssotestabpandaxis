using System.Text.Json;

namespace HCS.CollaborationService.Contracts;

public static class ChatTaskCardMessage
{
    public const string Prefix = "hcs.task:";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public sealed record Payload(
        Guid TaskId,
        string Title,
        Guid AssigneeUserId,
        string AssigneeName,
        DateOnly? DueDate,
        string? Note);

    public static string Format(Payload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Prefix + JsonSerializer.Serialize(payload, Json);
    }

    public static bool TryParse(string? text, out Payload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        try
        {
            payload = JsonSerializer.Deserialize<Payload>(text.AsSpan(Prefix.Length), Json);
            return payload is not null
                && payload.TaskId != Guid.Empty
                && !string.IsNullOrWhiteSpace(payload.Title);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string Preview(string? text)
    {
        return TryParse(text, out var payload) && payload is not null
            ? payload.Title
            : text ?? string.Empty;
    }
}

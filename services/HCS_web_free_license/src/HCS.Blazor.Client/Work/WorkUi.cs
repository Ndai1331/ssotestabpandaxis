using System;
using System.Globalization;

namespace HCS.Blazor.Client.Work;

internal static class WorkUi
{
    public static readonly string[] ProjectStatuses = ["Draft", "Active", "Completed", "Cancelled"];
    public static readonly string[] TaskStatuses = ["New", "InProgress", "Waiting", "Completed", "Cancelled"];
    public static readonly string[] TaskPriorities = ["Low", "Normal", "High", "Urgent"];

    public static string ProjectStatusKey(string status) => status switch
    {
        "Draft" => "Work:Status.Planning",
        "Active" => "Work:Status.InProgress",
        "Completed" => "Work:Status.Completed",
        "Cancelled" => "Work:Status.Cancelled",
        _ => "Work:Status.Planning"
    };

    public static string TaskStatusKey(string status) => status switch
    {
        "New" => "Work:Status.Todo",
        "InProgress" => "Work:Status.Doing",
        "Waiting" => "Work:Status.Waiting",
        "Completed" => "Work:Status.Done",
        "Cancelled" => "Work:Status.Cancelled",
        _ => "Work:Status.Todo"
    };

    public static string TaskStatusTone(string status) => status switch
    {
        "InProgress" => "doing",
        "Waiting" => "waiting",
        "Completed" => "done",
        "Cancelled" => "cancelled",
        _ => "todo"
    };

    public static string TaskStatusColor(string status) => status switch
    {
        "InProgress" => "#355dff",
        "Waiting" => "#D97706",
        "Completed" => "#1d9e75",
        "Cancelled" => "#dc2626",
        _ => "#64748B"
    };

    public static string PriorityKey(string priority) => priority switch
    {
        "Low" => "Work:Priority.Low",
        "High" => "Work:Priority.High",
        "Urgent" => "Work:Priority.Urgent",
        _ => "Work:Priority.Normal"
    };

    public static string ProjectBadgeDs(string status) => status switch
    {
        "Active" => "badge badge-info",
        "Completed" => "badge badge-success",
        "Cancelled" => "badge badge-overdue",
        _ => "badge badge-muted"
    };

    public static string TaskBadgeDs(string status) => status switch
    {
        "InProgress" => "badge badge-info",
        "Waiting" => "badge badge-waiting",
        "Completed" => "badge badge-success",
        "Cancelled" => "badge badge-danger",
        _ => "badge badge-muted"
    };

    public static string ProjectBadgeClass(string status) => ProjectBadgeDs(status);

    public static string TaskBadgeClass(string status) => TaskBadgeDs(status);

    public static string PriorityBadgeClass(string priority) => priority switch
    {
        "High" or "Urgent" => "hcs-priority-badge hcs-priority-badge--high",
        "Low" => "hcs-priority-badge hcs-priority-badge--low",
        _ => "hcs-priority-badge hcs-priority-badge--normal"
    };

    public static string ProgressBadgeClass(int percent) => percent >= 100
        ? "hcs-progress-badge hcs-progress-badge--done"
        : "hcs-progress-badge";

    public static int ProjectProgress(string status) => status switch
    {
        "Completed" => 100,
        "Active" => 55,
        "Cancelled" => 0,
        _ => 15
    };

    public const int MaxStars = 5;

    /// <summary>Survey scores are persisted on a 0-100 scale but collected as 1-5 stars.</summary>
    public static int ScoreToStars(decimal score) =>
        score <= 0 ? 0 : Math.Clamp((int)Math.Round(score / 20m, MidpointRounding.AwayFromZero), 1, MaxStars);

    public static decimal StarsToScore(int stars) => stars * 20m;

    public static string FormatRange(DateTime start, DateTime end) =>
        $"{FormatDay(start)} - {FormatDay(end)}";

    // Match the task modal/detail date pickers, which bind the task's clock time directly.
    public static string FormatDay(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Public check-in pages render on the Blazor host (Docker UTC). Convert the stored UTC
    /// instant to Vietnam wall-clock so guests see the same time as Event details in the browser.
    /// </summary>
    public static string FormatDisplayTime(DateTime value, TimeZoneInfo? timeZone = null)
    {
        var local = UtcToFormTime(AsUtc(value), timeZone ?? VietnamTimeZone);
        return local.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    internal static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    /// <summary>
    /// Date pickers bind Unspecified wall-clock. Convert to UTC in the browser timezone
    /// before save so the server does not stamp the clock face as UTC (+7h in Vietnam).
    /// </summary>
    public static DateTime FormTimeToUtc(DateTime value, TimeZoneInfo? timeZone = null) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), timeZone ?? TimeZoneInfo.Local);

    /// <summary>
    /// Convert a UTC instant back to Unspecified local wall-clock for datetime-local / HcsDatePicker.
    /// </summary>
    public static DateTime UtcToFormTime(DateTime value, TimeZoneInfo? timeZone = null) =>
        DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), timeZone ?? TimeZoneInfo.Local),
            DateTimeKind.Unspecified);

    public static string EventHref(string relatedType, string? relatedId, Guid eventId) =>
        RelatedEntityHref(relatedType, relatedId) ?? $"/calendar-event-detail/{eventId}";

    public static string? RelatedEntityHref(string relatedType, string? relatedId)
    {
        if (!Guid.TryParse(relatedId, out var id)) return null;
        if (string.Equals(relatedType, "PROJECT", StringComparison.OrdinalIgnoreCase))
            return $"/project-detail/{id}";
        if (string.Equals(relatedType, "TASK", StringComparison.OrdinalIgnoreCase))
            return $"/project-task-detail/{id}";
        return null;
    }

    public static bool TryRelatedTaskId(string? relatedType, string? relatedId, out Guid taskId)
    {
        taskId = default;
        return string.Equals(relatedType, "TASK", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(relatedId, out taskId);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone("ICT", TimeSpan.FromHours(7), "ICT", "ICT");
    }
}

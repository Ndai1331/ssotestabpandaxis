using System;
using System.Collections.Generic;
using System.Linq;
using HCS.Blazor.Client.Work;

namespace HCS.Blazor.Client.Components;

public sealed record HcsCalendarRange(string Start, string End, string CurrentStart, string ViewType);

public sealed class HcsCalendarJsEvent
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Start { get; init; } = "";
    public string End { get; init; } = "";
    public bool AllDay { get; init; }
    public string BackgroundColor { get; init; } = "";
    public string BorderColor { get; init; } = "";
    public string TextColor { get; init; } = "";
    public string[] ClassNames { get; init; } = [];
}

public static class CalendarEventDisplay
{
    public const string ProjectAccent = "#F59E0B";
    public const string ProjectBackground = "#FEF3C7";
    public const string ProjectText = "#92400E";
    public const string TaskAccent = "#38BDF8";
    public const string TaskBackground = "#E0F2FE";
    public const string TaskText = "#075985";
    public const string EventAccent = "#00B4A9";
    public const string EventBackground = "#CCFBF1";
    public const string EventText = "#0F766E";

    public static HcsCalendarJsEvent ToJs(CalendarEventDto item)
    {
        var (kind, accent, background, text) = Style(item);
        var startLocal = item.StartTime.ToLocalTime();
        var endLocal = item.EndTime.ToLocalTime();
        string start;
        string end;
        if (item.AllDay)
        {
            start = startLocal.ToString("yyyy-MM-dd");
            var lastInclusive = endLocal.Date < startLocal.Date ? startLocal.Date : endLocal.Date;
            end = lastInclusive.AddDays(1).ToString("yyyy-MM-dd");
        }
        else
        {
            start = startLocal.ToString("o");
            end = endLocal.ToString("o");
        }

        return new HcsCalendarJsEvent
        {
            Id = item.Id.ToString("D"),
            Title = item.Title,
            Start = start,
            End = end,
            AllDay = item.AllDay,
            BackgroundColor = background,
            BorderColor = accent,
            TextColor = text,
            ClassNames = ["hcs-cal-event", "hcs-cal-event--" + kind]
        };
    }

    public static IReadOnlyList<HcsCalendarJsEvent> ToJs(IEnumerable<CalendarEventDto> items) =>
        items.Select(ToJs).ToList();

    public static string Kind(CalendarEventDto item) => Style(item).Kind;

    private static (string Kind, string Accent, string Background, string Text) Style(CalendarEventDto item)
    {
        if (Is(item.EventType, "PROJECT") || Is(item.RelatedType, "PROJECT"))
            return ("project", ProjectAccent, ProjectBackground, ProjectText);
        if (Is(item.EventType, "TASK") || Is(item.RelatedType, "TASK"))
            return ("task", TaskAccent, TaskBackground, TaskText);
        return ("event", EventAccent, EventBackground, EventText);
    }

    private static bool Is(string? value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}

public static class CalendarUi
{
    public static string VisibilityKey(string? value) => value?.ToUpperInvariant() switch
    {
        "PUBLIC" => "Calendar:VisibilityPublic",
        "PARTICIPANTS" => "Calendar:VisibilityParticipants",
        _ => "Calendar:VisibilityPrivate"
    };

    public static string RelatedTypeKey(string? value) => value?.ToUpperInvariant() switch
    {
        "PROJECT" => "Calendar:RelatedProject",
        "TASK" => "Calendar:RelatedTask",
        _ => "Calendar:RelatedNone"
    };

    public static string EventTypeKey(string? value) =>
        string.Equals(value, "Meeting", StringComparison.OrdinalIgnoreCase)
            ? "Calendar:Meeting"
            : string.Empty;
}

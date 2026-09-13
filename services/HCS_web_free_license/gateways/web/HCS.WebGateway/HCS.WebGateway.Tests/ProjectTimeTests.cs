using HCS.Blazor.Client.Work;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class ProjectTimeTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(23, 0)]
    [InlineData(14, 14)]
    public void Task_and_event_times_survive_json_and_repeated_edits(int hour, int minute)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
        var entered = new DateTime(2026, 9, 11, hour, minute, 0);
        var saved = WorkUi.FormTimeToUtc(entered, zone);
        for (var edit = 0; edit < 3; edit++)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(saved);
            var received = System.Text.Json.JsonSerializer.Deserialize<DateTime>(json);
            Assert.Equal(DateTimeKind.Utc, received.Kind);
            var form = WorkUi.UtcToFormTime(received, zone);
            Assert.Equal(entered, form);
            Assert.Equal(DateTimeKind.Unspecified, form.Kind);
            saved = WorkUi.FormTimeToUtc(form, zone);
            Assert.Equal(entered.AddHours(-7).Ticks, saved.Ticks);
        }
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void Project_dates_keep_entered_time_across_save_and_edit(DateTimeKind kind)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
        var start = new DateTime(2026, 9, 11, 0, 0, 0, kind);
        var end = new DateTime(2026, 10, 11, 23, 0, 0, kind);
        var utcStart = WorkUi.FormTimeToUtc(start, zone);
        var utcEnd = WorkUi.FormTimeToUtc(end, zone);
        Assert.Equal(new DateTime(2026, 9, 10, 17, 0, 0, DateTimeKind.Utc), utcStart);
        Assert.Equal(new DateTime(2026, 10, 11, 16, 0, 0, DateTimeKind.Utc), utcEnd);
        Assert.Equal(start.Ticks, WorkUi.UtcToFormTime(utcStart, zone).Ticks);
        Assert.Equal(end.Ticks, WorkUi.UtcToFormTime(utcEnd, zone).Ticks);
        Assert.Equal(utcEnd, WorkUi.FormTimeToUtc(WorkUi.UtcToFormTime(utcEnd, zone), zone));
    }
}

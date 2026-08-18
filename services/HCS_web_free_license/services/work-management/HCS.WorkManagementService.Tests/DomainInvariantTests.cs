using HCS.WorkManagementService.Domain;
using Volo.Abp;

namespace HCS.WorkManagementService.Tests;

public sealed class DomainInvariantTests
{
    [Fact]
    public void Project_rejects_inverted_date_range()
    {
        Assert.Throws<BusinessException>(() => new Project(Guid.NewGuid(), "P-1", "Project",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), "Active", null, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Task_rejects_progress_outside_percentage(int progress)
    {
        Assert.Throws<BusinessException>(() => new ProjectTask(Guid.NewGuid(), Guid.NewGuid(), null, "T-1", "Task",
            null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Normal", "Open", progress));
    }

    [Fact]
    public void Calendar_rejects_inverted_date_range()
    {
        Assert.Throws<BusinessException>(() => new CalendarEvent(Guid.NewGuid(), "Meeting", null,
            DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1), false, "Meeting", null, "None", null, "Internal", Guid.NewGuid()));
    }

    [Fact]
    public void Calendar_change_rejects_inverted_date_range()
    {
        var start = DateTime.UtcNow;
        var calendar = new CalendarEvent(Guid.NewGuid(), "Meeting", null, start, start.AddHours(1), false,
            "Meeting", null, "NONE", null, "Internal", Guid.NewGuid());
        Assert.Throws<BusinessException>(() => calendar.Change("Updated", null, start, start.AddMinutes(-1), false,
            "Meeting", null, "PROJECT", null, "Private"));
    }

    [Fact]
    public void Survey_session_change_rejects_inverted_date_range()
    {
        var start = DateTime.UtcNow;
        var session = new SurveySession(Guid.NewGuid(), "S-1", "Survey", start, start.AddDays(1), null, Guid.NewGuid());
        Assert.Throws<BusinessException>(() => session.Change("Updated", start, start.AddDays(-1), null));
        session.Change("Updated", start, start.AddDays(2), null);
        Assert.Equal("Updated", session.Name);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Survey_rejects_score_outside_range(decimal score)
    {
        Assert.Throws<BusinessException>(() => new SurveyResult(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, score, null));
    }
}

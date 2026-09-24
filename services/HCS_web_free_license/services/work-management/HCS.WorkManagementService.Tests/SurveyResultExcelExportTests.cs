using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Data;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;

namespace HCS.WorkManagementService.Tests;

public sealed class SurveyResultExcelExportTests
{
    [Fact]
    public async Task Export_writes_one_row_per_criterion_with_session_detail()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var service = new SurveyAppService(db, null!);
        var clinic = await service.CreateLocationAsync(new("CLINIC", "Phòng khám", null), ct);
        var other = await service.CreateLocationAsync(new("OTHER", "Khoa khác", null), ct);
        var attitude = await service.CreateCriteriaAsync(new("ATT", "Thái độ", 1, clinic.Id), ct);
        var wait = await service.CreateCriteriaAsync(new("WAIT", "Thời gian chờ", 2, clinic.Id), ct);
        var otherCriteria = await service.CreateCriteriaAsync(new("OTH", "Khác", 1, other.Id), ct);

        var surveyedAt = new DateTime(2026, 9, 23, 8, 30, 0, DateTimeKind.Utc);
        var session = await service.CreatePublicSessionAsync(new(
            clinic.Id, "Nguyễn A", "0900000000", "BN-01", surveyedAt, "Web", "Ghi chú phiên"), ct);
        await service.SubmitPublicResultsAsync(session.Id,
        [
            new(attitude.Id, null, 100, "Rất hài lòng"),
            new(wait.Id, null, 60, "Hơi lâu")
        ], ct);
        await service.HandleResultAsync(session.Id, new("Processed", "Đã gọi lại"), ct);

        var otherSession = await service.CreatePublicSessionAsync(new(
            other.Id, "Trần B", "0911111111", null, surveyedAt.AddHours(-2), "Web", null), ct);
        await service.SubmitPublicResultsAsync(otherSession.Id, [new(otherCriteria.Id, null, 40, null)], ct);

        var file = await service.ExportResultsAsync(clinic.Id, ct);

        Assert.Equal("SurveyResults.xlsx", file.FileName);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        using var stream = new MemoryStream(file.Content);
        var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(["Thái độ", "Thời gian chờ"], rows.Select(row => row["Tiêu chí đánh giá"]).OrderBy(name => name).ToArray());
        var attitudeRow = rows.Single(row => Equals(row["Tiêu chí đánh giá"], "Thái độ"));
        Assert.Equal(5, Convert.ToInt32(attitudeRow["Điểm đánh giá"]));
        Assert.Equal("Rất hài lòng", attitudeRow["Nhận xét"]);
        Assert.Equal("Phòng khám", attitudeRow["Vị trí khảo sát"]);
        Assert.Equal("Nguyễn A", attitudeRow["Khách hàng"]);
        Assert.Equal("0900000000", attitudeRow["Số điện thoại"]);
        Assert.Equal("BN-01", attitudeRow["Mã bệnh nhân"]);
        Assert.Equal("Ghi chú phiên", attitudeRow["Ghi chú"]);
        Assert.Equal("23/09/2026 15:30", attitudeRow["Thời gian khảo sát"]);
        Assert.Equal("Đã xử lý", attitudeRow["Trạng thái"]);
        Assert.Equal("Đã gọi lại", attitudeRow["Ghi chú xử lý"]);
        Assert.Equal(3, Convert.ToInt32(rows.Single(row => Equals(row["Tiêu chí đánh giá"], "Thời gian chờ"))["Điểm đánh giá"]));
    }

    [Fact]
    public async Task Export_without_results_still_returns_a_workbook()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var service = new SurveyAppService(db, null!);

        var file = await service.ExportResultsAsync(null, ct);

        Assert.NotEmpty(file.Content);
        using var stream = new MemoryStream(file.Content);
        Assert.Empty(stream.Query(useHeaderRow: true));
    }

    private static WorkManagementDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WorkManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

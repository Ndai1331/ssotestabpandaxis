using HCS.DocumentService.Signing;
using MiniExcelLibs;

namespace HCS.DocumentService.Tests;

public sealed class SigningKpiExcelExportTests
{
    [Fact]
    public void Export_writes_detail_rows_in_the_licensed_layout()
    {
        var later = new DateTime(2026, 9, 23, 8, 30, 0, DateTimeKind.Utc);
        var earlier = new DateTime(2026, 9, 23, 1, 0, 0, DateTimeKind.Utc);
        var legacyLocal = new DateTime(2026, 9, 22, 14, 15, 0, DateTimeKind.Unspecified);
        var bytes = SigningKpiExcelExporter.Build(
        [
            new SigningKpiDetailRowDto
            {
                Source = "HCS",
                Code = "HS-2",
                Title = "Mới hơn",
                GroupCode = "WF",
                GroupName = "Quy trình",
                SubmitterId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                SubmittedAt = earlier,
                StatusCode = "IN_PROGRESS",
                StatusLabel = "Đang xử lý",
                SignerChain = "Bước 1"
            },
            new SigningKpiDetailRowDto
            {
                Source = "SQL",
                Code = "HS-1",
                Title = "Đề xuất",
                GroupCode = "DX",
                GroupName = "Đề xuất",
                SubmitterName = "Nguyễn A",
                SubmittedAt = later,
                DeadlineAt = later.AddHours(2),
                CompletedAt = later.AddHours(1),
                StatusCode = "COMPLETED",
                StatusLabel = "Đã phê duyệt",
                IsOnTime = true,
                ProcessingHours = 1.5,
                SignerChain = "Bước 1 | Bước 2",
                Note = "Đã duyệt"
            },
            new SigningKpiDetailRowDto
            {
                Source = "LEGACY",
                Code = "LS-1",
                Title = "Cũ",
                SubmittedAt = legacyLocal,
                IsOnTime = false,
                StatusLabel = "Từ chối"
            }
        ]);

        using var stream = new MemoryStream(bytes);
        var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        Assert.Equal(["HS-1", "HS-2", "LS-1"], rows.Select(row => row["Mã"]).ToArray());
        var approved = rows[0];
        Assert.Equal("SQL", approved["Nguồn"]);
        Assert.Equal("Đề xuất", approved["Tiêu đề"]);
        Assert.Equal("DX", approved["Mã nhóm"]);
        Assert.Equal("Đề xuất", approved["Tên nhóm"]);
        Assert.Equal("Nguyễn A", approved["Người trình"]);
        Assert.Equal("23/09/2026 15:30", approved["Ngày trình"]);
        Assert.Equal("23/09/2026 17:30", approved["Hạn"]);
        Assert.Equal("23/09/2026 16:30", approved["Ngày hoàn thành"]);
        Assert.Equal("COMPLETED", approved["Mã trạng thái"]);
        Assert.Equal("Đã phê duyệt", approved["Trạng thái"]);
        Assert.Equal("Có", approved["Đúng hạn"]);
        Assert.Equal(1.5d, Convert.ToDouble(approved["TG xử lý (giờ)"]));
        Assert.Equal("Bước 1 | Bước 2", approved["Chuỗi ký"]);
        Assert.Equal("Đã duyệt", approved["Ghi chú"]);
        Assert.Equal("11111111-1111-1111-1111-111111111111", rows[1]["Người trình"]);
        Assert.Equal("Không", rows[2]["Đúng hạn"]);
        Assert.Equal("22/09/2026 14:15", rows[2]["Ngày trình"]);
    }

    [Fact]
    public void File_name_uses_vietnam_clock()
    {
        var name = SigningKpiExcelExporter.FileName(new DateTime(2026, 9, 23, 8, 30, 0, DateTimeKind.Utc));
        Assert.Equal("bao_cao_trinh_ky_chi_tiet_20260923_1530.xlsx", name);
    }
}

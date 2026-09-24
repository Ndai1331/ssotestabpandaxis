using System.Globalization;
using MiniExcelLibs;

namespace HCS.DocumentService.Signing;

internal static class SigningKpiExcelExporter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Build(IReadOnlyCollection<SigningKpiDetailRowDto> rows)
    {
        var items = rows
            .OrderByDescending(row => row.SubmittedAt ?? DateTime.MinValue)
            .ThenBy(row => row.Source, StringComparer.Ordinal)
            .Select(ToRow)
            .ToList();

        using var stream = new MemoryStream();
        stream.SaveAs(items);
        return stream.ToArray();
    }

    public static string FileName(DateTime utcNow) =>
        $"bao_cao_trinh_ky_chi_tiet_{ToVietnam(utcNow):yyyyMMdd_HHmm}.xlsx";

    private static Dictionary<string, object?> ToRow(SigningKpiDetailRowDto row) => new()
    {
        ["Nguồn"] = row.Source,
        ["Mã"] = row.Code,
        ["Tiêu đề"] = row.Title,
        ["Mã nhóm"] = row.GroupCode,
        ["Tên nhóm"] = row.GroupName,
        ["Người trình"] = row.SubmitterName ?? row.SubmitterId?.ToString(),
        ["Ngày trình"] = FormatDateTime(row.SubmittedAt),
        ["Hạn"] = FormatDateTime(row.DeadlineAt),
        ["Ngày hoàn thành"] = FormatDateTime(row.CompletedAt),
        ["Mã trạng thái"] = row.StatusCode,
        ["Trạng thái"] = row.StatusLabel,
        ["Đúng hạn"] = row.IsOnTime switch
        {
            true => "Có",
            false => "Không",
            _ => null
        },
        ["TG xử lý (giờ)"] = row.ProcessingHours,
        ["Chuỗi ký"] = row.SignerChain,
        ["Ghi chú"] = row.Note
    };

    private static string? FormatDateTime(DateTime? value) =>
        value.HasValue
            ? ToVietnam(value.Value).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : null;

    private static DateTime ToVietnam(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => (DateTime?)null
        };
        return utc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(utc.Value, VietnamZone)
            : value;
    }

    private static readonly TimeZoneInfo VietnamZone = ResolveVietnamZone();

    private static TimeZoneInfo ResolveVietnamZone()
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

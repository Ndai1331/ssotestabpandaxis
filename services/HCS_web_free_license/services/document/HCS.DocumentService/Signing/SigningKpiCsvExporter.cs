using System.Globalization;
using System.Text;

namespace HCS.DocumentService.Signing;

internal static class SigningKpiCsvExporter
{
    public static byte[] Build(IReadOnlyCollection<SigningKpiDetailRowDto> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',',
            "Nguồn", "Mã hồ sơ", "Tiêu đề", "Mã quy trình", "Tên quy trình", "Người gửi",
            "Thời điểm gửi", "Hạn xử lý", "Hoàn tất", "Mã trạng thái", "Trạng thái",
            "Đúng hạn", "Số giờ xử lý", "Chuỗi bước ký", "Ghi chú"));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',',
                Csv(row.Source), Csv(row.Code), Csv(row.Title), Csv(row.GroupCode), Csv(row.GroupName),
                Csv(row.SubmitterId), Csv(row.SubmittedAt), Csv(row.DeadlineAt), Csv(row.CompletedAt),
                Csv(row.StatusCode), Csv(row.StatusLabel), Csv(row.IsOnTime), Csv(row.ProcessingHours),
                Csv(row.SignerChain), Csv(row.Note)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private static string Csv(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            bool boolean => boolean ? "Yes" : "No",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}

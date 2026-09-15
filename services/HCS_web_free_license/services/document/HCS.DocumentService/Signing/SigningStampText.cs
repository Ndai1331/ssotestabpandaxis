namespace HCS.DocumentService.Signing;

/// <summary>
/// Formats the visible signing-date line to match the licensed TAG stamp:
/// "Ngày ký:dd/MM/yyyy HH:mm:ss" in Vietnam time (UTC+7).
/// </summary>
internal static class SigningStampText
{
    internal const string DateLabel = "Ngày ký:";
    internal const string DateFormat = "dd/MM/yyyy HH:mm:ss";

    private static readonly TimeZoneInfo VietnamZone = ResolveVietnamZone();

    internal static DateTime ToVietnam(DateTime utcNow)
    {
        var utc = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamZone);
    }

    internal static string FormatDateLine(DateTime? utcNow = null) =>
        DateLabel + ToVietnam(utcNow ?? DateTime.UtcNow).ToString(DateFormat);

    internal static string FormatSignerWithDate(string? signerName, DateTime? utcNow = null)
    {
        var dateLine = FormatDateLine(utcNow);
        return string.IsNullOrWhiteSpace(signerName) ? dateLine : $"{signerName.Trim()}\n{dateLine}";
    }

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

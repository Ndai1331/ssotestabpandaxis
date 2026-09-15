using HCS.DocumentService.Signing;

namespace HCS.DocumentService.Tests;

public sealed class SigningStampTextTests
{
    [Fact]
    public void Date_line_matches_licensed_tag_stamp_format()
    {
        var utc = new DateTime(2026, 5, 20, 7, 14, 1, DateTimeKind.Utc);

        var line = SigningStampText.FormatDateLine(utc);

        Assert.Equal("Ngày ký:20/05/2026 14:14:01", line);
    }

    [Fact]
    public void Signer_stamp_puts_the_date_under_the_name()
    {
        var utc = new DateTime(2026, 5, 20, 7, 14, 1, DateTimeKind.Utc);

        var text = SigningStampText.FormatSignerWithDate("ABC Admin", utc);

        Assert.Equal("ABC Admin\nNgày ký:20/05/2026 14:14:01", text);
    }
}

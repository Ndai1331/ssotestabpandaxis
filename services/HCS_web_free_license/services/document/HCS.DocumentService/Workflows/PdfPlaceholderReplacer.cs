using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using SixLabors.ImageSharp;
using UglyToad.PdfPig;

namespace HCS.DocumentService.Workflows;

internal static class PdfPlaceholderReplacer
{
    internal sealed record PlaceholderHit(int Page, double X, double Y, double Width, double Height);

    public static byte[] ReplacePrepared(
        byte[] pdfBytes,
        byte[] signatureImage,
        string fullName,
        string? positionName,
        string? departmentName,
        string? signingContent,
        DateTime now)
    {
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["<<DD>>"] = now.ToString("dd"),
            ["<<Day>>"] = now.ToString("dd"),
            ["<<MM>>"] = now.ToString("MM"),
            ["<<Month>>"] = now.ToString("MM"),
            ["<<YYYY>>"] = now.ToString("yyyy"),
            ["<<Year>>"] = now.ToString("yyyy"),
            ["<<PreparedFullName>>"] = fullName,
            ["<<VitriVieclam>>"] = positionName ?? string.Empty,
            ["<<ViTriLamViec>>"] = positionName ?? string.Empty,
            ["<<Position>>"] = positionName ?? string.Empty,
            ["<<PositionName>>"] = positionName ?? string.Empty,
            ["<<Department>>"] = departmentName ?? string.Empty,
            ["<<PhongBan>>"] = departmentName ?? string.Empty,
            ["<<ContentToBeApproved>>"] = PlainText(signingContent),
        };

        return Replace(pdfBytes, replacements, signatureImage, "<<PreparedBySign>>");
    }

    public static byte[] ReplaceApprovalText(byte[] pdfBytes, int stepOrder, string fullName, string? noteContent)
    {
        var suffix = stepOrder.ToString("D2");
        return Replace(pdfBytes, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"<<FullName{suffix}>>"] = fullName,
            [$"<<NoteContent{suffix}>>"] = PlainText(noteContent),
            ["<<NoteContent>>"] = PlainText(noteContent),
        }, null, null);
    }

    private static byte[] Replace(
        byte[] pdfBytes,
        IReadOnlyDictionary<string, string> textReplacements,
        byte[]? imageBytes,
        string? imagePlaceholder)
    {
        var hits = new List<(PlaceholderHit Hit, string? Text, bool Image)>();
        foreach (var pair in textReplacements)
        {
            var hit = FindPlaceholder(pdfBytes, pair.Key);
            if (hit is not null) hits.Add((hit, pair.Value, false));
        }
        if (imageBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(imagePlaceholder))
        {
            var hit = FindPlaceholder(pdfBytes, imagePlaceholder);
            if (hit is not null) hits.Add((hit, null, true));
        }
        if (hits.Count == 0) return pdfBytes;

        using var input = new MemoryStream(pdfBytes);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
        foreach (var (hit, text, image) in hits)
        {
            if (hit.Page < 1 || hit.Page > document.PageCount) continue;
            var page = document.Pages[hit.Page - 1];
            var y = Math.Max(0, page.Height.Point - hit.Y - hit.Height - 3);
            var x = Math.Max(0, hit.X - 3);
            var width = Math.Max(12, hit.Width + 6);
            var height = Math.Max(12, hit.Height + 6);
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            graphics.DrawRectangle(XBrushes.White, new XRect(x, y, width, height));
            if (image)
            {
                using var imageStream = new MemoryStream(imageBytes!);
                using var signatureImage = XImage.FromStream(imageStream);
                var aspect = signatureImage.PixelHeight == 0 ? 1d : (double)signatureImage.PixelWidth / signatureImage.PixelHeight;
                var imageWidth = width;
                var imageHeight = imageWidth / aspect;
                if (imageHeight > height)
                {
                    imageHeight = height;
                    imageWidth = imageHeight * aspect;
                }
                graphics.DrawImage(signatureImage, x + (width - imageWidth) / 2, y + (height - imageHeight) / 2,
                    imageWidth, imageHeight);
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                var font = new XFont(HC.PdfFontEnvironment.DefaultPdfSerifFontFamily, 9);
                graphics.DrawString(text, font, XBrushes.Black, new XRect(x, y, width, height), XStringFormats.CenterLeft);
            }
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    internal static PlaceholderHit? FindPlaceholder(byte[] pdfBytes, string placeholder)
    {
        if (pdfBytes is not { Length: > 0 } || string.IsNullOrWhiteSpace(placeholder)) return null;
        using var document = PdfDocument.Open(pdfBytes);
        for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
        {
            var letters = document.GetPage(pageNumber).Letters.ToList();
            var text = string.Concat(letters.Select(x => x.Value));
            var index = text.IndexOf(placeholder, StringComparison.Ordinal);
            if (index < 0 || index + placeholder.Length > letters.Count) continue;
            var first = letters[index].GlyphRectangle;
            var left = first.Left;
            var bottom = first.Bottom;
            var right = first.Right;
            var top = first.Top;
            foreach (var letter in letters.Skip(index + 1).Take(placeholder.Length))
            {
                left = Math.Min(left, letter.GlyphRectangle.Left);
                bottom = Math.Min(bottom, letter.GlyphRectangle.Bottom);
                right = Math.Max(right, letter.GlyphRectangle.Right);
                top = Math.Max(top, letter.GlyphRectangle.Top);
            }
            return new PlaceholderHit(pageNumber, left, bottom, right - left, top - bottom);
        }
        return null;
    }

    private static string PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = value.Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", " ");
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }
}

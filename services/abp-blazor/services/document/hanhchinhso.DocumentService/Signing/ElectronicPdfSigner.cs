using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using UglyToad.PdfPig;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Signing;

public interface IElectronicPdfSigner
{
    byte[] Sign(
        byte[] sourcePdf,
        byte[] signatureImage,
        int stepOrder,
        int configuredWidth,
        int configuredHeight);
}

public sealed record PdfSigningPlaceholder(
    int Page,
    int X,
    int Y);

public interface IPdfSigningPlaceholderLocator
{
    PdfSigningPlaceholder Locate(byte[] sourcePdf, string placeholder);
}

public sealed class PdfSigningPlaceholderLocator :
    IPdfSigningPlaceholderLocator,
    ITransientDependency
{
    public PdfSigningPlaceholder Locate(
        byte[] sourcePdf,
        string placeholder)
    {
        if (placeholder.IsNullOrWhiteSpace())
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningPlaceholder");
        }
        try
        {
            using var document = PdfDocument.Open(sourcePdf);
            for (var pageNumber = 1;
                 pageNumber <= document.NumberOfPages;
                 pageNumber++)
            {
                var page = document.GetPage(pageNumber);
                var letters = page.Letters.ToList();
                var text = string.Concat(
                    letters.Select(x => x.Value));
                var offset = text.IndexOf(
                    placeholder,
                    StringComparison.OrdinalIgnoreCase);
                if (offset < 0 || offset + placeholder.Length >
                    letters.Count)
                {
                    continue;
                }
                var boxes = letters
                    .Skip(offset)
                    .Take(placeholder.Length)
                    .Select(x => x.GlyphRectangle)
                    .ToList();
                return new PdfSigningPlaceholder(
                    pageNumber,
                    (int)Math.Floor(boxes.Min(x => x.Left)),
                    (int)Math.Floor(boxes.Min(x => x.Bottom)));
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BusinessException(
                "DocumentService:InvalidSourcePdf",
                innerException: exception);
        }
        throw new BusinessException(
            "DocumentService:SigningPlaceholderNotFound")
            .WithData("Placeholder", placeholder);
    }
}

public sealed class ElectronicPdfSigner :
    IElectronicPdfSigner,
    ITransientDependency
{
    public byte[] Sign(
        byte[] sourcePdf,
        byte[] signatureImage,
        int stepOrder,
        int configuredWidth,
        int configuredHeight)
    {
        EnsurePdf(sourcePdf);
        if (signatureImage.Length == 0 ||
            configuredWidth is < 1 or > 2000 ||
            configuredHeight is < 1 or > 2000)
        {
            throw new BusinessException(
                "DocumentService:InvalidElectronicSignInput");
        }

        var tag = $"<<Sign{stepOrder:D2}>>";
        var positions = FindPositions(sourcePdf, tag);
        if (positions.Count == 0)
        {
            throw new BusinessException(
                "DocumentService:SigningPlaceholderNotFound")
                .WithData("Placeholder", tag);
        }

        try
        {
            using var source = new MemoryStream(sourcePdf, writable: false);
            using var imageStream =
                new MemoryStream(signatureImage, writable: false);
            using var image = XImage.FromStream(imageStream);
            using var document = PdfReader.Open(
                source, PdfDocumentOpenMode.Modify);
            foreach (var position in positions)
            {
                var page = document.Pages[position.PageIndex];
                using var graphics = XGraphics.FromPdfPage(
                    page, XGraphicsPdfPageOptions.Append);
                var width = Math.Max(position.Width, configuredWidth);
                var height = Math.Max(position.Height, configuredHeight);
                var x = Math.Clamp(
                    position.X, 0, Math.Max(0, page.Width.Point - width));
                var y = Math.Clamp(
                    position.PageHeight - position.Top,
                    0,
                    Math.Max(0, page.Height.Point - height));
                graphics.DrawRectangle(
                    XBrushes.White,
                    new XRect(x, y, width, height));
                graphics.DrawImage(image, x, y, width, height);
            }
            using var output = new MemoryStream();
            document.Save(output, closeStream: false);
            var result = output.ToArray();
            EnsurePdf(result);
            using var verified = PdfDocument.Open(result);
            if (verified.NumberOfPages == 0)
            {
                throw new BusinessException(
                    "DocumentService:InvalidSignedPdf");
            }
            return result;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BusinessException(
                "DocumentService:ElectronicSigningFailed",
                innerException: exception);
        }
    }

    private static List<PlaceholderPosition> FindPositions(
        byte[] sourcePdf,
        string tag)
    {
        var result = new List<PlaceholderPosition>();
        try
        {
            using var document = PdfDocument.Open(sourcePdf);
            for (var pageIndex = 0;
                 pageIndex < document.NumberOfPages;
                 pageIndex++)
            {
                var page = document.GetPage(pageIndex + 1);
                var letters = page.Letters.ToList();
                var textBuilder = new System.Text.StringBuilder();
                var characterToLetter = new List<int>();
                for (var letterIndex = 0;
                     letterIndex < letters.Count;
                     letterIndex++)
                {
                    var value = letters[letterIndex].Value;
                    textBuilder.Append(value);
                    characterToLetter.AddRange(
                        Enumerable.Repeat(letterIndex, value.Length));
                }
                var text = textBuilder.ToString();
                var offset = 0;
                while ((offset = text.IndexOf(
                           tag,
                           offset,
                           StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    if (offset + tag.Length > characterToLetter.Count)
                    {
                        break;
                    }
                    var letterIndexes = characterToLetter
                        .Skip(offset)
                        .Take(tag.Length)
                        .Distinct();
                    var boxes = letterIndexes
                        .Select(x => letters[x])
                        .Select(x => x.GlyphRectangle)
                        .ToList();
                    result.Add(new PlaceholderPosition(
                        pageIndex,
                        boxes.Min(x => x.Left),
                        boxes.Max(x => x.Top),
                        Math.Max(1, boxes.Max(x => x.Right) -
                            boxes.Min(x => x.Left)),
                        Math.Max(1, boxes.Max(x => x.Top) -
                            boxes.Min(x => x.Bottom)),
                        page.Height));
                    offset += tag.Length;
                }
            }
            return result;
        }
        catch (Exception exception)
        {
            throw new BusinessException(
                "DocumentService:InvalidSourcePdf",
                innerException: exception);
        }
    }

    private static void EnsurePdf(byte[] bytes)
    {
        if (bytes.Length < 8 ||
            !bytes.AsSpan(0, 4).SequenceEqual("%PDF"u8))
        {
            throw new BusinessException(
                "DocumentService:InvalidSourcePdf");
        }
    }

    private sealed record PlaceholderPosition(
        int PageIndex,
        double X,
        double Top,
        double Width,
        double Height,
        double PageHeight);
}

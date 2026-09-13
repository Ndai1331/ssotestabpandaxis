using System.Text;

namespace HCS.DocumentService.Documents;

public static class DocumentDownloadFileName
{
    private const int MaxStemLength = 180;

    public static string For(DocumentDto document, string? originalFileName) =>
        For(document.DocumentCode, document.Number, document.Title, originalFileName);

    public static string For(string? documentCode, string? archiveNumber, string? title, string? originalFileName)
    {
        var original = Path.GetFileName((originalFileName ?? string.Empty).Replace('\\', '/'));
        var extension = Path.GetExtension(original);
        var stem = FirstPresent(documentCode, archiveNumber, title, Path.GetFileNameWithoutExtension(original));
        stem = SanitizeStem(stem);
        if (stem.Length == 0)
        {
            stem = "document";
        }

        return stem + extension;
    }

    private static string FirstPresent(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static string SanitizeStem(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var previousDash = false;
        foreach (var c in value.Trim())
        {
            if (c is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
            {
                if (!previousDash && builder.Length > 0)
                {
                    builder.Append('-');
                    previousDash = true;
                }

                continue;
            }

            if (char.IsControl(c))
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (builder.Length == 0 || builder[^1] == ' ')
                {
                    continue;
                }

                builder.Append(' ');
                previousDash = false;
                continue;
            }

            builder.Append(c);
            previousDash = false;
        }

        var stem = builder.ToString().Trim().Trim('.', '-');
        if (stem.Length > MaxStemLength)
        {
            stem = stem[..MaxStemLength].TrimEnd(' ', '.', '-');
        }

        return stem;
    }
}

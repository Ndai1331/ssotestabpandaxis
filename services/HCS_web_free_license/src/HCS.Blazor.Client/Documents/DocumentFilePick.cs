using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HCS.Blazor.Client.Documents;

internal static class DocumentFilePick
{
    public static DocumentFileDto? PreferPdf(IReadOnlyList<DocumentFileDto> files)
    {
        if (files.Count == 0) return null;
        var ordered = files.OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id).ToList();
        var pdf = ordered.FirstOrDefault(IsPdf);
        if (pdf is not null) return pdf;
        foreach (var file in ordered)
        {
            if (file.PairedFileId is not { } pair) continue;
            var paired = files.FirstOrDefault(x => x.Id == pair);
            if (paired is not null && IsPdf(paired)) return paired;
        }
        return null;
    }

    public static bool IsPdf(DocumentFileDto file) => IsPdf(file.FileName, file.ContentType);

    public static bool IsPdf(string fileName, string? contentType) =>
        contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true
        || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool IsWord(string fileName, string? contentType) =>
        contentType?.Contains("word", StringComparison.OrdinalIgnoreCase) == true
        || contentType?.Contains("officedocument.wordprocessingml", StringComparison.OrdinalIgnoreCase) == true
        || fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

    public static string IconClass(DocumentFileDto file) => IconClass(file.FileName, file.ContentType);

    public static string IconClass(string fileName, string? contentType)
    {
        if (IsPdf(fileName, contentType)) return "fa-file-pdf";
        if (IsWord(fileName, contentType)) return "fa-file-word";
        if (fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || contentType?.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase) == true)
            return "fa-file-excel";
        return "fa-file-lines";
    }
}

internal static class DocumentSelectText
{
    public static string Label(DocumentDto document)
    {
        var fileName = document.Files
            .OrderByDescending(x => x.CreationTime)
            .Select(x => x.FileName)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return Join(fileName, document.DocumentCode, document.Number, document.Title);
    }

    public static string? ReferenceCode(DocumentDto? document) =>
        string.IsNullOrWhiteSpace(document?.DocumentCode) ? document?.Number : document.DocumentCode;

    public static bool Matches(DocumentDto document, string term) =>
        string.IsNullOrWhiteSpace(term)
        || Contains(document.Title, term)
        || Contains(document.Number, term)
        || Contains(document.DocumentCode, term)
        || document.Files.Any(file => Contains(file.FileName, term));

    private static string Join(params string?[] parts) =>
        string.Join(" · ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}

internal static class DocumentDownloadFileName
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

        var builder = new System.Text.StringBuilder(value.Length);
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HCS.Coding;

public enum AutoCodeKind
{
    Project,
    Task,
    Document,
    PersonalDocument,
    Archive,
    PersonalArchive,
    Workflow,
    Catalog
}

public static class AutoCode
{
    public const int Padding = 4;
    public const int MaxPrefixLength = 12;
    public const int MaxSequence = 999_999;

    public static class Defaults
    {
        public const string Project = "PJ";
        public const string Task = "T";
        public const string Document = "VB";
        public const string PersonalDocument = "VBCN";
        public const string Archive = "LT";
        public const string PersonalArchive = "LTCN";
        public const string Workflow = "QT";
        public const string Catalog = "DM";
    }

    public static string DefaultPrefix(AutoCodeKind kind) => kind switch
    {
        AutoCodeKind.Project => Defaults.Project,
        AutoCodeKind.Task => Defaults.Task,
        AutoCodeKind.Document => Defaults.Document,
        AutoCodeKind.PersonalDocument => Defaults.PersonalDocument,
        AutoCodeKind.Archive => Defaults.Archive,
        AutoCodeKind.PersonalArchive => Defaults.PersonalArchive,
        AutoCodeKind.Workflow => Defaults.Workflow,
        AutoCodeKind.Catalog => Defaults.Catalog,
        _ => Defaults.Catalog
    };

    public static string NormalizePrefix(string? prefix, AutoCodeKind kind)
    {
        var fallback = DefaultPrefix(kind);
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return fallback;
        }

        var builder = new StringBuilder(MaxPrefixLength);
        foreach (var character in prefix.Trim())
        {
            if (char.IsLetterOrDigit(character) || character is '_' or '-')
            {
                builder.Append(character);
                if (builder.Length >= MaxPrefixLength)
                {
                    break;
                }
            }
        }

        return builder.Length == 0 ? fallback : builder.ToString();
    }

    public static string Preview(string prefix) => Format(prefix, 1);

    public static string Format(string prefix, int number)
    {
        var width = Math.Max(Padding, number.ToString(CultureInfo.InvariantCulture).Length);
        return string.Concat(prefix, number.ToString($"D{width}", CultureInfo.InvariantCulture));
    }

    public static string Next(string prefix, IEnumerable<string> existing)
    {
        var max = 0;
        foreach (var code in existing)
        {
            if (TryParseSequence(code, prefix, out var number) && number > max)
            {
                max = number;
            }
        }

        var next = max + 1;
        if (next > MaxSequence)
        {
            throw new InvalidOperationException("Auto-generated code sequence is exhausted.");
        }

        return Format(prefix, next);
    }

    public static async Task<string> AllocateAsync(
        IAutoCodeSettings settings,
        AutoCodeKind kind,
        string? requested,
        IEnumerable<string> existing,
        CancellationToken cancellationToken = default)
    {
        var explicitCode = NullIfEmpty(requested);
        if (explicitCode is not null)
        {
            return explicitCode;
        }

        var prefix = await settings.GetPrefixAsync(kind, cancellationToken);
        return Next(prefix, existing);
    }

    public static bool TryParseSequence(string? code, string prefix, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        if (!code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suffix = code[prefix.Length..];
        return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0;
    }

    public static string? NullIfEmpty(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

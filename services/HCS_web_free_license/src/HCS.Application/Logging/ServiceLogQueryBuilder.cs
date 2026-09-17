using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HCS.Logging;

public static class ServiceLogQueryBuilder
{
    public const int MaxFilterLength = 256;
    public const int DefaultCount = 100;
    public const int MaxCount = 200;

    public static string Build(
        IReadOnlyList<string>? applications,
        IReadOnlyList<string>? levels,
        string? keyword)
    {
        var selectedApps = NormalizeApplications(applications);
        var selectedLevels = NormalizeLevels(levels);
        var term = NormalizeKeyword(keyword);

        var clauses = new List<string>();
        if (selectedApps.Count > 0)
        {
            clauses.Add(InClause("Application", selectedApps));
        }

        clauses.Add(InClause("@Level", selectedLevels));

        if (term is not null)
        {
            var escaped = Escape(term);
            clauses.Add($"(Contains(@Message, '{escaped}') or Contains(@Exception, '{escaped}'))");
        }

        return string.Join(" and ", clauses);
    }

    public static int NormalizeCount(int maxResultCount)
    {
        if (maxResultCount <= 0)
        {
            return DefaultCount;
        }

        return Math.Min(maxResultCount, MaxCount);
    }

    public static string? NormalizeAfterId(string? afterId)
    {
        if (string.IsNullOrWhiteSpace(afterId))
        {
            return null;
        }

        var value = afterId.Trim();
        if (value.Length > 128)
        {
            throw new ArgumentException("AfterId is too long.", nameof(afterId));
        }

        foreach (var ch in value)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_')
            {
                throw new ArgumentException("AfterId contains invalid characters.", nameof(afterId));
            }
        }

        return value;
    }

    private static List<string> NormalizeApplications(IReadOnlyList<string>? applications)
    {
        if (applications is null || applications.Count == 0)
        {
            return [];
        }

        var selected = new List<string>();
        foreach (var application in applications)
        {
            if (string.IsNullOrWhiteSpace(application))
            {
                continue;
            }

            var name = application.Trim();
            if (!HcsServiceLogCatalog.Applications.IsKnown(name))
            {
                throw new ArgumentException($"Unknown application '{name}'.", nameof(applications));
            }

            if (!selected.Contains(name, StringComparer.Ordinal))
            {
                selected.Add(name);
            }
        }

        if (selected.Count == HcsServiceLogCatalog.Applications.All.Count)
        {
            return [];
        }

        return selected;
    }

    private static List<string> NormalizeLevels(IReadOnlyList<string>? levels)
    {
        if (levels is null || levels.Count == 0)
        {
            return [.. HcsServiceLogCatalog.Levels.Default];
        }

        var selected = new List<string>();
        foreach (var level in levels)
        {
            if (string.IsNullOrWhiteSpace(level))
            {
                continue;
            }

            var name = HcsServiceLogCatalog.Levels.Allowed
                .FirstOrDefault(allowed => string.Equals(allowed, level.Trim(), StringComparison.OrdinalIgnoreCase));
            if (name is null)
            {
                throw new ArgumentException($"Unknown level '{level}'.", nameof(levels));
            }

            if (!selected.Contains(name, StringComparer.Ordinal))
            {
                selected.Add(name);
            }
        }

        return selected.Count == 0 ? [.. HcsServiceLogCatalog.Levels.Default] : selected;
    }

    private static string? NormalizeKeyword(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return null;
        }

        var value = keyword.Trim();
        if (value.Length > MaxFilterLength)
        {
            throw new ArgumentException("Filter is too long.", nameof(keyword));
        }

        return value;
    }

    private static string InClause(string property, IReadOnlyList<string> values)
    {
        if (values.Count == 1)
        {
            return $"{property} = '{Escape(values[0])}'";
        }

        var builder = new StringBuilder();
        builder.Append(property).Append(" in [");
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append('\'').Append(Escape(values[index])).Append('\'');
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "''", StringComparison.Ordinal);
}

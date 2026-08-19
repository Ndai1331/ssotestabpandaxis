using System;
using System.Collections.Generic;
using System.Linq;

namespace HCS.Blazor.Client.Components;

public static class CatalogSelect2Text
{
    public static string CodeName(string? code, string? name)
    {
        var trimmedCode = code?.Trim() ?? "";
        var trimmedName = name?.Trim() ?? "";
        if (trimmedCode.Length == 0) return trimmedName;
        if (trimmedName.Length == 0) return trimmedCode;
        return $"{trimmedCode} - {trimmedName}";
    }
}

/// <summary>
/// Helpers for pages that back a <c>CatalogSelect2</c> with a locally cached lookup list.
/// The cache keeps rows returned by remote searches so the selected item can still be
/// rendered after the search results are discarded.
/// </summary>
public static class CatalogSelect2Cache
{
    public static CatalogSelect2SearchResponse Merge<T>(List<T> cache, IReadOnlyList<T> found,
        Func<T, Guid> idOf, Func<T, string> textOf, bool more)
    {
        foreach (var item in found)
        {
            if (cache.All(x => idOf(x) != idOf(item))) cache.Add(item);
        }
        return new(found.Select(x => new CatalogSelect2Item(idOf(x).ToString(), textOf(x))).ToList(), more);
    }

    public static string TextFor<T>(IEnumerable<T> cache, Guid? id, Func<T, Guid> idOf, Func<T, string> textOf) =>
        id is null ? "" : cache.Where(x => idOf(x) == id).Select(textOf).FirstOrDefault() ?? "";

    public static IReadOnlyList<string> TextsFor<T>(IEnumerable<T> cache, IEnumerable<Guid> ids,
        Func<T, Guid> idOf, Func<T, string> textOf) =>
        ids.Select(id => TextFor(cache, id, idOf, textOf)).ToList();
}

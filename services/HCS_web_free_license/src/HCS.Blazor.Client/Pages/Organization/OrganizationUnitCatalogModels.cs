using System;
using System.Collections.Generic;
using System.Linq;
using HCS.Blazor.Client.Services;
using HCS.OrganizationUnits;

namespace HCS.Blazor.Client.Pages.Organization;

public sealed class OrganizationUnitTreeNode
{
    public OrganizationUnitTreeNode(OrganizationUnitDto source)
    {
        Id = source.Id;
        ParentId = source.ParentId;
        Code = source.Code;
        DisplayName = source.DisplayName;
        ConcurrencyStamp = source.ConcurrencyStamp;
    }

    public Guid Id { get; }
    public Guid? ParentId { get; }
    public string Code { get; }
    public string DisplayName { get; }
    public string ConcurrencyStamp { get; }
    public bool IsExpanded { get; set; } = true;
    public List<OrganizationUnitTreeNode> Children { get; } = [];
    public bool HasChildren => Children.Count > 0;
}

public enum OrganizationUnitAction
{
    AddSubUnit,
    Edit,
    Move,
    Delete,
    MoveAllMembers
}

public sealed record OrganizationUnitActionRequest(
    OrganizationUnitTreeNode Node,
    OrganizationUnitAction Action);

public sealed class OrganizationUnitFormModel
{
    public string DisplayName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}

public static class OrganizationUnitCatalogMapper
{
    public static DepartmentCatalogDto ToDepartment(OrganizationUnitDto unit) =>
        new(unit.Id, unit.Code, unit.DisplayName, unit.ParentId, 0, true);

    public static string DisplayText(DepartmentCatalogDto department) =>
        string.IsNullOrWhiteSpace(department.Name) ? department.Code : department.Name;

    public static string HierarchicalText(
        DepartmentCatalogDto department,
        IReadOnlyDictionary<Guid, DepartmentCatalogDto> byId,
        Func<DepartmentCatalogDto, string>? textOf = null)
    {
        var depth = 0;
        var current = department.ParentId;
        while (current is { } parentId && byId.TryGetValue(parentId, out var parent) && depth < 16)
        {
            depth++;
            current = parent.ParentId;
        }

        var label = (textOf ?? DisplayText)(department);
        return depth == 0 ? label : string.Concat(Enumerable.Repeat("— ", depth)) + label;
    }

    public static Func<DepartmentCatalogDto, string> OptionText(IEnumerable<DepartmentCatalogDto> all)
    {
        var byId = all.ToDictionary(item => item.Id);
        return item => HierarchicalText(item, byId);
    }

    public static List<DepartmentCatalogDto> FilterTree(IEnumerable<DepartmentCatalogDto> source, string? filter)
    {
        var ordered = InTreeOrder(source);
        var term = SearchText.Normalize(filter);
        return string.IsNullOrWhiteSpace(term)
            ? ordered
            : ordered.Where(item =>
                    item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    item.Code.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    public static List<DepartmentCatalogDto> InTreeOrder(IEnumerable<DepartmentCatalogDto> source)
    {
        var items = source.ToList();
        var byId = items.ToDictionary(item => item.Id);
        var children = items.ToLookup(item => item.ParentId);
        var result = new List<DepartmentCatalogDto>(items.Count);

        void Walk(DepartmentCatalogDto node)
        {
            result.Add(node);
            foreach (var child in children[node.Id]
                .OrderBy(item => item.Code, StringComparer.Ordinal)
                .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                Walk(child);
            }
        }

        foreach (var root in items
            .Where(item => item.ParentId is null || !byId.ContainsKey(item.ParentId.Value))
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Walk(root);
        }

        return result;
    }

    public static UserDepartmentLookupDto ToUserDepartment(UserOrganizationUnitLookupDto row) =>
        new(row.UserId, row.OrganizationUnitId, row.DisplayName, DepartmentIds: row.OrganizationUnitIds);

    public static OrganizationPagedResponse<DepartmentCatalogDto> Search(
        IReadOnlyList<DepartmentCatalogDto> source,
        string? filter,
        int skipCount,
        int maxResultCount)
    {
        var filtered = FilterTree(source, filter);
        var skip = Math.Max(0, skipCount);
        var take = Math.Clamp(maxResultCount, 1, 100);
        return new(filtered.Count, filtered.Skip(skip).Take(take).ToList());
    }
}

public static class OrganizationUnitTreeBuilder
{
    public static List<OrganizationUnitTreeNode> Build(
        IEnumerable<OrganizationUnitDto> source,
        IReadOnlyDictionary<Guid, bool>? expandedState = null)
    {
        var nodes = source
            .OrderBy(unit => unit.Code, StringComparer.Ordinal)
            .Select(unit => new OrganizationUnitTreeNode(unit))
            .ToDictionary(node => node.Id);

        foreach (var node in nodes.Values)
        {
            if (expandedState is not null && expandedState.TryGetValue(node.Id, out var expanded))
            {
                node.IsExpanded = expanded;
            }

            if (node.ParentId.HasValue && nodes.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
        }

        return nodes.Values
            .Where(node => !node.ParentId.HasValue || !nodes.ContainsKey(node.ParentId.Value))
            .OrderBy(node => node.Code, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyDictionary<Guid, bool> CaptureExpandedState(
        IEnumerable<OrganizationUnitTreeNode> roots) =>
        Flatten(roots).ToDictionary(node => node.Id, node => node.IsExpanded);

    public static IEnumerable<OrganizationUnitTreeNode> Flatten(
        IEnumerable<OrganizationUnitTreeNode> roots)
    {
        foreach (var node in roots)
        {
            yield return node;
            foreach (var child in Flatten(node.Children))
            {
                yield return child;
            }
        }
    }
}

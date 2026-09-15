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

    public static UserDepartmentLookupDto ToUserDepartment(UserOrganizationUnitLookupDto row) =>
        new(row.UserId, row.OrganizationUnitId, row.DisplayName);

    public static OrganizationPagedResponse<DepartmentCatalogDto> Search(
        IReadOnlyList<DepartmentCatalogDto> source,
        string? filter,
        int skipCount,
        int maxResultCount)
    {
        var term = SearchText.Normalize(filter);
        IReadOnlyList<DepartmentCatalogDto> filtered = string.IsNullOrWhiteSpace(term)
            ? source
            : source.Where(item =>
                    item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    item.Code.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
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

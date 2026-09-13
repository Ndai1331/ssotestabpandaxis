using System;
using System.Collections.Generic;
using System.Linq;
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

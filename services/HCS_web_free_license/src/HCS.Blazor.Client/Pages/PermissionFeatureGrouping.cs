using System;
using System.Collections.Generic;
using System.Linq;
using HCS.Permissions;

namespace HCS.Blazor.Client.Pages;

internal sealed class PermissionFeatureRow
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public IdentityAdminPermission? Access { get; init; }
    public IdentityAdminPermission? Create { get; init; }
    public IdentityAdminPermission? Update { get; init; }
    public IdentityAdminPermission? Delete { get; init; }
    public List<PermissionFeatureAction> Extra { get; init; } = [];

    public IEnumerable<IdentityAdminPermission> AllPermissions()
    {
        if (Access is not null)
        {
            yield return Access;
        }

        if (Create is not null)
        {
            yield return Create;
        }

        if (Update is not null)
        {
            yield return Update;
        }

        if (Delete is not null)
        {
            yield return Delete;
        }

        foreach (var extra in Extra)
        {
            yield return extra.Permission;
        }
    }
}

internal sealed class PermissionFeatureAction
{
    public required string Label { get; init; }
    public required IdentityAdminPermission Permission { get; init; }
}

internal static class PermissionFeatureGrouper
{
    private const string ViewSuffix = ".View";

    private static readonly string[] CrudSuffixes =
    [
        HcsCrudPermissions.CreateSuffix,
        HcsCrudPermissions.UpdateSuffix,
        HcsCrudPermissions.DeleteSuffix
    ];

    public static IReadOnlyList<PermissionFeatureRow> Group(
        IReadOnlyList<IdentityAdminPermission> permissions,
        Func<string, string, string> label)
    {
        if (permissions.Count == 0)
        {
            return [];
        }

        var byName = permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission.Name))
            .GroupBy(permission => permission.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var grouped = new Dictionary<string, List<IdentityAdminPermission>>(StringComparer.Ordinal);
        foreach (var permission in byName.Values)
        {
            var rootName = RootName(permission, byName);
            if (!grouped.TryGetValue(rootName, out var members))
            {
                grouped[rootName] = members = [];
            }

            members.Add(permission);
        }

        var features = new List<PermissionFeatureRow>();
        var consumed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (rootName, members) in grouped)
        {
            var hasTree = members.Count > 1 ||
                          members.Any(member => !string.Equals(member.Name, rootName, StringComparison.Ordinal));
            if (!hasTree)
            {
                continue;
            }

            features.Add(CreateFeature(rootName, byName.GetValueOrDefault(rootName), members, label, synthetic: false));
            foreach (var member in members)
            {
                consumed.Add(member.Name);
            }
        }

        var leftover = byName.Values.Where(permission => !consumed.Contains(permission.Name)).ToList();
        foreach (var feature in BuildSyntheticFeatures(leftover, label))
        {
            features.Add(feature);
            foreach (var permission in feature.AllPermissions())
            {
                consumed.Add(permission.Name);
            }
        }

        foreach (var permission in leftover
                     .Where(item => !consumed.Contains(item.Name))
                     .OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            features.Add(CreateFeature(permission.Name, permission, [permission], label, synthetic: false));
        }

        return features
            .OrderBy(feature => feature.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(feature => feature.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<PermissionFeatureRow> BuildSyntheticFeatures(
        IReadOnlyList<IdentityAdminPermission> leftover,
        Func<string, string, string> label)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var prefixGroups = leftover
            .Select(permission => (Permission: permission, Prefix: ParentPrefix(permission.Name)))
            .Where(item => item.Prefix is not null)
            .GroupBy(item => item.Prefix!, StringComparer.Ordinal)
            .OrderByDescending(group => group.Key.Count(character => character == '.'));

        foreach (var prefixGroup in prefixGroups)
        {
            var members = prefixGroup
                .Select(item => item.Permission)
                .Where(permission => !used.Contains(permission.Name))
                .ToList();
            if (members.Count < 2 || !HasCrudFamilySegment(members))
            {
                continue;
            }

            yield return CreateFeature(prefixGroup.Key, null, members, label, synthetic: true);
            foreach (var member in members)
            {
                used.Add(member.Name);
            }
        }
    }

    private static PermissionFeatureRow CreateFeature(
        string name,
        IdentityAdminPermission? access,
        IReadOnlyList<IdentityAdminPermission> members,
        Func<string, string, string> label,
        bool synthetic)
    {
        access ??= members.FirstOrDefault(permission =>
            string.Equals(permission.Name, name, StringComparison.Ordinal) ||
            (synthetic && string.Equals(permission.Name, name + ViewSuffix, StringComparison.Ordinal)));

        var create = PickSuffix(members, name, access, HcsCrudPermissions.CreateSuffix);
        var update = PickSuffix(members, name, access, HcsCrudPermissions.UpdateSuffix);
        var delete = PickSuffix(members, name, access, HcsCrudPermissions.DeleteSuffix);

        var slotted = new HashSet<IdentityAdminPermission>();
        if (access is not null)
        {
            slotted.Add(access);
        }

        if (create is not null)
        {
            slotted.Add(create);
        }

        if (update is not null)
        {
            slotted.Add(update);
        }

        if (delete is not null)
        {
            slotted.Add(delete);
        }

        var extra = members
            .Where(permission => !slotted.Contains(permission))
            .OrderBy(permission => permission.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(permission => permission.Name, StringComparer.Ordinal)
            .Select(permission => new PermissionFeatureAction
            {
                Label = label(permission.Name, permission.DisplayName),
                Permission = permission
            })
            .ToList();

        var displayName = synthetic || access is null
            ? label(name, LastSegment(name))
            : label(access.Name, access.DisplayName);

        return new PermissionFeatureRow
        {
            Key = name,
            Name = name,
            DisplayName = displayName,
            Access = access,
            Create = create,
            Update = update,
            Delete = delete,
            Extra = extra
        };
    }

    private static IdentityAdminPermission? PickSuffix(
        IReadOnlyList<IdentityAdminPermission> members,
        string parentName,
        IdentityAdminPermission? access,
        string suffix)
    {
        var expected = parentName + suffix;
        return members.FirstOrDefault(permission =>
            !ReferenceEquals(permission, access) &&
            string.Equals(permission.Name, expected, StringComparison.Ordinal));
    }

    private static string RootName(
        IdentityAdminPermission permission,
        IReadOnlyDictionary<string, IdentityAdminPermission> byName)
    {
        var current = permission;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (seen.Add(current.Name))
        {
            var parentName = DirectParentName(current, byName);
            if (parentName is null)
            {
                return current.Name;
            }

            if (!byName.TryGetValue(parentName, out current))
            {
                return parentName;
            }
        }

        return permission.Name;
    }

    private static string? DirectParentName(
        IdentityAdminPermission permission,
        IReadOnlyDictionary<string, IdentityAdminPermission> byName)
    {
        if (!string.IsNullOrWhiteSpace(permission.ParentName) &&
            byName.ContainsKey(permission.ParentName))
        {
            return permission.ParentName;
        }

        foreach (var suffix in CrudSuffixes)
        {
            if (permission.Name.EndsWith(suffix, StringComparison.Ordinal) &&
                permission.Name.Length > suffix.Length)
            {
                var parentName = permission.Name[..^suffix.Length];
                if (byName.ContainsKey(parentName))
                {
                    return parentName;
                }
            }
        }

        return null;
    }

    private static bool HasCrudFamilySegment(IReadOnlyList<IdentityAdminPermission> members)
    {
        foreach (var member in members)
        {
            var segment = LastSegment(member.Name);
            if (segment.Equals("View", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("Create", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("Delete", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ParentPrefix(string name)
    {
        var separator = name.LastIndexOf('.');
        return separator > 0 ? name[..separator] : null;
    }

    private static string LastSegment(string name)
    {
        var separator = name.LastIndexOf('.');
        return separator >= 0 && separator < name.Length - 1 ? name[(separator + 1)..] : name;
    }
}

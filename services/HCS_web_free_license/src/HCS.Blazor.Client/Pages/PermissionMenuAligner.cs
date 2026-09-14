using System;
using System.Collections.Generic;
using System.Linq;
using HCS.Permissions;

namespace HCS.Blazor.Client.Pages;

internal static class PermissionMenuAligner
{
    public static List<IdentityAdminPermissionGroup> Align(
        IReadOnlyList<IdentityAdminPermissionGroup> source,
        Func<string, string> localize)
    {
        var mapped = new Dictionary<string, List<IdentityAdminPermission>>(StringComparer.Ordinal);
        var leftovers = new List<(string GroupName, string DisplayName, IdentityAdminPermission Permission)>();

        foreach (var group in source)
        {
            foreach (var permission in group.Permissions)
            {
                if (string.IsNullOrWhiteSpace(permission.Name))
                {
                    continue;
                }

                var menuGroup = HCSPermissionMenuMap.ResolveGroupName(permission.Name);
                if (menuGroup is null)
                {
                    leftovers.Add((group.Name, group.DisplayName, permission));
                    continue;
                }

                if (!mapped.TryGetValue(menuGroup, out var members))
                {
                    mapped[menuGroup] = members = [];
                }

                members.Add(permission);
            }
        }

        var result = new List<IdentityAdminPermissionGroup>();
        foreach (var menu in HCSPermissionMenuMap.Groups)
        {
            if (!mapped.TryGetValue(menu.Name, out var permissions) || permissions.Count == 0)
            {
                continue;
            }

            result.Add(new IdentityAdminPermissionGroup
            {
                Name = menu.Name,
                DisplayName = localize(menu.DisplayNameKey),
                Permissions = permissions
            });
        }

        foreach (var leftoverGroup in leftovers.GroupBy(item => item.GroupName, StringComparer.Ordinal))
        {
            result.Add(new IdentityAdminPermissionGroup
            {
                Name = leftoverGroup.Key,
                DisplayName = leftoverGroup.First().DisplayName,
                Permissions = leftoverGroup.Select(item => item.Permission).ToList()
            });
        }

        return result;
    }
}

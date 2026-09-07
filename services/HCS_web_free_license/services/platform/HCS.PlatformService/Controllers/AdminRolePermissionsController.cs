using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.PermissionManagement;

namespace HCS.PlatformService.Controllers;

/// <summary>
/// BFF-facing role permission write endpoint. It avoids the generic permission-management
/// model binder ambiguity while preserving ABP's existing permission definitions and store.
/// </summary>
[ApiController]
[Route("api/admin/roles")]
[Authorize]
public sealed class AdminRolePermissionsController(
    IPermissionManager permissionManager,
    IPermissionGrantRepository permissionGrants) : ControllerBase
{
    [HttpPut("{roleName}/permissions")]
    public async Task<IActionResult> UpdateAsync(string roleName, [FromBody] JsonElement request)
    {
        if (!User.HasClaim("role", "admin") && !User.IsInRole("admin"))
        {
            return Forbid();
        }

        // The built-in administrator is intentionally immutable in this screen:
        // data seeding grants it every enabled policy so there is always a recovery role.
        if (string.Equals(roleName, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("The admin role always has all enabled permissions.");
        }

        if (string.IsNullOrWhiteSpace(roleName) ||
            !TryGetProperty(request, "permissions", out var permissions) ||
            permissions.ValueKind != JsonValueKind.Array)
        {
            return BadRequest();
        }

        var requested = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var permission in permissions.EnumerateArray())
        {
            if (!TryGetProperty(permission, "name", out var nameProperty) ||
                !TryGetProperty(permission, "isGranted", out var grantedProperty))
            {
                return BadRequest();
            }

            var name = nameProperty.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            requested[name.Trim()] = grantedProperty.GetBoolean();
        }

        if (requested.Count == 0)
            return NoContent();

        // Read the role's current grants once and avoid a write/cache invalidation
        // for every permission whose value is already correct.
        var current = await permissionGrants.GetListAsync(
            RolePermissionValueProvider.ProviderName, roleName, HttpContext.RequestAborted);
        var currentNames = current.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var change in requested)
        {
            var isCurrentlyGranted = currentNames.Contains(change.Key);
            if (isCurrentlyGranted == change.Value)
                continue;

            await permissionManager.SetAsync(
                change.Key,
                RolePermissionValueProvider.ProviderName,
                roleName,
                change.Value);
        }

        return NoContent();
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}

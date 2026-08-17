using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Volo.Abp.Security.Claims;

namespace hanhchinhso.AuthServer;

/// <summary>
/// On Keycloak OIDC token validation: require bd-app-hcs, then map groups → roles.
/// </summary>
public static class KeycloakOpenIdConnectEvents
{
    public static void Configure(OpenIdConnectOptions options)
    {
        var previousRedirect = options.Events.OnRedirectToIdentityProvider;
        options.Events.OnRedirectToIdentityProvider = async context =>
        {
            if (previousRedirect != null)
            {
                await previousRedirect(context);
            }

            // Force credentials form even if Keycloak SSO cookie still exists
            context.ProtocolMessage.Prompt = "login";
        };

        var previous = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
            if (previous != null)
            {
                await previous(context);
            }

            var principal = context.Principal;
            if (principal?.Identity is not ClaimsIdentity identity)
            {
                return;
            }

            var groups = principal.FindAll(KeycloakGroupRoleMapper.GroupsClaim)
                .Select(c => c.Value)
                .Concat(principal.FindAll("group").Select(c => c.Value))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Also accept JSON array style single claim
            var groupsJson = principal.FindFirst(KeycloakGroupRoleMapper.GroupsClaim)?.Value;
            if (groups.Count == 0 && !string.IsNullOrWhiteSpace(groupsJson) && groupsJson.StartsWith('['))
            {
                try
                {
                    groups = System.Text.Json.JsonSerializer.Deserialize<List<string>>(groupsJson) ?? [];
                }
                catch
                {
                    // ignore
                }
            }

            if (!KeycloakGroupRoleMapper.HasAppAccess(groups))
            {
                context.Fail(
                    $"User is not entitled to ABP (missing Keycloak group '{KeycloakGroupRoleMapper.AppAccessGroup}').");
                return;
            }

            var roles = KeycloakGroupRoleMapper.ResolveRoles(groups);
            foreach (var role in roles)
            {
                if (!identity.HasClaim(ClaimTypes.Role, role) &&
                    !identity.HasClaim(AbpClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(AbpClaimTypes.Role, role));
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst("email")?.Value;
            if (!string.IsNullOrWhiteSpace(email) && identity.FindFirst(AbpClaimTypes.Email) == null)
            {
                identity.AddClaim(new Claim(AbpClaimTypes.Email, email));
            }
        };
    }
}

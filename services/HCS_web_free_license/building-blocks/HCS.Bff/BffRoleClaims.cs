using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace HCS.Bff;

/// <summary>
/// OIDC tickets keep roles as JWT <c>role</c> while cookie identities default
/// <see cref="ClaimsIdentity.RoleClaimType"/> to <see cref="ClaimTypes.Role"/>.
/// <see cref="ClaimsPrincipal.IsInRole"/> only looks at RoleClaimType, so Blazor
/// SSR <c>[Authorize(Roles)]</c> fails on F5 until the claims are mirrored.
/// </summary>
public static class BffRoleClaims
{
    public const string JwtRole = "role";

    public static void EnsureAspNetRoleClaims(ClaimsIdentity identity)
    {
        foreach (var role in identity.FindAll(JwtRole)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray())
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    public static void MergeAccessTokenClaims(ClaimsIdentity identity, IEnumerable<Claim> accessTokenClaims)
    {
        foreach (var claim in accessTokenClaims)
        {
            if (claim.Type == "permission" && !identity.HasClaim("permission", claim.Value))
            {
                identity.AddClaim(new Claim("permission", claim.Value));
            }

            if ((claim.Type == JwtRole || claim.Type == ClaimTypes.Role) &&
                !identity.HasClaim(JwtRole, claim.Value))
            {
                identity.AddClaim(new Claim(JwtRole, claim.Value));
            }
        }

        EnsureAspNetRoleClaims(identity);
    }
}

public sealed class BffRoleClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        if (identity.FindAll(BffRoleClaims.JwtRole)
            .All(role => identity.HasClaim(ClaimTypes.Role, role.Value)))
        {
            return Task.FromResult(principal);
        }

        var clone = principal.Clone();
        if (clone.Identity is ClaimsIdentity clonedIdentity)
        {
            BffRoleClaims.EnsureAspNetRoleClaims(clonedIdentity);
        }

        return Task.FromResult(clone);
    }
}

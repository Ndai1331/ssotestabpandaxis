using System.Security.Claims;
using HCS.Bff;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class BffRoleClaimsTests
{
    [Fact]
    public async Task Transformation_maps_jwt_role_so_IsInRole_succeeds()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(BffRoleClaims.JwtRole, "admin")],
            authenticationType: "Cookies"));

        Assert.False(principal.IsInRole("admin"));

        var transformed = await new BffRoleClaimsTransformation().TransformAsync(principal);

        Assert.True(transformed.IsInRole("admin"));
        Assert.Contains(transformed.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "admin");
    }

    [Fact]
    public async Task Transformation_leaves_anonymous_principals_unchanged()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var transformed = await new BffRoleClaimsTransformation().TransformAsync(principal);

        Assert.False(transformed.Identity?.IsAuthenticated ?? false);
        Assert.Empty(transformed.Claims);
    }

    [Fact]
    public void MergeAccessTokenClaims_copies_roles_and_permissions_from_the_access_token()
    {
        var identity = new ClaimsIdentity(authenticationType: "Cookies");

        BffRoleClaims.MergeAccessTokenClaims(identity,
        [
            new Claim("permission", "WorkManagement.Dashboard"),
            new Claim(BffRoleClaims.JwtRole, "admin")
        ]);

        var principal = new ClaimsPrincipal(identity);
        Assert.True(principal.IsInRole("admin"));
        Assert.True(principal.HasClaim("permission", "WorkManagement.Dashboard"));
        Assert.True(principal.HasClaim(BffRoleClaims.JwtRole, "admin"));
        Assert.True(principal.HasClaim(ClaimTypes.Role, "admin"));
    }
}

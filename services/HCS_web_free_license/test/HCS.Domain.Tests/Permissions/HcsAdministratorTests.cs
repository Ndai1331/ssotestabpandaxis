using System.Security.Claims;
using HCS.Permissions;
using Xunit;

namespace HCS;

public sealed class HcsAdministratorTests
{
    [Fact]
    public void Null_user_is_not_admin()
    {
        Assert.False(HcsAdministrator.Is(null));
    }

    [Fact]
    public void Short_role_claim_is_admin()
    {
        var user = Principal("role", "admin");
        Assert.True(HcsAdministrator.Is(user));
    }

    [Fact]
    public void Dotnet_role_claim_is_admin()
    {
        var identity = new ClaimsIdentity("test", ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
        Assert.True(HcsAdministrator.Is(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void Other_roles_are_not_admin()
    {
        Assert.False(HcsAdministrator.Is(Principal("role", "nhanvien")));
    }

    private static ClaimsPrincipal Principal(string type, string value) =>
        new(new ClaimsIdentity([new Claim(type, value)], "test"));
}

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class BffAccessTokenTransformTests
{
    [Fact]
    public void Mobile_bearer_replaces_copied_authorization_with_a_single_jwt()
    {
        var jwt = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJhZG1pbiJ9.signature";
        var http = new DefaultHttpContext();
        http.Request.Headers.Authorization = $"Bearer {jwt}";

        var proxy = new HttpRequestMessage();
        proxy.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");
        proxy.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");

        BffAccessTokenTransform.Apply(http, proxy);

        Assert.Single(proxy.Headers.GetValues("Authorization"));
        Assert.Equal("Bearer", proxy.Headers.Authorization?.Scheme);
        Assert.Equal(jwt, proxy.Headers.Authorization?.Parameter);
        Assert.Equal(2, jwt.Count(c => c == '.'));
    }

    [Fact]
    public void Bff_cookie_token_wins_over_copied_authorization()
    {
        var http = new DefaultHttpContext();
        http.Items[BffAccessTokenMiddleware.AccessTokenItemKey] = "cookie-access-token";
        http.Request.Headers.Authorization = "Bearer leftover-mobile-token";

        var proxy = new HttpRequestMessage();
        proxy.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "copied");

        BffAccessTokenTransform.Apply(http, proxy);

        Assert.Equal("cookie-access-token", proxy.Headers.Authorization?.Parameter);
    }

    [Fact]
    public void Anonymous_bootstrap_clears_copied_authorization()
    {
        var http = new DefaultHttpContext();
        var proxy = new HttpRequestMessage();
        proxy.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "should-not-forward");

        BffAccessTokenTransform.Apply(http, proxy);

        Assert.Null(proxy.Headers.Authorization);
        Assert.False(proxy.Headers.Contains("Authorization"));
    }
}

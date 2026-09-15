using HCS.Blazor.Client.Authentication;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class BffUnauthenticatedDocumentRedirectTests
{
    private const string HtmlAccept = "text/html,application/xhtml+xml;q=0.9,*/*;q=0.8";

    [Theory]
    [InlineData("/", "https://localhost:44403/")]
    [InlineData("/workspace", "https://localhost:44403/workspace")]
    [InlineData("/login", "https://localhost:44403/")]
    [InlineData("/Login", "https://localhost:44403/")]
    public void Redirects_html_documents_to_bff_login_without_booting_wasm(
        string path,
        string expectedReturnUrl)
    {
        Assert.True(BffUnauthenticatedDocumentRedirect.TryGetLoginUrl(
            isAuthenticated: false,
            httpMethod: "GET",
            path,
            queryString: null,
            acceptHeader: HtmlAccept,
            CreateConfiguration(),
            out var loginUrl));

        Assert.Equal(
            "https://localhost:44402/bff/login?returnUrl=" + Uri.EscapeDataString(expectedReturnUrl),
            loginUrl);
    }

    [Fact]
    public void Preserves_deep_link_query_on_the_post_login_return_url()
    {
        Assert.True(BffUnauthenticatedDocumentRedirect.TryGetLoginUrl(
            isAuthenticated: false,
            "GET",
            "/manage-documents",
            "?sourceType=2",
            HtmlAccept,
            CreateConfiguration(),
            out var loginUrl));

        Assert.Equal(
            "https://localhost:44402/bff/login?returnUrl=" +
            Uri.EscapeDataString("https://localhost:44403/manage-documents?sourceType=2"),
            loginUrl);
    }

    [Fact]
    public void Ignores_query_on_the_login_route_so_auth_returns_to_the_app_root()
    {
        var returnUrl = BffUnauthenticatedDocumentRedirect.BuildReturnUrl(
            CreateConfiguration(),
            "/login",
            "?returnUrl=/workspace");

        Assert.Equal("https://localhost:44403/", returnUrl);
    }

    [Theory]
    [InlineData(true, "GET", "/", HtmlAccept)]
    [InlineData(false, "POST", "/", HtmlAccept)]
    [InlineData(false, "GET", "/", "application/json")]
    [InlineData(false, "GET", "/survey-collections/3f1c0a5e-2b7a-4d9e-9c1f-8a6b5d4c3e2f", HtmlAccept)]
    [InlineData(false, "GET", "/_framework/blazor.web.js", HtmlAccept)]
    [InlineData(false, "GET", "/hcs-tokens.css", HtmlAccept)]
    [InlineData(false, "GET", "/culture", HtmlAccept)]
    public void Does_not_redirect_authenticated_non_document_or_anonymous_routes(
        bool isAuthenticated,
        string method,
        string path,
        string accept)
    {
        Assert.False(BffUnauthenticatedDocumentRedirect.ShouldRedirect(
            isAuthenticated,
            method,
            path,
            accept));
    }

    [Fact]
    public void Anonymous_survey_route_matcher_stays_aligned_with_the_blazor_gate()
    {
        Assert.True(BffAnonymousRoutes.IsAnonymous("survey-collections/3f1c0a5e-2b7a-4d9e-9c1f-8a6b5d4c3e2f?x=1"));
        Assert.False(BffAnonymousRoutes.IsAnonymous("survey-collections"));
        Assert.True(BffAnonymousRoutes.IsLogin("/login?foo=1"));
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bff:PublicOrigin"] = "https://localhost:44402/",
            ["App:SelfUrl"] = "https://localhost:44403"
        })
        .Build();
}

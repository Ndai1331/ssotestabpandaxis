using System.Net;
using System.Net.Http.Json;
using HCS.Blazor.Client.Branding;
using HCS.Blazor.Client.Navigation;
using HCS.Branding;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class SystemBrandingResourceUrlTests
{
    private const string GatewayOrigin = "https://qldhapi.bvlevanthinh.vn";

    [Theory]
    [InlineData("api/hcs/system-branding/assets/logo?v=1")]
    [InlineData("/api/hcs/system-branding/assets/logo?v=1")]
    public void Builder_resolves_both_api_path_forms_against_the_gateway(string path)
    {
        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/logo?v=1",
            GatewayResourceUrlBuilder.Build(Configuration(), path));
    }

    [Fact]
    public void Builder_preserves_an_absolute_asset_url()
    {
        const string url = "https://cdn.example.com/logo.png?v=2";
        Assert.Equal(url, GatewayResourceUrlBuilder.Build(Configuration(), url));
    }

    [Fact]
    public void Builder_uses_remote_service_origin_when_bff_origin_is_not_configured()
    {
        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/logo?v=1",
            GatewayResourceUrlBuilder.Build(Configuration("RemoteServices:Default:BaseUrl"),
                "/api/hcs/system-branding/assets/logo?v=1"));
    }

    [Theory]
    [InlineData("settings")]
    [InlineData("public")]
    [InlineData("update")]
    public async Task Client_normalizes_all_asset_urls_in_every_response(string operation)
    {
        using var handler = new BrandingResponseHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(GatewayOrigin) };
        var client = new SystemBrandingClient(new BrandingHttpClientFactory(httpClient), Configuration());

        var branding = operation switch
        {
            "settings" => await client.GetAsync(),
            "public" => await client.GetPublicAsync(),
            _ => await client.UpdateAsync("Hospital", "Description", false, false, false, null, null, null)
        };

        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/logo?v=1", branding.Logo?.Url);
        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/favicon?v=2", branding.Favicon?.Url);
        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/background?v=3", branding.Background?.Url);
        Assert.Equal(operation == "update" ? HttpMethod.Put : HttpMethod.Get, handler.Method);
        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding{(operation == "public" ? "/public" : "")}",
            handler.Url);
    }

    [Fact]
    public async Task State_passes_gateway_urls_to_settings_layout_and_loading_screen()
    {
        var js = new RecordingJsRuntime();
        var client = new SystemBrandingClient(null!, Configuration());
        await using var state = new SystemBrandingState(client, js);

        await state.ApplyAsync(Branding());

        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/logo?v=1", state.LogoUrl);
        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/favicon?v=2", state.FaviconUrl);
        Assert.Equal($"{GatewayOrigin}/api/hcs/system-branding/assets/background?v=3", state.BackgroundUrl);
        Assert.Equal($"--hcs-branding-background-image: url('{state.BackgroundUrl}')", state.BackgroundStyle);
        Assert.Equal("hcsApplySystemBranding", js.Identifier);
        Assert.Equal(new object?[] { state.Title, state.Description, state.LogoUrl, state.FaviconUrl, state.BackgroundUrl },
            js.Arguments);
    }

    [Fact]
    public async Task Missing_assets_keep_the_built_in_fallbacks()
    {
        await using var state = new SystemBrandingState(new SystemBrandingClient(null!, Configuration()),
            new RecordingJsRuntime());

        Assert.Equal("/images/logo/logo.png", state.LogoUrl);
        Assert.Equal("/favicon.ico", state.FaviconUrl);
        Assert.Null(state.BackgroundUrl);
        Assert.Empty(state.BackgroundStyle);
    }

    private static IConfiguration Configuration(string key = "Bff:PublicOrigin") => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { [key] = GatewayOrigin })
        .Build();

    private static SystemBrandingDto Branding() => new()
    {
        Revision = 3,
        Logo = new() { Url = "/api/hcs/system-branding/assets/logo?v=1" },
        Favicon = new() { Url = "/api/hcs/system-branding/assets/favicon?v=2" },
        Background = new() { Url = "/api/hcs/system-branding/assets/background?v=3" }
    };

    private sealed class BrandingHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal("HCS.Bff", name);
            return client;
        }
    }

    private sealed class BrandingResponseHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Url { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Url = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Branding()) });
        }
    }

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public string? Identifier { get; private set; }
        public object?[]? Arguments { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifier = identifier;
            Arguments = args;
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class GatewayJwtBearerTests
{
    // Per-hospital in deploy: Authentication__Authority=https://${HCS_AUTH_PUBLIC_HOST}
    private static string Authority =>
        Environment.GetEnvironmentVariable("Authentication__Authority")
        ?? "https://auth.hcs.localhost";


    [Fact]
    public void Accepts_openiddict_access_token_type_and_issuer_slash_variants()
    {
        var authority = Authority;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Authority"] = authority,
            ["Authentication:BearerAudience"] = "HCS"
        }).Build();

        var options = new JwtBearerOptions();
        GatewayJwtBearer.Configure(options, configuration, authority);

        var trimmed = authority.Trim().TrimEnd('/');
        Assert.Equal(authority, options.Authority);
        Assert.Equal("HCS", options.Audience);
        Assert.False(options.MapInboundClaims);
        Assert.Contains(GatewayJwtBearer.AccessTokenType, options.TokenValidationParameters.ValidTypes!);
        Assert.Contains(GatewayJwtBearer.JwtTokenType, options.TokenValidationParameters.ValidTypes!);
        Assert.Contains(trimmed, options.TokenValidationParameters.ValidIssuers!);
        Assert.Contains(trimmed + "/", options.TokenValidationParameters.ValidIssuers!);
    }

    [Fact]
    public void Internal_metadata_address_does_not_require_https_metadata()
    {
        var authority = Authority;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Authority"] = authority,
            ["Authentication:BearerMetadataAddress"] = "http://auth-server:8080/.well-known/openid-configuration",
            ["Authentication:RequireHttpsMetadata"] = "true"
        }).Build();

        var options = new JwtBearerOptions();
        GatewayJwtBearer.Configure(options, configuration, authority);

        Assert.Equal(
            "http://auth-server:8080/.well-known/openid-configuration",
            options.MetadataAddress);
        Assert.False(options.RequireHttpsMetadata);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;

namespace HCS.CollaborationService.Tests;

public sealed class BearerAuthContractTests
{
    [Fact]
    public void Bearer_apis_do_not_auto_validate_antiforgery_cookies()
    {
        var options = new AbpAntiForgeryOptions { AutoValidate = true };
        BearerApiAntiforgery.DisableCookieValidation(options);
        options.AutoValidate.ShouldBeFalse();
    }

    [Fact]
    public void Jwt_issuers_include_the_public_authority_with_and_without_slash()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AuthServer:Authority"] = "https://auth.hcs.localhost"
        }).Build();

        var issuers = CollaborationJwtBearer.ResolveIssuers(configuration);

        issuers.ShouldContain("https://auth.hcs.localhost");
        issuers.ShouldContain("https://auth.hcs.localhost/");
    }

    [Fact]
    public void Jwt_accepts_the_public_issuer_even_when_metadata_is_fetched_internally()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AuthServer:Authority"] = "https://auth.hcs.localhost",
            ["AuthServer:ValidIssuers:0"] = "http://auth-server:8080",
            ["AuthServer:AllowUntrustedBackchannelCertificate"] = "true",
            ["AuthServer:Audience"] = "HCS"
        }).Build();

        var options = new JwtBearerOptions();
        CollaborationJwtBearer.Configure(options, configuration);

        options.Authority.ShouldBe("https://auth.hcs.localhost");
        options.Audience.ShouldBe("HCS");
        options.MapInboundClaims.ShouldBeFalse();
        options.TokenValidationParameters.ValidTypes.ShouldContain("at+jwt");
        options.TokenValidationParameters.ValidTypes.ShouldContain("JWT");
        options.TokenValidationParameters.NameClaimType.ShouldBe(CollaborationJwtBearer.JwtSubjectClaim);
        options.TokenValidationParameters.RoleClaimType.ShouldBe(CollaborationJwtBearer.JwtRoleClaim);
        Volo.Abp.Security.Claims.AbpClaimTypes.UserId.ShouldBe(CollaborationJwtBearer.JwtSubjectClaim);
        Volo.Abp.Security.Claims.AbpClaimTypes.Role.ShouldBe(CollaborationJwtBearer.JwtRoleClaim);
        Volo.Abp.Security.Claims.AbpClaimTypes.UserName.ShouldBe("preferred_username");
        Volo.Abp.Security.Claims.AbpClaimTypes.Name.ShouldBe("given_name");
        Volo.Abp.Security.Claims.AbpClaimTypes.SurName.ShouldBe("family_name");
        options.BackchannelHttpHandler.ShouldNotBeNull();
        CollaborationJwtBearer.ResolveIssuers(configuration)
            .ShouldContain("https://auth.hcs.localhost/");
        CollaborationJwtBearer.ResolveIssuers(configuration)
            .ShouldContain("http://auth-server:8080/");
    }

    [Fact]
    public void Jwt_issuers_include_gateway_authentication_authority()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AuthServer:Authority"] = "https://auth.hcs.localhost",
            ["Authentication:Authority"] = "https://auth.example.hospital"
        }).Build();

        var issuers = CollaborationJwtBearer.ResolveIssuers(configuration);

        issuers.ShouldContain("https://auth.hcs.localhost/");
        issuers.ShouldContain("https://auth.example.hospital/");
    }

    [Fact]
    public void Force_default_schemes_keeps_jwt_bearer_after_later_openiddict_configure()
    {
        var services = new ServiceCollection();
        services.AddAuthentication("OpenIddict.Validation.AspNetCore");
        HCS.HcsServiceJwtBearer.ForceDefaultSchemes(services);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        options.DefaultScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
        options.DefaultAuthenticateScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
        options.DefaultChallengeScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
        options.DefaultForbidScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void Jwt_falls_back_to_authentication_authority_when_auth_server_is_missing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Authority"] = "https://auth.hcs.localhost",
            ["Authentication:BearerAudience"] = "HCS",
            ["Authentication:BearerMetadataAddress"] = "http://auth-server:8080/.well-known/openid-configuration"
        }).Build();

        var options = new JwtBearerOptions();
        CollaborationJwtBearer.Configure(options, configuration);

        options.Authority.ShouldBe("https://auth.hcs.localhost");
        options.Audience.ShouldBe("HCS");
        options.MetadataAddress.ShouldBe("http://auth-server:8080/.well-known/openid-configuration");
        options.RequireHttpsMetadata.ShouldBeFalse();
        options.Events.ShouldNotBeNull();
    }

    [Fact]
    public void Read_bearer_token_keeps_only_the_first_jwt_when_headers_are_joined()
    {
        const string jwt = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJhZG1pbiJ9.signature";
        var joined = $"Bearer {jwt}, Bearer {jwt}";

        HCS.HcsServiceJwtBearer.ReadBearerToken(joined).ShouldBe(jwt);
        HCS.HcsServiceJwtBearer.ReadBearerToken($"Bearer {jwt}").ShouldBe(jwt);
        HCS.HcsServiceJwtBearer.ReadBearerToken(null).ShouldBeNull();
    }
}

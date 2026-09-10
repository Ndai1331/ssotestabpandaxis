using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.Uow;

namespace HCS.OpenIddict;

/* Creates initial data that is needed to property run the application
 * and make client-to-server communication possible.
 */
public class OpenIddictDataSeedContributor : OpenIddictDataSeedContributorBase, IDataSeedContributor, ITransientDependency
{
    internal const string GatewayScope = "HCS";
    internal const string GatewayAudience = "HCS";
    internal const string DefaultGatewayRootUrl = "https://localhost:44402";
    internal const string DefaultBlazorRootUrl = "https://localhost:44403";
    public OpenIddictDataSeedContributor(
        IConfiguration configuration,
        IOpenIddictApplicationRepository openIddictApplicationRepository,
        IAbpApplicationManager applicationManager,
        IOpenIddictScopeRepository openIddictScopeRepository,
        IOpenIddictScopeManager scopeManager)
        : base(configuration, openIddictApplicationRepository, applicationManager, openIddictScopeRepository, scopeManager)
    {
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await CreateScopesAsync();
        await CreateApplicationsAsync();
    }

    private async Task CreateScopesAsync()
    {
        await CreateScopesAsync(new OpenIddictScopeDescriptor 
        {
            Name = GatewayScope,
            DisplayName = "HCS Web Gateway API",
            Resources = { GatewayAudience }
        });
    }

    private async Task CreateApplicationsAsync()
    {
        var commonScopes = new List<string> {
            OpenIddictConstants.Permissions.Scopes.Address,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            GatewayScope
        };

        var configurationSection = Configuration.GetSection("OpenIddict:Applications");


        // Console Test / Angular Client
        
        var app = GetHcsAppRegistration(configurationSection);
        if (app is not null)
        {
            await CreateOrUpdateApplicationAsync(
                applicationType: OpenIddictConstants.ApplicationTypes.Web,
                name: app.ClientId,
                type: OpenIddictConstants.ClientTypes.Confidential,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "HCS Blazor Application",
                secret: app.ClientSecret,
                grantTypes: new List<string> {
                    OpenIddictConstants.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.GrantTypes.RefreshToken,
                    "LinkLogin"
                },
                scopes: commonScopes,
                redirectUris: new List<string> { app.CallbackUrl },
                postLogoutRedirectUris: new List<string>
                {
                    app.LogoutCallbackUrl,
                    app.BlazorRootUrl,
                    $"{app.BlazorRootUrl}/login"
                }.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                clientUri: app.BlazorRootUrl,
                logoUri: "/images/clients/hcs.svg"
            );
        }

        await CreateMobileApplicationAsync(configurationSection.GetSection("HCS_Mobile"));

        
        




        // Swagger Client
        var swaggerClientId = configurationSection["HCS_Swagger:ClientId"];
        var swaggerRootUrl = configurationSection["HCS_Swagger:RootUrl"]?.TrimEnd('/');
        if (!swaggerClientId.IsNullOrWhiteSpace() && !swaggerRootUrl.IsNullOrWhiteSpace())
        {
            await CreateOrUpdateApplicationAsync(
                applicationType: OpenIddictConstants.ApplicationTypes.Web,
                name: swaggerClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "Swagger Application",
                secret: null,
                grantTypes: new List<string> { OpenIddictConstants.GrantTypes.AuthorizationCode, },
                scopes: commonScopes,
                redirectUris: new List<string> { $"{swaggerRootUrl}/swagger/oauth2-redirect.html" },
                clientUri: swaggerRootUrl!.EnsureEndsWith('/') + "swagger",
                logoUri: "/images/clients/swagger.svg"
            );
        }

        foreach (var serviceName in new[]
                 {
                     "PlatformService", "OrganizationService", "DocumentService",
                     "WorkManagementService", "CollaborationService"
                 })
        {
            var serviceSection = configurationSection.GetSection(serviceName);
            var clientId = serviceSection["ClientId"]?.Trim();
            var clientSecret = serviceSection["ClientSecret"];
            if (clientId.IsNullOrWhiteSpace() || clientSecret.IsNullOrWhiteSpace())
            {
                continue;
            }

            await CreateOrUpdateApplicationAsync(
                applicationType: OpenIddictConstants.ApplicationTypes.Web,
                name: clientId!,
                type: OpenIddictConstants.ClientTypes.Confidential,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: $"HCS {serviceName}",
                secret: clientSecret,
                grantTypes: new List<string> { OpenIddictConstants.GrantTypes.ClientCredentials },
                scopes: new List<string> { GatewayScope },
                clientUri: null,
                logoUri: null);
        }


    }

    private async Task CreateMobileApplicationAsync(IConfigurationSection mobileSection)
    {
        var clientId = mobileSection["ClientId"]?.Trim();
        if (clientId.IsNullOrWhiteSpace())
        {
            return;
        }

        var redirectUris = GetConfiguredUris(mobileSection, "RedirectUris", required: true);
        var postLogoutRedirectUris = GetConfiguredUris(mobileSection, "PostLogoutRedirectUris", required: false);
        var descriptor = new AbpApplicationDescriptor
        {
            ApplicationType = OpenIddictConstants.ApplicationTypes.Native,
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = mobileSection["DisplayName"]?.Trim() ?? "HCS Mobile Application",
            ClientUri = mobileSection["ClientUri"]?.Trim()
        };

        descriptor.Permissions.UnionWith(
        [
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.Revocation,
            OpenIddictConstants.Permissions.Endpoints.Introspection,
            OpenIddictConstants.Permissions.Endpoints.EndSession,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Scopes.Address,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            $"{OpenIddictConstants.Permissions.Prefixes.Scope}{OpenIddictConstants.Scopes.OfflineAccess}",
            $"{OpenIddictConstants.Permissions.Prefixes.Scope}{GatewayScope}"
        ]);
        descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        descriptor.RedirectUris.UnionWith(redirectUris);
        descriptor.PostLogoutRedirectUris.UnionWith(postLogoutRedirectUris);

        var existingApplication = await OpenIddictApplicationRepository.FindByClientIdAsync(clientId);
        if (existingApplication is null)
        {
            await ApplicationManager.CreateAsync(descriptor);
        }
        else
        {
            await ApplicationManager.UpdateAsync(existingApplication.ToModel(), descriptor);
        }
    }

    private static IReadOnlyCollection<Uri> GetConfiguredUris(
        IConfigurationSection section,
        string key,
        bool required)
    {
        var values = section.GetSection(key).Get<string[]>() ?? [];
        var uris = new HashSet<Uri>();
        foreach (var value in values)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
                !uri.IsWellFormedOriginalString() ||
                uri.IsFile)
            {
                throw new InvalidOperationException(
                    $"OpenIddict HCS_Mobile {key} contains an invalid absolute URI '{normalized}'.");
            }

            uris.Add(uri);
        }

        if (required && uris.Count == 0)
        {
            throw new InvalidOperationException(
                $"OpenIddict HCS_Mobile {key} must contain at least one URI when ClientId is configured.");
        }

        return uris;
    }

    internal static HcsAppRegistration? GetHcsAppRegistration(IConfigurationSection applications)
    {
        var clientId = applications["HCS_App:ClientId"]?.Trim();
        if (clientId.IsNullOrWhiteSpace()) return null;
        var clientSecret = applications["HCS_App:ClientSecret"];
        if (clientSecret.IsNullOrWhiteSpace())
            throw new InvalidOperationException("OpenIddict HCS_App ClientSecret must come from environment variables or User Secrets.");
        var gatewayRootUrl = ValidateHttpsOrigin(
            applications["HCS_App:RootUrl"] ?? DefaultGatewayRootUrl, "HCS_App RootUrl");
        var blazorRootUrl = ValidateHttpsOrigin(
            applications["HCS_App:PostLogoutRootUrl"] ?? DefaultBlazorRootUrl, "HCS_App PostLogoutRootUrl");
        return new HcsAppRegistration(clientId!, clientSecret!, gatewayRootUrl, blazorRootUrl,
            $"{gatewayRootUrl}/signin-oidc", $"{gatewayRootUrl}/signout-callback-oidc");
    }

    private static string ValidateHttpsOrigin(string value, string setting)
    {
        var origin = value.Trim().TrimEnd('/');
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException($"OpenIddict {setting} must be an HTTPS origin without path, query, or fragment.");
        return origin;
    }
}

internal sealed record HcsAppRegistration(string ClientId, string ClientSecret, string GatewayRootUrl,
    string BlazorRootUrl, string CallbackUrl, string LogoutCallbackUrl);

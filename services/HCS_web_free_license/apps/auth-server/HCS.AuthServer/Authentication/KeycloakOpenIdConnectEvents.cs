using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;

namespace HCS.AuthServer;

public static class KeycloakOpenIdConnectEvents
{
    public static OpenIdConnectEvents Create() => new()
    {
        OnRedirectToIdentityProvider = async context =>
        {
            var resolver = context.HttpContext.RequestServices.GetRequiredService<KeycloakSettingsResolver>();
            var previous = resolver.Current;
            var settings = await resolver.RefreshAsync(context.HttpContext.RequestAborted);
            if (!settings.Enabled)
            {
                context.HandleResponse();
                context.Response.Redirect("/Account/Login");
                return;
            }

            var forceRefresh = !string.Equals(previous.Revision, settings.Revision, StringComparison.Ordinal)
                || !string.Equals(previous.Authority, settings.Authority, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(previous.ClientSecret, settings.ClientSecret, StringComparison.Ordinal);
            KeycloakOpenIdConnectOptionsSetup.Apply(context.Options, settings, forceRefresh);
            context.ProtocolMessage.Prompt = "login";
        },
        OnTokenValidated = async context =>
        {
            if (context.Principal is null)
            {
                context.Fail("Keycloak did not return an authenticated principal.");
                return;
            }

            var resolver = context.HttpContext.RequestServices.GetRequiredService<KeycloakSettingsResolver>();
            var settings = resolver.Current;
            var result = KeycloakClaimsProcessor.Apply(
                context.Principal,
                settings.AppAccessGroup,
                settings.RoleMappings);
            if (!result.IsAllowed)
            {
                context.Fail(result.FailureReason!);
                return;
            }

            try
            {
                var provisioner = context.HttpContext.RequestServices.GetRequiredService<IKeycloakUserProvisioner>();
                await provisioner.ProvisionAsync(
                    context.Principal,
                    result.Roles,
                    context.HttpContext.RequestAborted);
            }
            catch (KeycloakProvisioningException exception)
            {
                context.Fail(exception.Message);
            }
        }
    };
}

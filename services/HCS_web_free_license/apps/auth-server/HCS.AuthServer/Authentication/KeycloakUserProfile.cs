using System.Security.Claims;

namespace HCS.AuthServer;

public sealed record KeycloakUserProfile(
    string Subject,
    string UserName,
    string? VerifiedEmail,
    IReadOnlyList<string> Roles)
{
    public static KeycloakUserProfile FromPrincipal(ClaimsPrincipal principal, IReadOnlyList<string> roles)
    {
        var subject = principal.FindFirst("sub")?.Value;
        var userName = principal.FindFirst("preferred_username")?.Value
                       ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        var email = principal.FindFirst("email")?.Value
                    ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        var emailVerified = string.Equals(
            principal.FindFirst("email_verified")?.Value,
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new KeycloakProvisioningException("Keycloak subject is missing.");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new KeycloakProvisioningException("Keycloak preferred_username is missing.");
        }

        return new KeycloakUserProfile(
            subject.Trim(),
            userName.Trim(),
            emailVerified && !string.IsNullOrWhiteSpace(email) ? email.Trim() : null,
            roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}

public sealed class KeycloakProvisioningException(string message) : Exception(message);

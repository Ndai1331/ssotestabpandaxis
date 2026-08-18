namespace HCS.AuthServer;

public sealed class KeycloakOptions
{
    public const string SectionName = "Authentication:Keycloak";
    public const string Scheme = "Keycloak";
    public const string CallbackPath = "/signin-oidc";

    public bool Enabled { get; set; }

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;
}

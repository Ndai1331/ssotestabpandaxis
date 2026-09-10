namespace HCS.Settings;

public sealed class AuthenticationSettingsDto
{
    public bool ShowSsoLoginButton { get; set; }
}

public sealed class UpdateAuthenticationSettingsDto
{
    public bool ShowSsoLoginButton { get; set; }
}

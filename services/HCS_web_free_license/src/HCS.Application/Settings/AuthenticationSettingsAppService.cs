using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.SettingManagement;

namespace HCS.Settings;

[RemoteService(IsEnabled = false)]
[Authorize(HCSPermissions.SystemBranding.Update)]
public class AuthenticationSettingsAppService(
    ISettingManager settingManager,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : HCSAppService, IAuthenticationSettingsAppService
{
    public const string HttpClientName = "HCS.KeycloakSettings";

    public async Task<AuthenticationSettingsDto> GetAsync()
    {
        var resolved = KeycloakSettingsResolution.Resolve(await ReadStoredAsync(), FromEnvironment());
        return new AuthenticationSettingsDto
        {
            ShowSsoLoginButton = resolved.ShowSsoLoginButton,
            Enabled = resolved.Enabled,
            BaseUrl = resolved.BaseUrl,
            Realm = resolved.Realm,
            ClientId = resolved.ClientId,
            HasClientSecret = resolved.HasClientSecret,
            RequireHttpsMetadata = resolved.RequireHttpsMetadata,
            AdminUser = resolved.AdminUser,
            HasAdminSecret = resolved.HasAdminSecret,
            AppAccessGroup = resolved.AppAccessGroup,
            RoleMappings = [.. resolved.RoleMappings]
        };
    }

    public async Task UpdateAsync(UpdateAuthenticationSettingsDto input)
    {
        Check.NotNull(input, nameof(input));
        if (input.Enabled)
        {
            Check.NotNullOrWhiteSpace(input.BaseUrl, nameof(input.BaseUrl));
            Check.NotNullOrWhiteSpace(input.ClientId, nameof(input.ClientId));
        }

        var mappings = KeycloakRoleMappingSerializer.Normalize(input.RoleMappings);
        await settingManager.SetGlobalAsync(HCSSettings.ShowSsoLoginButton, Bool(input.ShowSsoLoginButton));
        await settingManager.SetGlobalAsync(HCSSettings.KeycloakEnabled, Bool(input.Enabled));
        await settingManager.SetGlobalAsync(HCSSettings.KeycloakBaseUrl, Trim(input.BaseUrl).TrimEnd('/'));
        await settingManager.SetGlobalAsync(
            HCSSettings.KeycloakRealm,
            string.IsNullOrWhiteSpace(input.Realm) ? KeycloakSettingDefaults.Realm : input.Realm.Trim());
        await settingManager.SetGlobalAsync(HCSSettings.KeycloakClientId, Trim(input.ClientId));
        if (KeycloakSettingsResolution.ShouldReplaceSecret(input.ClientSecret))
        {
            await settingManager.SetGlobalAsync(HCSSettings.KeycloakClientSecret, input.ClientSecret!.Trim());
        }

        await settingManager.SetGlobalAsync(
            HCSSettings.KeycloakRequireHttpsMetadata,
            Bool(input.RequireHttpsMetadata));
        await settingManager.SetGlobalAsync(HCSSettings.KeycloakAdminUser, Trim(input.AdminUser));
        if (KeycloakSettingsResolution.ShouldReplaceSecret(input.AdminSecret))
        {
            await settingManager.SetGlobalAsync(HCSSettings.KeycloakAdminSecret, input.AdminSecret!.Trim());
        }

        await settingManager.SetGlobalAsync(
            HCSSettings.KeycloakAppAccessGroup,
            string.IsNullOrWhiteSpace(input.AppAccessGroup)
                ? KeycloakSettingDefaults.AppAccessGroup
                : input.AppAccessGroup.Trim().TrimStart('/'));
        await settingManager.SetGlobalAsync(
            HCSSettings.KeycloakRoleMappings,
            KeycloakRoleMappingSerializer.Serialize(mappings));

        var revision = await settingManager.GetOrNullGlobalAsync(HCSSettings.KeycloakRevision, fallback: false);
        await settingManager.SetGlobalAsync(
            HCSSettings.KeycloakRevision,
            KeycloakSettingsResolution.NextRevision(revision));
    }

    public async Task<AuthenticationConnectionTestResultDto> TestAsync(UpdateAuthenticationSettingsDto input)
    {
        Check.NotNull(input, nameof(input));
        var stored = await ReadStoredAsync();
        var resolved = KeycloakSettingsResolution.Resolve(MergeInput(input, stored), FromEnvironment());
        if (string.IsNullOrWhiteSpace(resolved.Authority))
        {
            return Failed(L["Settings:SsoTestMissingUrl"]);
        }

        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var discoveryResponse = await client.GetAsync(KeycloakAuthority.DiscoveryUrl(resolved.Authority));
            if (!discoveryResponse.IsSuccessStatusCode)
            {
                return Failed(string.Format(
                    L["Settings:SsoTestDiscoveryFailed"],
                    (int)discoveryResponse.StatusCode));
            }

            var discoveryJson = await discoveryResponse.Content.ReadAsStringAsync();
            if (!discoveryJson.Contains("issuer", StringComparison.OrdinalIgnoreCase))
            {
                return Failed(L["Settings:SsoTestDiscoveryInvalid"]);
            }

            if (string.IsNullOrWhiteSpace(resolved.AdminUser) || string.IsNullOrWhiteSpace(resolved.AdminSecret))
            {
                return Succeeded(L["Settings:SsoTestDiscoveryOk"]);
            }

            var tokenUrl = $"{resolved.BaseUrl.TrimEnd('/')}/realms/master/protocol/openid-connect/token";
            using var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = resolved.AdminUser,
                ["password"] = resolved.AdminSecret
            });
            using var tokenResponse = await client.PostAsync(tokenUrl, tokenContent);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return Failed(L["Settings:SsoTestAdminAuthFailed"]);
            }

            using var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>();
            if (tokenPayload is null
                || !tokenPayload.RootElement.TryGetProperty("access_token", out var accessToken)
                || string.IsNullOrWhiteSpace(accessToken.GetString()))
            {
                return Failed(L["Settings:SsoTestAdminAuthFailed"]);
            }

            using var adminRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{resolved.BaseUrl.TrimEnd('/')}/admin/realms/{Uri.EscapeDataString(resolved.Realm)}");
            adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.GetString());
            using var adminResponse = await client.SendAsync(adminRequest);
            if (!adminResponse.IsSuccessStatusCode)
            {
                return Failed(string.Format(
                    L["Settings:SsoTestAdminFailed"],
                    (int)adminResponse.StatusCode));
            }

            return Succeeded(L["Settings:SsoTestAdminOk"]);
        }
        catch (Exception exception)
        {
            return Failed(exception.Message);
        }
    }

    internal static KeycloakSettingValues MergeInput(
        UpdateAuthenticationSettingsDto input,
        KeycloakSettingValues stored)
    {
        return new KeycloakSettingValues
        {
            Enabled = Bool(input.Enabled),
            ShowSsoLoginButton = Bool(input.ShowSsoLoginButton),
            BaseUrl = input.BaseUrl,
            Realm = input.Realm,
            ClientId = input.ClientId,
            ClientSecret = KeycloakSettingsResolution.ShouldReplaceSecret(input.ClientSecret)
                ? input.ClientSecret
                : stored.ClientSecret,
            RequireHttpsMetadata = Bool(input.RequireHttpsMetadata),
            AdminUser = input.AdminUser,
            AdminSecret = KeycloakSettingsResolution.ShouldReplaceSecret(input.AdminSecret)
                ? input.AdminSecret
                : stored.AdminSecret,
            AppAccessGroup = input.AppAccessGroup,
            RoleMappings = KeycloakRoleMappingSerializer.Serialize(input.RoleMappings)
        };
    }

    private async Task<KeycloakSettingValues> ReadStoredAsync()
    {
        return new KeycloakSettingValues
        {
            Enabled = await ReadAsync(HCSSettings.KeycloakEnabled),
            ShowSsoLoginButton = await ReadAsync(HCSSettings.ShowSsoLoginButton),
            BaseUrl = await ReadAsync(HCSSettings.KeycloakBaseUrl),
            Realm = await ReadAsync(HCSSettings.KeycloakRealm),
            ClientId = await ReadAsync(HCSSettings.KeycloakClientId),
            ClientSecret = await ReadAsync(HCSSettings.KeycloakClientSecret),
            RequireHttpsMetadata = await ReadAsync(HCSSettings.KeycloakRequireHttpsMetadata),
            AdminUser = await ReadAsync(HCSSettings.KeycloakAdminUser),
            AdminSecret = await ReadAsync(HCSSettings.KeycloakAdminSecret),
            AppAccessGroup = await ReadAsync(HCSSettings.KeycloakAppAccessGroup),
            RoleMappings = await ReadAsync(HCSSettings.KeycloakRoleMappings),
            Revision = await ReadAsync(HCSSettings.KeycloakRevision)
        };
    }

    private Task<string> ReadAsync(string name) =>
        settingManager.GetOrNullGlobalAsync(name, fallback: false);

    private KeycloakSettingValues FromEnvironment()
    {
        var section = configuration.GetSection("Authentication:Keycloak");
        return new KeycloakSettingValues
        {
            Enabled = section["Enabled"],
            Authority = section["Authority"],
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            RequireHttpsMetadata = section["RequireHttpsMetadata"],
            MetadataAddress = section["MetadataAddress"]
        };
    }

    private static AuthenticationConnectionTestResultDto Succeeded(string message) =>
        new() { Success = true, Message = message };

    private static AuthenticationConnectionTestResultDto Failed(string message) =>
        new() { Success = false, Message = message };

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}

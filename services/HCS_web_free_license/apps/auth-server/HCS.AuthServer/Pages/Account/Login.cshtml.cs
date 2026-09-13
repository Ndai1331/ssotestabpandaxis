using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using HCS.Branding;
using HCS.Settings;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Alerts;
using Volo.Abp.Identity;
using Volo.Abp.Settings;

namespace HCS.AuthServer.Pages.Account;

public class LoginModel : Volo.Abp.Account.Web.Pages.Account.LoginModel
{
    private readonly IConfiguration _configuration;
    private readonly ISettingProvider _settingProvider;

    public bool ShowSsoLoginButton { get; private set; } = true;
    public string BrandingTitle { get; private set; } = SystemBrandingDefaults.Title;
    public string BrandingDescription { get; private set; } = SystemBrandingDefaults.Description;
    public long BrandingRevision { get; private set; }
    public string BrandingLogoUrl => BuildBrandingAssetUrl(SystemBrandingDefaults.LogoSlot, "/images/logo/logo-hcs.svg", BrandingLogoRevision);
    public string BrandingFaviconUrl => BuildBrandingAssetUrl(SystemBrandingDefaults.FaviconSlot, "/favicon.ico", BrandingFaviconRevision);
    public string BrandingBackgroundUrl => BuildBrandingAssetUrl(SystemBrandingDefaults.BackgroundSlot, "/background-login.png", BrandingBackgroundRevision);
    private long BrandingLogoRevision { get; set; }
    private long BrandingFaviconRevision { get; set; }
    private long BrandingBackgroundRevision { get; set; }

    public IReadOnlyList<AlertMessage> VisibleAlerts => Alerts;

    public LoginModel(
        IAuthenticationSchemeProvider schemeProvider,
        IOptions<AbpAccountOptions> accountOptions,
        IOptions<IdentityOptions> identityOptions,
        IdentityDynamicClaimsPrincipalContributorCache identityDynamicClaimsPrincipalContributorCache,
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration,
        ISettingProvider settingProvider)
        : base(
            schemeProvider,
            accountOptions,
            identityOptions,
            identityDynamicClaimsPrincipalContributorCache,
            webHostEnvironment)
    {
        _configuration = configuration;
        _settingProvider = settingProvider;
    }

    public override async Task<IActionResult> OnGetAsync()
    {
        ApplyDefaultReturnUrl();
        await LoadAuthenticationSettingsAsync();
        await LoadBrandingAsync();
        return await base.OnGetAsync();
    }

    public override async Task<IActionResult> OnPostAsync(string action)
    {
        ApplyDefaultReturnUrl();
        await LoadAuthenticationSettingsAsync();
        await LoadBrandingAsync();
        if (string.IsNullOrWhiteSpace(action))
        {
            action = "Login";
            ModelState.Remove("action");
        }

        return await base.OnPostAsync(action);
    }

    private async Task LoadAuthenticationSettingsAsync()
    {
        var configuredValue = await _settingProvider.GetOrNullAsync(HCSSettings.ShowSsoLoginButton);
        ShowSsoLoginButton = !string.Equals(configuredValue, "false", StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadBrandingAsync()
    {
        BrandingTitle = await _settingProvider.GetOrNullAsync(HCSSettings.BrandingTitle)
            ?? SystemBrandingDefaults.Title;
        BrandingDescription = await _settingProvider.GetOrNullAsync(HCSSettings.BrandingDescription)
            ?? SystemBrandingDefaults.Description;
        BrandingRevision = await ReadSettingRevisionAsync(HCSSettings.BrandingRevision);
        BrandingLogoRevision = await ReadSettingRevisionAsync(HCSSettings.BrandingLogoRevision);
        BrandingFaviconRevision = await ReadSettingRevisionAsync(HCSSettings.BrandingFaviconRevision);
        BrandingBackgroundRevision = await ReadSettingRevisionAsync(HCSSettings.BrandingBackgroundRevision);
    }

    private async Task<long> ReadSettingRevisionAsync(string settingName)
    {
        var value = await _settingProvider.GetOrNullAsync(settingName);
        return long.TryParse(value, out var revision) && revision > 0 ? revision : 0;
    }

    private string BuildBrandingAssetUrl(string slot, string fallback, long assetRevision)
    {
        if (assetRevision <= 0)
        {
            return fallback;
        }

        var origin = _configuration["App:GatewayUrl"]
            ?? _configuration["App:ClientUrl"]
            ?? "https://localhost:44402";
        return $"{origin.TrimEnd('/')}/api/hcs/system-branding/assets/{slot}?v={assetRevision}";
    }

    private void ApplyDefaultReturnUrl()
    {
        if (!string.IsNullOrWhiteSpace(ReturnUrl) &&
            !string.Equals(ReturnUrl, "/", StringComparison.Ordinal))
        {
            return;
        }

        ReturnUrl = GetClientAppUrl();
    }

    private string GetClientAppUrl()
    {
        var configured = _configuration["App:ClientUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/') + "/workspace";
        }

        return "https://hcs.localhost/workspace";
    }
}

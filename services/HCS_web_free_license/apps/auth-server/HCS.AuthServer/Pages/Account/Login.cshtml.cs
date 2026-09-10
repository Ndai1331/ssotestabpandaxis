using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
        return await base.OnGetAsync();
    }

    public override async Task<IActionResult> OnPostAsync(string action)
    {
        ApplyDefaultReturnUrl();
        await LoadAuthenticationSettingsAsync();
        return await base.OnPostAsync(action);
    }

    private async Task LoadAuthenticationSettingsAsync()
    {
        var configuredValue = await _settingProvider.GetOrNullAsync(HCSSettings.ShowSsoLoginButton);
        ShowSsoLoginButton = !string.Equals(configuredValue, "false", StringComparison.OrdinalIgnoreCase);
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

using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace HCS.AuthServer.Pages.Account;

public class CultureModel : AbpPageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Culture { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UiCulture { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        var culture = Normalize(Culture) ?? "en";
        var uiCulture = Normalize(UiCulture) ?? culture;

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, uiCulture)),
            new CookieOptions
            {
                Expires = Clock.Now.AddYears(2),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        Response.Cookies.Append(
            "Abp.Localization.CultureName",
            uiCulture,
            new CookieOptions
            {
                Expires = Clock.Now.AddYears(2),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        return LocalRedirect(SafeReturnUrl(ReturnUrl));
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(value.Trim().Replace('_', '-')).Name;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/Account/Login";
        }

        if (Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return "/Account/Login";
    }
}

using System;
using HCS.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Volo.Abp;

namespace HCS.PlatformService.Filters;

/// <summary>
/// Supplies the fallback culture for ABP's initial localization bootstrap
/// request when no browser culture has been selected yet.
/// </summary>
public sealed class DefaultApplicationLocalizationCultureFilter : IAsyncResourceFilter, IOrderedFilter
{
    private readonly ILanguageRepository _languageRepository;

    public DefaultApplicationLocalizationCultureFilter(ILanguageRepository languageRepository)
    {
        _languageRepository = languageRepository;
    }

    public int Order => int.MinValue;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (request.Path.Equals("/api/abp/application-localization") &&
            string.IsNullOrWhiteSpace(request.Query["CultureName"]))
        {
            var query = request.Query.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            var cookieCulture = await ReadCultureCookieAsync(request);
            query["CultureName"] = cookieCulture ?? await GetDefaultCultureAsync();
            request.Query = new QueryCollection(query);
        }

        await next();
    }

    private async Task<string?> ReadCultureCookieAsync(HttpRequest request)
    {
        if (request.Cookies.TryGetValue("hcs.culture", out var hcs) && await IsEnabledAsync(hcs))
        {
            return Language.NormalizeCultureName(hcs);
        }

        if (request.Cookies.TryGetValue("Abp.Localization.CultureName", out var abp) && await IsEnabledAsync(abp))
        {
            return Language.NormalizeCultureName(abp);
        }

        if (request.Cookies.TryGetValue(".AspNetCore.Culture", out var aspNet))
        {
            var decoded = Uri.UnescapeDataString(aspNet);
            var marker = "uic=";
            var start = decoded.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start >= 0)
            {
                var value = decoded[(start + marker.Length)..].Split('|')[0].Trim();
                if (await IsEnabledAsync(value))
                {
                    return Language.NormalizeCultureName(value);
                }
            }
        }

        return null;
    }

    private async Task<string> GetDefaultCultureAsync()
    {
        var defaultLanguage = await _languageRepository.FindDefaultAsync();
        if (defaultLanguage is { IsEnabled: true })
        {
            return defaultLanguage.CultureName;
        }

        var english = await _languageRepository.FindByCultureNameAsync("en");
        if (english is { IsEnabled: true })
        {
            return english.CultureName;
        }

        return "en";
    }

    private async Task<bool> IsEnabledAsync(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return false;
        }

        string normalized;
        try
        {
            normalized = Language.NormalizeCultureName(culture);
        }
        catch (BusinessException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        var language = await _languageRepository.FindByCultureNameAsync(normalized);
        return language is { IsEnabled: true };
    }
}

using Microsoft.Extensions.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;
using hanhchinhso.LanguageService.Localization;

namespace hanhchinhso.Blazor;

[Dependency(ReplaceServices = true)]
public class hanhchinhsoBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<LanguageServiceResource> _localizer;

    public hanhchinhsoBrandingProvider(IStringLocalizer<LanguageServiceResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["hanhchinhso"];
}

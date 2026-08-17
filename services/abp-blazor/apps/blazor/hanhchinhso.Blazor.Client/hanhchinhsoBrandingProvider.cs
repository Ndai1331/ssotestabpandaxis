using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Localization;
using hanhchinhso.LanguageService.Localization;

namespace hanhchinhso.Blazor.Client;

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

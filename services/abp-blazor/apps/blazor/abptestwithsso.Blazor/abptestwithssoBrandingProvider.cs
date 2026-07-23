using Microsoft.Extensions.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;
using abptestwithsso.LanguageService.Localization;

namespace abptestwithsso.Blazor;

[Dependency(ReplaceServices = true)]
public class abptestwithssoBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<LanguageServiceResource> _localizer;

    public abptestwithssoBrandingProvider(IStringLocalizer<LanguageServiceResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["abptestwithsso"];
}

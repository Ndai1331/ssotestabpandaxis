using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Localization;
using abptestwithsso.LanguageService.Localization;

namespace abptestwithsso.Blazor.Client;

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

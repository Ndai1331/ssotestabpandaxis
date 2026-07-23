using Volo.Abp.AspNetCore.Components;
using abptestwithsso.LanguageService.Localization;

namespace abptestwithsso.Blazor.Client;

public abstract class abptestwithssoComponentBase : AbpComponentBase
{
    protected abptestwithssoComponentBase()
    {
        LocalizationResource = typeof(LanguageServiceResource);
    }
}

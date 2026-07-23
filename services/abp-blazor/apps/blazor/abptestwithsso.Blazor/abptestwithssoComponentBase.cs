using Volo.Abp.AspNetCore.Components;
using abptestwithsso.LanguageService.Localization;

namespace abptestwithsso.Blazor;

public abstract class abptestwithssoComponentBase : AbpComponentBase
{
    protected abptestwithssoComponentBase()
    {
        LocalizationResource = typeof(LanguageServiceResource);
    }
}

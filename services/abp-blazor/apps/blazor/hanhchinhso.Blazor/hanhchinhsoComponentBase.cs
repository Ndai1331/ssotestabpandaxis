using Volo.Abp.AspNetCore.Components;
using hanhchinhso.LanguageService.Localization;

namespace hanhchinhso.Blazor;

public abstract class hanhchinhsoComponentBase : AbpComponentBase
{
    protected hanhchinhsoComponentBase()
    {
        LocalizationResource = typeof(LanguageServiceResource);
    }
}

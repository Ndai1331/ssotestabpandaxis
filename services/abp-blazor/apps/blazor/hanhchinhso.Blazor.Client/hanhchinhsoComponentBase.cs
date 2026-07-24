using Volo.Abp.AspNetCore.Components;
using hanhchinhso.LanguageService.Localization;

namespace hanhchinhso.Blazor.Client;

public abstract class hanhchinhsoComponentBase : AbpComponentBase
{
    protected hanhchinhsoComponentBase()
    {
        LocalizationResource = typeof(LanguageServiceResource);
    }
}

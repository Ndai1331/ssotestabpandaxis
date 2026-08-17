using Localization.Resources.AbpUi;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;

namespace hanhchinhso.LanguageService.Localization;
    
[LocalizationResourceName("LanguageService")]
[InheritResource(
    typeof(AbpValidationResource),
    typeof(AbpUiResource)
)]
public class LanguageServiceResource
{
        
}

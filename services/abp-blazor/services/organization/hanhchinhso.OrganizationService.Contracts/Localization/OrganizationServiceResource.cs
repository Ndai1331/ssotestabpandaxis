using Localization.Resources.AbpUi;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;

namespace hanhchinhso.OrganizationService.Localization;
    
[LocalizationResourceName("OrganizationService")]
[InheritResource(
    typeof(AbpValidationResource),
    typeof(AbpUiResource)
)]
public class OrganizationServiceResource
{
        
}

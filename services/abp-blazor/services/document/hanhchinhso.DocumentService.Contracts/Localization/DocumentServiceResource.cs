using Localization.Resources.AbpUi;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;

namespace hanhchinhso.DocumentService.Localization;
    
[LocalizationResourceName("DocumentService")]
[InheritResource(
    typeof(AbpValidationResource),
    typeof(AbpUiResource)
)]
public class DocumentServiceResource
{
        
}

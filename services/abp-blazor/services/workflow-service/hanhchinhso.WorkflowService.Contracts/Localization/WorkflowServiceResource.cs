using Localization.Resources.AbpUi;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;

namespace hanhchinhso.WorkflowService.Localization;
    
[LocalizationResourceName("WorkflowService")]
[InheritResource(
    typeof(AbpValidationResource),
    typeof(AbpUiResource)
)]
public class WorkflowServiceResource
{
        
}

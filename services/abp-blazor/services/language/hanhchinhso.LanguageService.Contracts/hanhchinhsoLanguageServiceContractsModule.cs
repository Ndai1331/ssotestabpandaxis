using Localization.Resources.AbpUi;
using hanhchinhso.LanguageService.Localization;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Commercial.SuiteTemplates;
using Volo.Abp.Application;
using Volo.Abp.Authorization;
using Volo.Abp.Domain;
using Volo.Abp.LanguageManagement;
using Volo.Abp.LanguageManagement.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.UI;
using Volo.Abp.Validation;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace hanhchinhso.LanguageService;
    
[DependsOn(
    typeof(AbpValidationModule),
    typeof(AbpUiModule),
    typeof(VoloAbpCommercialSuiteTemplatesModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(LanguageManagementApplicationContractsModule)
)]

public class hanhchinhsoLanguageServiceContractsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<hanhchinhsoLanguageServiceContractsModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<LanguageServiceResource>("vi")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("/Localization/LanguageService");
            
            options.Languages.Add(new LanguageInfo("vi", "vi", "Vietnamese")); 
            options.Languages.Add(new LanguageInfo("en", "en", "English")); 

        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("LanguageManagementService", typeof(LanguageManagementResource));
        });
    }
}

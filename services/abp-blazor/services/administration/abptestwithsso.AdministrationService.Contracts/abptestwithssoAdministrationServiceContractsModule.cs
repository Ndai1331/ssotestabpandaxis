using Localization.Resources.AbpUi;
using abptestwithsso.AdministrationService.Localization;
using Volo.Abp.Commercial.SuiteTemplates;
using Volo.Abp.Application;
using Volo.Abp.Authorization;
using Volo.Abp.Domain;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.UI;
using Volo.Abp.Validation;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace abptestwithsso.AdministrationService;

[DependsOn(
    typeof(AbpValidationModule),
    typeof(AbpUiModule),
    typeof(AbpAuthorizationAbstractionsModule),
    typeof(VoloAbpCommercialSuiteTemplatesModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpDddDomainSharedModule)
    )]
public class abptestwithssoAdministrationServiceContractsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<abptestwithssoAdministrationServiceContractsModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<AdministrationServiceResource>("vi")
                .AddBaseTypes(typeof(AbpValidationResource), typeof(AbpUiResource))
                .AddVirtualJson("/Localization/AdministrationService");
        });



        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("AdministrationService", typeof(AdministrationServiceResource));
        });
    }
}

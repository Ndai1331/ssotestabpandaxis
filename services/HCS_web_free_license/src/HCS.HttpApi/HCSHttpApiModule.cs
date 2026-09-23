using Localization.Resources.AbpUi;
using HCS.Filters;
using HCS.Localization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Account;
using Volo.Abp.SettingManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.Localization;

namespace HCS;

 [DependsOn(
    typeof(HCSApplicationContractsModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule)
    )]
public class HCSHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<MvcOptions>(options => options.Filters.Add<IdentityUserListNotActiveFilter>());
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<HCSResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}

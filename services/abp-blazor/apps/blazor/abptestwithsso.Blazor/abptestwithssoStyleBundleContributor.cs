using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace abptestwithsso.Blazor;

public class abptestwithssoStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add(new BundleFile("main.css", true));
    }
}
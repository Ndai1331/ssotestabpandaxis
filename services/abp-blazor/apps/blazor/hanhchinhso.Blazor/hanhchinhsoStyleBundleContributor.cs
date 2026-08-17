using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace hanhchinhso.Blazor;

public class hanhchinhsoStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add(new BundleFile("main.css", true));
    }
}
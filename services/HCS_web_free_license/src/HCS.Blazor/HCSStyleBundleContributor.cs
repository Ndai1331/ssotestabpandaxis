using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace HCS.Blazor;

public class HCSStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        // Catalog / workspace / kanban rules are concatenated into main.css so the
        // host bundle always ships them (separate wwwroot copies conflict with WASM assets).
        context.Files.Add(new BundleFile("main.css", true));
    }
}

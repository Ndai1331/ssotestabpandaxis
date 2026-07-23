using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace abptestwithsso.AuthServer;

[Dependency(ReplaceServices = true)]
public class BrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "abptestwithsso Authentication Server";
}
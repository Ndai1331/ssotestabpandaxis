namespace HCS.DocumentService.Tests;

public sealed class LicenseBoundaryTests
{
    [Fact]
    public void Runtime_references_no_commercial_or_unapproved_signing_assemblies()
    {
        var forbidden = new[]
        {
            string.Concat(".", "Pro"),
            string.Concat("Volo.Abp.", "Commercial"),
            string.Concat("Bnn.", "SignLib"),
            string.Concat("Bnn.", "Sdk")
        };
        var references = typeof(HcsDocumentServiceModule).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToList();
        Assert.DoesNotContain(references, name => forbidden.Any(name.Contains));
    }
}

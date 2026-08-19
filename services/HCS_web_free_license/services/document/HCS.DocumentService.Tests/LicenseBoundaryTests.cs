namespace HCS.DocumentService.Tests;

public sealed class LicenseBoundaryTests
{
    [Fact]
    public void Runtime_references_no_commercial_or_unapproved_signing_assemblies()
    {
        var references = typeof(HcsDocumentServiceModule).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToList();
        Assert.DoesNotContain(references, name =>
            name.Contains(string.Concat("Volo.Abp.", "Commercial"), StringComparison.Ordinal)
            || name.Contains(string.Concat("Bnn.", "SignLib"), StringComparison.Ordinal)
            || name.Contains(string.Concat("Bnn.", "Sdk"), StringComparison.Ordinal)
            || name.Split('.').Contains("Pro", StringComparer.Ordinal));
    }
}

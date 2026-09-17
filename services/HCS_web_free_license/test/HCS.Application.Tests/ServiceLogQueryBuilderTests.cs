using System;
using HCS.Logging;
using Xunit;

namespace HCS;

public sealed class ServiceLogQueryBuilderTests
{
    [Fact]
    public void All_services_omits_application_clause()
    {
        var filter = ServiceLogQueryBuilder.Build(HcsServiceLogCatalog.Applications.All, ["Error"], null);

        Assert.Equal("@Level = 'Error'", filter);
        Assert.DoesNotContain("Application", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_applications_means_all_services()
    {
        var filter = ServiceLogQueryBuilder.Build([], ["Warning"], null);

        Assert.Equal("@Level = 'Warning'", filter);
    }

    [Fact]
    public void Subset_uses_in_clause()
    {
        var filter = ServiceLogQueryBuilder.Build(
            [HcsServiceLogCatalog.Applications.PlatformService, HcsServiceLogCatalog.Applications.DocumentService],
            ["Error", "Warning"],
            null);

        Assert.Contains("Application in ['HCS.PlatformService', 'HCS.DocumentService']", filter, StringComparison.Ordinal);
        Assert.Contains("@Level in ['Error', 'Warning']", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_application_is_rejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ServiceLogQueryBuilder.Build(["not-a-host"], ["Error"], null));

        Assert.Equal("applications", exception.ParamName);
    }

    [Fact]
    public void Unknown_level_is_rejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ServiceLogQueryBuilder.Build(null, ["Debug"], null));

        Assert.Equal("levels", exception.ParamName);
    }

    [Fact]
    public void Empty_levels_default_to_error_and_warning()
    {
        var filter = ServiceLogQueryBuilder.Build(null, [], null);

        Assert.Contains("@Level in ['Error', 'Warning']", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void Keyword_is_escaped_and_searches_message_or_exception()
    {
        var filter = ServiceLogQueryBuilder.Build(
            [HcsServiceLogCatalog.Applications.Blazor],
            ["Error"],
            "it's a 100% fail");

        Assert.Contains("Application = 'HCS.Blazor'", filter, StringComparison.Ordinal);
        Assert.Contains("Contains(@Message, 'it''s a 100% fail')", filter, StringComparison.Ordinal);
        Assert.Contains("Contains(@Exception, 'it''s a 100% fail')", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void AfterId_rejects_injection()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ServiceLogQueryBuilder.NormalizeAfterId("event-1' or true"));

        Assert.Equal("afterId", exception.ParamName);
    }

    [Fact]
    public void Count_is_clamped()
    {
        Assert.Equal(100, ServiceLogQueryBuilder.NormalizeCount(0));
        Assert.Equal(200, ServiceLogQueryBuilder.NormalizeCount(500));
        Assert.Equal(20, ServiceLogQueryBuilder.NormalizeCount(20));
    }
}

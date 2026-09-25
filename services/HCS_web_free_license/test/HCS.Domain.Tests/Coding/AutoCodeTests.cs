using System.Threading.Tasks;
using HCS.Coding;
using Shouldly;
using Xunit;

namespace HCS.CodingTests;

public class AutoCodeTests
{
    [Fact]
    public void NormalizePrefix_keeps_letters_digits_and_separators()
    {
        AutoCode.NormalizePrefix(" pj_01 ", AutoCodeKind.Project).ShouldBe("pj_01");
        AutoCode.NormalizePrefix("T*ask!", AutoCodeKind.Task).ShouldBe("Task");
        AutoCode.NormalizePrefix("   ", AutoCodeKind.Document).ShouldBe(AutoCode.Defaults.Document);
        AutoCode.NormalizePrefix("TOOLONGPREFIX12X", AutoCodeKind.Catalog).ShouldBe("TOOLONGPREFI");
    }

    [Fact]
    public void Next_uses_the_highest_matching_sequence()
    {
        AutoCode.Next("PJ", new[] { "PJ0001", "OTHER2", "pj0009", "PJ10" }).ShouldBe("PJ0011");
        AutoCode.Next("T", []).ShouldBe("T0001");
        AutoCode.DefaultPrefix(AutoCodeKind.PersonalDocument).ShouldBe("VBCN");
        AutoCode.DefaultPrefix(AutoCodeKind.PersonalArchive).ShouldBe("LTCN");
        AutoCode.Next("VB", ["VB0001", "VBCN0008"]).ShouldBe("VB0002");
        AutoCode.Next("LT", ["LT0001", "LTCN0008"]).ShouldBe("LT0002");
    }

    [Fact]
    public async Task AllocateAsync_keeps_an_explicit_code_and_fills_blank_ones()
    {
        var settings = new FixedAutoCodeSettings("VB");

        (await AutoCode.AllocateAsync(settings, AutoCodeKind.Document, " VB-9 ", ["VB0001"])).ShouldBe("VB-9");
        (await AutoCode.AllocateAsync(settings, AutoCodeKind.Document, "  ", ["VB0002", "VB0004"])).ShouldBe("VB0005");
    }

    private sealed class FixedAutoCodeSettings(string prefix) : IAutoCodeSettings
    {
        public Task<string> GetPrefixAsync(AutoCodeKind kind, System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult(prefix);
    }
}

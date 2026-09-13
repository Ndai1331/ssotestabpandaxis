using HC.RemoteSigns;

namespace HCS.DocumentService.Tests;

public sealed class SignTextV2Tests
{
    [Theory]
    [InlineData("10.20.30.40", "http://10.20.30.40")]
    [InlineData("10.20.30.40:8443/", "http://10.20.30.40:8443")]
    [InlineData("https://tag.internal:9443", "https://tag.internal:9443")]
    public void Normalizes_tag_endpoint_without_inventing_a_path(string raw, string expected)
    {
        Assert.Equal(expected, SignTextV2.NormalizeRemoteSignBaseUri(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://")]
    public void Rejects_an_invalid_tag_endpoint(string raw)
    {
        Assert.Throws<ArgumentException>(() => SignTextV2.NormalizeRemoteSignBaseUri(raw));
    }
}

using HCS.Logging;
using Xunit;

namespace HCS;

public sealed class SeqServiceLogQueryTests
{
    [Fact]
    public void ParseEvents_maps_seq_payload()
    {
        const string json = """
        {
          "Events": [
            {
              "Id": "event-abc",
              "Timestamp": "2026-09-17T09:00:00Z",
              "Level": "Error",
              "RenderedMessage": "Payment failed",
              "Exception": "TimeoutException: boom",
              "Properties": [
                { "Name": "Application", "Value": "HCS.PlatformService" },
                { "Name": "SourceContext", "Value": "HCS.Payments" },
                { "Name": "CorrelationId", "Value": "corr-1" }
              ]
            }
          ]
        }
        """;

        var items = SeqServiceLogQuery.ParseEvents(json);
        var item = Assert.Single(items);
        Assert.Equal("event-abc", item.Id);
        Assert.Equal("Error", item.Level);
        Assert.Equal("HCS.PlatformService", item.Application);
        Assert.Equal("HCS.Payments", item.SourceContext);
        Assert.Equal("Payment failed", item.Message);
        Assert.Equal("TimeoutException: boom", item.Exception);
        Assert.Equal("corr-1", item.CorrelationId);
        Assert.Equal("HCS.PlatformService", item.Properties["Application"]);
    }

    [Fact]
    public void ParseEvents_returns_empty_when_payload_has_no_events()
    {
        Assert.Empty(SeqServiceLogQuery.ParseEvents("{}"));
        Assert.Empty(SeqServiceLogQuery.ParseEvents(""));
    }
}

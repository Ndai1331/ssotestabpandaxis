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
    public void ParseEvents_maps_camel_case_and_clef_payloads()
    {
        const string camel = """
        {
          "events": [
            {
              "id": "event-camel",
              "timestamp": "2026-09-18T06:30:00Z",
              "level": "Warning",
              "renderedMessage": "Slow query",
              "properties": { "Application": "HCS.WebGateway", "SourceContext": "Yarp" }
            }
          ]
        }
        """;
        var camelItem = Assert.Single(SeqServiceLogQuery.ParseEvents(camel));
        Assert.Equal("event-camel", camelItem.Id);
        Assert.Equal("Warning", camelItem.Level);
        Assert.Equal("HCS.WebGateway", camelItem.Application);
        Assert.Equal("Slow query", camelItem.Message);

        const string clef = """
        {"@t":"2026-09-18T06:31:00Z","@i":"event-clef","@l":"Error","@mt":"Payment failed","Application":"HCS.PlatformService","SourceContext":"HCS.Payments"}
        {"@t":"2026-09-18T06:31:01Z","@i":"event-clef-2","@mt":"Started","Application":"HCS.Blazor"}
        """;
        var clefItems = SeqServiceLogQuery.ParseEvents(clef);
        Assert.Equal(2, clefItems.Count);
        Assert.Equal("Error", clefItems[0].Level);
        Assert.Equal("HCS.PlatformService", clefItems[0].Application);
        Assert.Equal("Payment failed", clefItems[0].Message);
        Assert.Equal("Information", clefItems[1].Level);
    }

    [Fact]
    public void ParseEvents_maps_root_array_payload()
    {
        const string json = """
        [
          {
            "Id": "event-array",
            "Timestamp": "2026-09-18T06:32:00Z",
            "Level": "Information",
            "RenderedMessage": "Ready",
            "Properties": [
              { "Name": "Application", "Value": "HCS.AuthServer" }
            ]
          }
        ]
        """;
        var item = Assert.Single(SeqServiceLogQuery.ParseEvents(json));
        Assert.Equal("event-array", item.Id);
        Assert.Equal("HCS.AuthServer", item.Application);
    }

    [Fact]
    public void ParseEvents_returns_empty_when_payload_has_no_events()
    {
        Assert.Empty(SeqServiceLogQuery.ParseEvents("{}"));
        Assert.Empty(SeqServiceLogQuery.ParseEvents(""));
    }
}

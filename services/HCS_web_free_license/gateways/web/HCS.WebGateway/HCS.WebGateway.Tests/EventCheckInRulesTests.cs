using HCS.Blazor.Client.Work;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class EventCheckInRulesTests
{
    [Fact]
    public void Check_in_stays_a_manual_choice_while_the_event_is_ongoing()
    {
        Assert.True(EventCheckInRules.CanCheckIn(Event("Ongoing", checkInStatus: "NotCheckedIn")));
        Assert.False(EventCheckInRules.CanCheckIn(Event("Ongoing", checkInStatus: "CheckedIn")));
        Assert.False(EventCheckInRules.CanCheckIn(Event("Preparing", checkInStatus: "NotCheckedIn")));
        Assert.False(EventCheckInRules.CanCheckIn(null));
    }

    [Fact]
    public void Confirm_and_decline_are_available_only_before_the_event_starts()
    {
        var preparing = Event("Preparing", registrationStatus: "Pending");
        Assert.True(EventCheckInRules.CanConfirm(preparing));
        Assert.True(EventCheckInRules.CanDecline(preparing));

        Assert.False(EventCheckInRules.CanConfirm(Event("Preparing", registrationStatus: "Confirmed")));
        Assert.False(EventCheckInRules.CanDecline(Event("Preparing", registrationStatus: "Declined")));
        Assert.False(EventCheckInRules.CanConfirm(Event("Ongoing", registrationStatus: "Pending")));
    }

    [Fact]
    public void Home_cta_path_stays_on_the_authenticated_workspace()
    {
        Assert.Equal("/workspace", EventCheckInRules.HomePath);
    }

    [Fact]
    public void Server_check_in_uses_the_internal_gateway_when_both_origins_are_configured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bff:PublicOrigin"] = "https://hcs.localhost",
                ["RemoteServices:Default:BaseUrl"] = "http://web-gateway:8080/"
            })
            .Build();

        Assert.Equal("http://web-gateway:8080/", EventCheckInOrigins.ResolveBackchannel(configuration).AbsoluteUri);
        Assert.Equal("https://hcs.localhost/", EventCheckInOrigins.ResolveBrowser(configuration).AbsoluteUri);
    }

    [Theory]
    [InlineData("Ongoing", "hcs-status-badge hcs-status-badge--success")]
    [InlineData("Completed", "hcs-status-badge hcs-status-badge--todo")]
    [InlineData("Cancelled", "hcs-status-badge hcs-status-badge--danger")]
    [InlineData("Preparing", "hcs-status-badge hcs-status-badge--planning")]
    public void Status_badge_classes_match_the_event_lifecycle(string status, string expected) =>
        Assert.Equal(expected, EventCheckInRules.StatusClass(status));

    private static PublicEventDto Event(
        string status,
        string? registrationStatus = null,
        string? checkInStatus = null) =>
        new("EVT-1", "Town hall", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Hall",
            Status: status, RegistrationStatus: registrationStatus, CheckInStatus: checkInStatus);
}

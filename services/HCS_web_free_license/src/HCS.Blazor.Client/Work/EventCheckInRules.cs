using System;
using Microsoft.Extensions.Configuration;

namespace HCS.Blazor.Client.Work;

public static class EventCheckInOrigins
{
    public static Uri ResolveBackchannel(IConfiguration configuration)
    {
        var configuredOrigin = configuration["RemoteServices:Default:BaseUrl"] ?? configuration["Bff:PublicOrigin"];
        if (!Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var origin) ||
            (origin.Scheme != Uri.UriSchemeHttps && origin.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "RemoteServices:Default:BaseUrl or Bff:PublicOrigin must be an absolute HTTP(S) gateway origin.");
        }

        return Root(origin);
    }

    public static Uri ResolveBrowser(IConfiguration configuration)
    {
        var configuredOrigin = configuration["Bff:PublicOrigin"] ?? configuration["RemoteServices:Default:BaseUrl"];
        if (!Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var origin) ||
            origin.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Bff:PublicOrigin or RemoteServices:Default:BaseUrl must be an absolute HTTPS origin for browser links.");
        }

        return Root(origin);
    }

    private static Uri Root(Uri origin) =>
        new(origin.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
}

public static class EventCheckInRules
{
    public const string HomePath = "/workspace";

    public static bool IsPreparing(string? status) =>
        EqualsOrdinal(status, "Preparing");

    public static bool IsOngoing(string? status) =>
        EqualsOrdinal(status, "Ongoing");

    public static bool IsClosed(string? status) =>
        EqualsOrdinal(status, "Completed") || EqualsOrdinal(status, "Cancelled");

    public static bool IsConfirmed(string? status) =>
        EqualsOrdinal(status, "Confirmed");

    public static bool IsDeclined(string? status) =>
        EqualsOrdinal(status, "Declined");

    public static bool IsCheckedIn(string? status) =>
        EqualsOrdinal(status, "CheckedIn");

    public static bool CanConfirm(PublicEventDto? eventInfo) =>
        eventInfo is not null && IsPreparing(eventInfo.Status) && !IsConfirmed(eventInfo.RegistrationStatus);

    public static bool CanDecline(PublicEventDto? eventInfo) =>
        eventInfo is not null && IsPreparing(eventInfo.Status) && !IsDeclined(eventInfo.RegistrationStatus);

    public static bool CanCheckIn(PublicEventDto? eventInfo) =>
        eventInfo is not null && IsOngoing(eventInfo.Status) && !IsCheckedIn(eventInfo.CheckInStatus);

    public static string StatusClass(string? status) => status switch
    {
        "Ongoing" => "hcs-status-badge hcs-status-badge--success",
        "Completed" => "hcs-status-badge hcs-status-badge--todo",
        "Cancelled" => "hcs-status-badge hcs-status-badge--danger",
        _ => "hcs-status-badge hcs-status-badge--planning"
    };

    public static string NoticeClass(PublicEventDto? eventInfo)
    {
        if (eventInfo is null) return "";
        if (IsCheckedIn(eventInfo.CheckInStatus)) return "is-done";
        if (IsClosed(eventInfo.Status)) return "is-warn";
        if (IsDeclined(eventInfo.RegistrationStatus) && !IsOngoing(eventInfo.Status)) return "is-declined";
        if (IsConfirmed(eventInfo.RegistrationStatus) && !IsOngoing(eventInfo.Status)) return "is-wait";
        return "";
    }

    private static bool EqualsOrdinal(string? value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}

using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace HCS.Blazor.EventCheckIn;

public sealed record EventGuestProfile(string Code, string FullName, string PhoneNumber, string Email, string? Note);

public sealed class EventGuestSession(IDataProtectionProvider protectionProvider)
{
    public const string CookieName = ".HCS.EventGuest";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);
    private readonly IDataProtector protector = protectionProvider.CreateProtector("HCS.EventCheckIn.Guest.v1");

    public void Save(HttpResponse response, EventGuestProfile profile)
    {
        var payload = protector.Protect(JsonSerializer.Serialize(profile));
        response.Cookies.Append(CookieName, payload, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = Lifetime,
            Path = "/"
        });
    }

    public EventGuestProfile? Read(HttpRequest request, string eventCode)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var profile = JsonSerializer.Deserialize<EventGuestProfile>(protector.Unprotect(value));
            if (profile is null ||
                !string.Equals(profile.Code, eventCode, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(profile.FullName) ||
                string.IsNullOrWhiteSpace(profile.PhoneNumber) ||
                string.IsNullOrWhiteSpace(profile.Email))
            {
                return null;
            }

            return profile;
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal static class EventCheckInPaths
{
    public static string Pre(string code, string? token) => Combine($"/event-check-in/{code}", token);

    public static string Guest(string code, string? token) => Combine($"/event-check-in/{code}/guest", token);

    public static string Attend(string code, string? token) => Combine($"/event-check-in/{code}/attend", token);

    private static string Combine(string path, string? token) =>
        string.IsNullOrWhiteSpace(token) ? path : $"{path}?token={Uri.EscapeDataString(token)}";
}

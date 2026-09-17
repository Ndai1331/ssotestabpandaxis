using System;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Blazor.Client.Work;
using HCS.Blazor.EventCheckIn;
using HCS.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HCS.Blazor.Pages;

[AllowAnonymous]
public sealed class EventCheckInAttendModel(
    EventCheckInGatewayClient gateway,
    EventGuestSession guestSession,
    IStringLocalizer<HCSResource> localizer) : EventCheckInPageModel(gateway, localizer)
{
    public bool IsGuest { get; private set; }
    public bool CanConfirm => EventCheckInRules.CanConfirm(Event);
    public bool CanDecline => EventCheckInRules.CanDecline(Event);
    public bool CanCheckIn => EventCheckInRules.CanCheckIn(Event);
    public bool IsCheckedIn => EventCheckInRules.IsCheckedIn(Event?.CheckInStatus);
    public bool ShowHomeCta => IsCheckedIn && !IsGuest;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var guest = ResolveGuest();
        if (User.Identity?.IsAuthenticated != true && guest is null)
        {
            return Redirect(EventCheckInPaths.Pre(Code, Token));
        }

        IsGuest = guest is not null;
        await LoadEventAsync(guest, cancellationToken);
        return Page();
    }

    public Task<IActionResult> OnPostConfirmAsync(CancellationToken cancellationToken) =>
        SubmitAsync(guest => Gateway.ConfirmAsync(Code, Token!, guest, cancellationToken), cancellationToken);

    public Task<IActionResult> OnPostDeclineAsync(CancellationToken cancellationToken) =>
        SubmitAsync(guest => Gateway.DeclineAsync(Code, Token!, guest, cancellationToken), cancellationToken);

    public Task<IActionResult> OnPostCheckInAsync(CancellationToken cancellationToken) =>
        SubmitAsync(guest => Gateway.CheckInAsync(Code, Token!, guest, cancellationToken), cancellationToken);

    public string NoticeText()
    {
        if (Event is null) return "";
        if (IsCheckedIn)
        {
            var time = Event.CheckedInAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            return string.IsNullOrWhiteSpace(time)
                ? T("Event:CheckInSuccess")
                : $"{T("Event:CheckInSuccess")} · {time}";
        }

        if (EventCheckInRules.IsClosed(Event.Status)) return T("Event:EventClosedHint");
        if (EventCheckInRules.IsOngoing(Event.Status)) return T("Event:CheckInReadyHint");
        if (EventCheckInRules.IsDeclined(Event.RegistrationStatus)) return T("Event:DeclineHint");
        if (EventCheckInRules.IsConfirmed(Event.RegistrationStatus)) return T("Event:CheckInWaitHint");
        return T("Event:ConfirmHint");
    }

    private async Task<IActionResult> SubmitAsync(Func<EventGuestProfile?, Task> action,
        CancellationToken cancellationToken)
    {
        var guest = ResolveGuest();
        if (User.Identity?.IsAuthenticated != true && guest is null)
        {
            return Redirect(EventCheckInPaths.Pre(Code, Token));
        }

        IsGuest = guest is not null;
        if (!EnsureToken())
        {
            return Page();
        }

        try
        {
            await action(guest);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = MapError(ex, BffErrorKind.Save);
        }

        await LoadEventAsync(guest, cancellationToken);
        return Page();
    }

    private EventGuestProfile? ResolveGuest() =>
        User.Identity?.IsAuthenticated == true ? null : guestSession.Read(Request, Code);
}

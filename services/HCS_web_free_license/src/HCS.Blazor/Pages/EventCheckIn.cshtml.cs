using System;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Blazor.Client.Work;
using HCS.Blazor.EventCheckIn;
using HCS.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace HCS.Blazor.Pages;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventCheckInModel(
    EventCheckInGatewayClient gateway,
    IStringLocalizer<HCSResource> localizer) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Code { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public PublicEventDto? Event { get; private set; }
    public string? ErrorMessage { get; private set; }

    public bool CanConfirm => EventCheckInRules.CanConfirm(Event);
    public bool CanDecline => EventCheckInRules.CanDecline(Event);
    public bool CanCheckIn => EventCheckInRules.CanCheckIn(Event);
    public bool IsCheckedIn => EventCheckInRules.IsCheckedIn(Event?.CheckInStatus);
    public bool ShowHomeCta => IsCheckedIn;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadEventAsync(cancellationToken);
        return Page();
    }

    public Task<IActionResult> OnPostConfirmAsync(CancellationToken cancellationToken) =>
        SubmitAsync(() => gateway.ConfirmAsync(Code, Token!, cancellationToken), cancellationToken);

    public Task<IActionResult> OnPostDeclineAsync(CancellationToken cancellationToken) =>
        SubmitAsync(() => gateway.DeclineAsync(Code, Token!, cancellationToken), cancellationToken);

    public Task<IActionResult> OnPostCheckInAsync(CancellationToken cancellationToken) =>
        SubmitAsync(() => gateway.CheckInAsync(Code, Token!, cancellationToken), cancellationToken);

    public string StatusLabel(string value) => value switch
    {
        "Preparing" => localizer["Event:Status.Preparing"].Value,
        "Ongoing" => localizer["Event:Status.Ongoing"].Value,
        "Completed" => localizer["Event:Status.Completed"].Value,
        "Cancelled" => localizer["Event:Status.Cancelled"].Value,
        _ => value
    };

    public string NoticeText()
    {
        if (Event is null) return "";
        if (IsCheckedIn)
        {
            var time = Event.CheckedInAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            return string.IsNullOrWhiteSpace(time)
                ? localizer["Event:CheckInSuccess"].Value
                : $"{localizer["Event:CheckInSuccess"]} · {time}";
        }

        if (EventCheckInRules.IsClosed(Event.Status)) return localizer["Event:EventClosedHint"].Value;
        if (EventCheckInRules.IsOngoing(Event.Status)) return localizer["Event:CheckInReadyHint"].Value;
        if (EventCheckInRules.IsDeclined(Event.RegistrationStatus)) return localizer["Event:DeclineHint"].Value;
        if (EventCheckInRules.IsConfirmed(Event.RegistrationStatus)) return localizer["Event:CheckInWaitHint"].Value;
        return localizer["Event:ConfirmHint"].Value;
    }

    public string PublicAttachmentUrl(Guid fileId) => gateway.BuildPublicAttachmentUrl(Code, Token ?? "", fileId);

    public string T(string key) => localizer[key].Value;

    public string FormatTime(DateTime value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    private async Task<IActionResult> SubmitAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await TrySubmitAsync(action, cancellationToken);
        return Page();
    }

    private async Task<bool> TrySubmitAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        if (!EnsureToken())
        {
            return false;
        }

        try
        {
            await action();
            ErrorMessage = null;
            await LoadEventAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = MapError(ex, BffErrorKind.Save);
            if (Event is null)
            {
                await LoadEventAsync(cancellationToken);
            }

            return false;
        }
    }

    private async Task LoadEventAsync(CancellationToken cancellationToken)
    {
        if (!EnsureToken())
        {
            return;
        }

        try
        {
            Event = await gateway.GetAsync(Code, Token!, cancellationToken);
        }
        catch (Exception ex)
        {
            Event = null;
            ErrorMessage ??= MapError(ex);
        }
    }

    private bool EnsureToken()
    {
        if (!string.IsNullOrWhiteSpace(Token))
        {
            return true;
        }

        Event = null;
        ErrorMessage = localizer["Event:InvalidQr"].Value;
        return false;
    }

    private string MapError(Exception exception, BffErrorKind kind = BffErrorKind.Load) =>
        BffErrorMapper.From(localizer, exception, kind);
}

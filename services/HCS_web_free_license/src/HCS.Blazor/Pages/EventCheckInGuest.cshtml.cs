using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.EventCheckIn;
using HCS.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HCS.Blazor.Pages;

[AllowAnonymous]
public sealed class EventCheckInGuestModel(
    EventCheckInGatewayClient gateway,
    EventGuestSession guestSession,
    IStringLocalizer<HCSResource> localizer) : EventCheckInPageModel(gateway, localizer)
{
    [BindProperty]
    public string FullName { get; set; } = "";

    [BindProperty]
    public string PhoneNumber { get; set; } = "";

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string? Note { get; set; }

    public string PreUrl => EventCheckInPaths.Pre(Code, Token);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (guestSession.Read(Request, Code) is not null)
        {
            return Redirect(EventCheckInPaths.Attend(Code, Token));
        }

        await LoadEventAsync(guest: null, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadEventAsync(guest: null, cancellationToken);
        if (Event is null)
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(PhoneNumber) ||
            string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = T("Work:EventAttendeeRequiredFields");
            return Page();
        }

        guestSession.Save(Response, new EventGuestProfile(
            Code,
            FullName.Trim(),
            PhoneNumber.Trim(),
            Email.Trim(),
            string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()));
        return Redirect(EventCheckInPaths.Attend(Code, Token));
    }
}

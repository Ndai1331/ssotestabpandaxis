using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Authentication;
using HCS.Blazor.Client.Navigation;
using HCS.Blazor.EventCheckIn;
using HCS.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace HCS.Blazor.Pages;

[AllowAnonymous]
public sealed class EventCheckInModel(
    EventCheckInGatewayClient gateway,
    EventGuestSession guestSession,
    IConfiguration configuration,
    IStringLocalizer<HCSResource> localizer) : EventCheckInPageModel(gateway, localizer)
{
    public string StaffUrl { get; private set; } = "";
    public string GuestUrl { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadEventAsync(guest: null, cancellationToken);
        if (Event is null)
        {
            return Page();
        }

        var attend = EventCheckInPaths.Attend(Code, Token);
        StaffUrl = User.Identity?.IsAuthenticated == true
            ? attend
            : BffLoginUrlBuilder.Build(configuration,
                BffUnauthenticatedDocumentRedirect.BuildReturnUrl(
                    configuration,
                    $"/event-check-in/{Code}/attend",
                    string.IsNullOrWhiteSpace(Token) ? null : "?token=" + Token));
        GuestUrl = guestSession.Read(Request, Code) is null
            ? EventCheckInPaths.Guest(Code, Token)
            : attend;
        return Page();
    }
}

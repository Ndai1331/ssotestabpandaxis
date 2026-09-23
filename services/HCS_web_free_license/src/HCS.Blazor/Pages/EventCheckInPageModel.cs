using System;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Services;
using HCS.Blazor.Client.Work;
using HCS.Blazor.EventCheckIn;
using HCS.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace HCS.Blazor.Pages;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class EventCheckInPageModel : PageModel
{
    private readonly EventCheckInGatewayClient gateway;
    private readonly IStringLocalizer<HCSResource> localizer;

    protected EventCheckInPageModel(EventCheckInGatewayClient gateway, IStringLocalizer<HCSResource> localizer)
    {
        this.gateway = gateway;
        this.localizer = localizer;
    }

    [BindProperty(SupportsGet = true)]
    public string Code { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public PublicEventDto? Event { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    protected EventCheckInGatewayClient Gateway => gateway;

    public string T(string key) => localizer[key].Value;

    public string FormatTime(DateTime value) => WorkUi.FormatDisplayTime(value);

    public string StatusLabel(string value) => value switch
    {
        "Preparing" => localizer["Event:Status.Preparing"].Value,
        "Ongoing" => localizer["Event:Status.Ongoing"].Value,
        "Completed" => localizer["Event:Status.Completed"].Value,
        "Cancelled" => localizer["Event:Status.Cancelled"].Value,
        _ => value
    };

    public string PublicAttachmentUrl(Guid fileId) => gateway.BuildPublicAttachmentUrl(Code, Token ?? "", fileId);

    protected bool EnsureToken()
    {
        if (!string.IsNullOrWhiteSpace(Token))
        {
            return true;
        }

        Event = null;
        ErrorMessage = localizer["Event:InvalidQr"].Value;
        return false;
    }

    protected async Task LoadEventAsync(EventGuestProfile? guest, CancellationToken cancellationToken)
    {
        if (!EnsureToken())
        {
            return;
        }

        try
        {
            Event = await gateway.GetAsync(Code, Token!, guest, cancellationToken);
            ViewData["Title"] = Event.Name;
        }
        catch (Exception ex)
        {
            Event = null;
            ErrorMessage ??= MapError(ex);
        }
    }

    protected string MapError(Exception exception, BffErrorKind kind = BffErrorKind.Load) =>
        BffErrorMapper.From(localizer, exception, kind);
}

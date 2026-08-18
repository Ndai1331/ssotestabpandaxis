using System;
using System.Net;
using HCS.Blazor.Client.Collaboration;
using Microsoft.Extensions.Localization;

namespace HCS.Blazor.Client.Services;

public enum BffErrorKind
{
    Load,
    Save,
    Delete
}

public static class BffErrorMapper
{
    public static string From(IStringLocalizer localizer, Exception exception, BffErrorKind kind = BffErrorKind.Load)
    {
        var status = exception switch
        {
            BffApiException bff => bff.StatusCode,
            CollaborationApiException collaboration => collaboration.StatusCode,
            _ => (HttpStatusCode?)null
        };
        return status is null ? Fallback(localizer, kind) : From(localizer, status.Value, kind);
    }

    public static string From(IStringLocalizer localizer, HttpStatusCode status, BffErrorKind kind = BffErrorKind.Load) =>
        status switch
        {
            HttpStatusCode.Unauthorized => localizer["Catalog:Unauthorized"].Value,
            HttpStatusCode.Forbidden => localizer["Catalog:ForbiddenDescription"].Value,
            HttpStatusCode.BadRequest => localizer["Catalog:ValidationError"].Value,
            HttpStatusCode.NotFound => localizer["Catalog:NotFound"].Value,
            HttpStatusCode.Conflict => localizer["Catalog:Conflict"].Value,
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
                => localizer["Catalog:ServiceUnavailable"].Value,
            _ => Fallback(localizer, kind)
        };

    private static string Fallback(IStringLocalizer localizer, BffErrorKind kind) => kind switch
    {
        BffErrorKind.Save => localizer["Catalog:SaveError"].Value,
        BffErrorKind.Delete => localizer["Catalog:DeleteError"].Value,
        _ => localizer["Catalog:LoadError"].Value
    };
}

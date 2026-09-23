using System;
using System.Threading.Tasks;
using HCS.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HCS.Filters;

public sealed class IdentityUserListNotActiveFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var isList = IsIdentityUserListGet(request);
        if (isList && bool.TryParse(request.Query["notActive"], out var notActive))
        {
            HcsIdentityUserListQuery.NotActive = notActive;
        }

        try
        {
            await next();
        }
        finally
        {
            if (isList)
            {
                HcsIdentityUserListQuery.NotActive = null;
            }
        }
    }

    private static bool IsIdentityUserListGet(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? string.Empty;
        return path.Equals("/api/identity/users", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/api/identity/users/", StringComparison.OrdinalIgnoreCase);
    }
}

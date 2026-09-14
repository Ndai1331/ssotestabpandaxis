using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HCS.WebGateway;

internal sealed class BffProxyAuthorizationRequirement : IAuthorizationRequirement;

internal sealed class BffProxyAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<BffProxyAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BffProxyAuthorizationRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext ?? context.Resource as HttpContext;
        if (httpContext is not null &&
            (BffRequestPolicy.IsAnonymousBootstrapPath(httpContext.Request.Path) ||
             BffRequestPolicy.IsAnonymousSurveyPath(httpContext.Request.Path) ||
             BffRequestPolicy.IsAnonymousEventPath(httpContext.Request.Path)))
        {
            context.Succeed(requirement);
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

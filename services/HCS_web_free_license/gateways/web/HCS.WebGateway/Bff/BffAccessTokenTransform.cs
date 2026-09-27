using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace HCS.WebGateway;

internal static class BffAccessTokenTransform
{
    internal static void Add(TransformBuilderContext builderContext)
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            Apply(transformContext.HttpContext, transformContext.ProxyRequest);
            return ValueTask.CompletedTask;
        });
    }

    internal static void Apply(HttpContext httpContext, HttpRequestMessage proxyRequest)
    {
        var token = ResolveAccessToken(httpContext);
        proxyRequest.Headers.Remove("Authorization");
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        proxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    internal static string? ResolveAccessToken(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(BffAccessTokenMiddleware.AccessTokenItemKey, out var value) &&
            value is string accessToken &&
            !string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken.Trim();
        }

        var header = httpContext.Request.Headers.Authorization;
        if (header.Count == 0)
        {
            return null;
        }

        // StringValues.ToString() joins duplicate Authorization values with ", ".
        // A 3-part JWS then looks like a 5-part JWE and JwtBearer throws IDX14309.
        var first = header[0];
        if (string.IsNullOrWhiteSpace(first))
        {
            return null;
        }

        return AuthenticationHeaderValue.TryParse(first, out var parsed) &&
            parsed.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parsed.Parameter)
            ? parsed.Parameter
            : null;
    }
}

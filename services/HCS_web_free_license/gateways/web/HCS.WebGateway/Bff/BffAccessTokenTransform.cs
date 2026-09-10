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
        // Anonymous bootstrap paths (application-configuration/localization) proxy without a token.
        if (httpContext.Items.TryGetValue(BffAccessTokenMiddleware.AccessTokenItemKey, out var value) &&
            value is string accessToken &&
            !string.IsNullOrWhiteSpace(accessToken))
        {
            proxyRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        // YARP copies the incoming Authorization header by default. Native Bearer
        // requests must be left untouched here; appending it again can produce a
        // combined value that downstream token validators reject as malformed.
    }
}

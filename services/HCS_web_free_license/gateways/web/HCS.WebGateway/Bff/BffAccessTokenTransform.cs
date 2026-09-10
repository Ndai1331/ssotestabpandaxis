using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace HCS.WebGateway;

internal static class BffAccessTokenTransform
{
    internal static void Add(TransformBuilderContext builderContext)
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            // Anonymous bootstrap paths (application-configuration/localization) proxy without a token.
            if (transformContext.HttpContext.Items.TryGetValue(BffAccessTokenMiddleware.AccessTokenItemKey, out var value) &&
                value is string accessToken &&
                !string.IsNullOrWhiteSpace(accessToken))
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
            else if (BffRequestPolicy.HasBearerCredentials(transformContext.HttpContext.Request))
            {
                var authorization = transformContext.HttpContext.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrWhiteSpace(authorization))
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("Authorization", authorization);
                }
            }

            return ValueTask.CompletedTask;
        });
    }
}

using System;
using HCS.Blazor.Client.Navigation;
using Microsoft.Extensions.Configuration;

namespace HCS.Blazor.Client.Authentication;

internal static class BffUnauthenticatedDocumentRedirect
{
    private static readonly string[] SkippedPathPrefixes =
    [
        "/_framework",
        "/_content",
        "/_blazor",
        "/_vs"
    ];

    public static bool TryGetLoginUrl(
        bool isAuthenticated,
        string httpMethod,
        string path,
        string? queryString,
        string? acceptHeader,
        IConfiguration configuration,
        out string loginUrl)
    {
        loginUrl = null!;
        if (!ShouldRedirect(isAuthenticated, httpMethod, path, acceptHeader))
        {
            return false;
        }

        loginUrl = BffLoginUrlBuilder.Build(configuration, BuildReturnUrl(configuration, path, queryString));
        return true;
    }

    internal static bool ShouldRedirect(
        bool isAuthenticated,
        string httpMethod,
        string path,
        string? acceptHeader)
    {
        if (isAuthenticated)
        {
            return false;
        }

        if (!httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!AcceptsHtml(acceptHeader) || IsSkippedPath(path) || BffAnonymousRoutes.IsAnonymous(path))
        {
            return false;
        }

        return true;
    }

    internal static string BuildReturnUrl(IConfiguration configuration, string path, string? queryString)
    {
        var appRoot = GetHttpsAppRoot(configuration);
        if (BffAnonymousRoutes.IsLogin(path))
        {
            return appRoot.AbsoluteUri;
        }

        var builder = new UriBuilder(appRoot)
        {
            Path = string.IsNullOrWhiteSpace(path) ? "/" : path
        };
        if (!string.IsNullOrWhiteSpace(queryString))
        {
            builder.Query = queryString.TrimStart('?');
        }

        return builder.Uri.AbsoluteUri;
    }

    private static bool AcceptsHtml(string? acceptHeader) =>
        !string.IsNullOrWhiteSpace(acceptHeader) &&
        acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase);

    private static bool IsSkippedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.Equals("/culture", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/culture/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var prefix in SkippedPathPrefixes)
        {
            if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var slash = path.LastIndexOf('/');
        var segment = slash >= 0 ? path[(slash + 1)..] : path;
        return segment.Contains('.', StringComparison.Ordinal);
    }

    private static Uri GetHttpsAppRoot(IConfiguration configuration)
    {
        var selfUrl = configuration["App:SelfUrl"];
        if (!Uri.TryCreate(selfUrl, UriKind.Absolute, out var self) ||
            self.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("App:SelfUrl must be an absolute HTTPS URL.");
        }

        return new Uri(self.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
    }
}

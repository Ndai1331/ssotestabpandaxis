using System;

namespace HCS.Blazor.Client.Authentication;

internal static class BffAnonymousRoutes
{
    public static bool IsAnonymous(string? relativePath)
    {
        var path = Normalize(relativePath);
        return path.StartsWith("survey-collections/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLogin(string? relativePath)
    {
        var path = Normalize(relativePath);
        return path.Equals("login", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("login/", StringComparison.OrdinalIgnoreCase);
    }

    internal static string Normalize(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        return relativePath.Split('?', '#')[0].Trim('/');
    }
}

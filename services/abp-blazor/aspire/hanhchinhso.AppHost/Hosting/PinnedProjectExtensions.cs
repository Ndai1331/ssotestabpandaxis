using Aspire.Hosting.ApplicationModel;

namespace hanhchinhso.AppHost.Hosting;

/// <summary>
/// Adds a .NET project with a fixed localhost HTTP port (no Aspire reverse proxy).
/// Required so ABP OIDC / YARP URLs in appsettings keep working.
/// </summary>
internal static class PinnedProjectExtensions
{
    private const string DevEnvironment = "Development";

    public static IResourceBuilder<ProjectResource> AddPinnedHttpProject<TProject>(
        this IDistributedApplicationBuilder builder,
        string name,
        int port)
        where TProject : IProjectMetadata, new()
    {
        // launchProfileName: null — skip launchSettings endpoints (avoids proxy port remap)
        return builder.AddProject<TProject>(name, launchProfileName: null)
            .WithHttpEndpoint(port: port, targetPort: port, name: "http", isProxied: false)
            // String concat — Aspire treats $"..." as ReferenceExpression, not a plain string.
            .WithEnvironment("ASPNETCORE_URLS", "http://localhost:" + port)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", DevEnvironment)
            .WithEnvironment("DOTNET_ENVIRONMENT", DevEnvironment);
    }
}

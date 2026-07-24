using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

builder.RootComponents.Add<App>("#app");

var backendUri = new Uri(configuration["Elsa:BackendUrl"] ?? "http://localhost:44395/elsa/api");

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(new BackendApiConfig
{
    ConfigureHttpClientBuilder = options => options.BaseAddress = backendUri,
    ConfigureBackendOptions = options => options.Url = backendUri
});
builder.Services
    .AddLoginModule()
    .UseOpenIdConnect(connectConfiguration =>
    {
        // AuthServer OpenIddict (federates Keycloak upstream in BD lab)
        var authority = (configuration["Oidc:Authority"] ?? "http://localhost:44372").TrimEnd('/');
        connectConfiguration.AuthEndpoint = $"{authority}/connect/authorize";
        connectConfiguration.TokenEndpoint = $"{authority}/connect/token";
        connectConfiguration.EndSessionEndpoint = $"{authority}/connect/endsession";
        connectConfiguration.ClientId = configuration["Oidc:ClientId"] ?? "ElsaStudio";
        connectConfiguration.Scopes =
        [
            "openid",
            "profile",
            "email",
            "roles",
            "offline_access",
            "WorkflowService"
        ];
    });
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();

await builder.Build().RunAsync();

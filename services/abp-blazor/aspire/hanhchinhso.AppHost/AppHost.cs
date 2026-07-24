// Aspire AppHost SDK 13.4.6 — CLI SoT for local stack (not ABP Studio).
// Profiles: light (default) | full. Pin ports — never let Aspire proxy remap OIDC/YARP URLs.
using hanhchinhso.AppHost.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var profile = RunProfile.Resolve(args);
Console.WriteLine($"[hanhchinhso.AppHost] HCS_RUN_PROFILE={profile}");

// Light profile — core login stack
var identity = builder.AddPinnedHttpProject<Projects.hanhchinhso_IdentityService>("identity", 44392);
var administration = builder.AddPinnedHttpProject<Projects.hanhchinhso_AdministrationService>("administration", 44323);
var language = builder.AddPinnedHttpProject<Projects.hanhchinhso_LanguageService>("language", 44391);

var authServer = builder.AddPinnedHttpProject<Projects.hanhchinhso_AuthServer>("auth-server", 44372)
    .WaitFor(identity)
    .WaitFor(administration);

var webGateway = builder.AddPinnedHttpProject<Projects.hanhchinhso_WebGateway>("web-gateway", 44398)
    .WaitFor(authServer)
    .WaitFor(identity)
    .WaitFor(administration)
    .WaitFor(language);

builder.AddPinnedHttpProject<Projects.hanhchinhso_Blazor>("blazor", 44306)
    .WaitFor(webGateway)
    .WaitFor(authServer);

if (profile == RunProfile.Full)
{
    builder.AddPinnedHttpProject<Projects.hanhchinhso_AuditLoggingService>("audit-logging", 44302);
    builder.AddPinnedHttpProject<Projects.hanhchinhso_GdprService>("gdpr", 44348);
    builder.AddPinnedHttpProject<Projects.hanhchinhso_AIManagementService>("ai-management", 44318);
    builder.AddPinnedHttpProject<Projects.hanhchinhso_OrganizationService>("organization", 44370);

    builder.AddPinnedHttpProject<Projects.hanhchinhso_WorkflowService>("workflow", 44395)
        .WaitFor(authServer)
        .WaitFor(identity);

    // Wait for AuthServer only — Workflow may fail independently (Elsa DI); Studio UI still useful.
    builder.AddPinnedHttpProject<Projects.HanhChinhSo_ElsaStudio>("elsa-studio", 44396)
        .WaitFor(authServer);
}

builder.Build().Run();

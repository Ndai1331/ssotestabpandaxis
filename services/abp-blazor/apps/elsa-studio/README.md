# Elsa Studio — HanhChinhSo

Blazor WebAssembly standalone app for the **Elsa Workflow Designer** (Elsa Pro 3.5.x), wired to `hanhchinhso.WorkflowService` `:44395` via OIDC Code+PKCE through the ABP AuthServer `:44372`.

## Port

`http://localhost:44396`

## Auth flow

```
Browser → Elsa Studio :44396
  → OIDC Code+PKCE → AuthServer :44372 (OpenIddict)
  → access_token (aud=WorkflowService)
  → REST /elsa/api → WorkflowService :44395
```

## Prerequisites

1. ABP `abp login` with a **Team+** license (required for `Volo.Abp.Elsa.*` packages).
2. All infra running: PostgreSQL, Redis, RabbitMQ, Keycloak `:5110`.
3. AuthServer `:44372` seeded (OpenIddict client `ElsaStudio` registered).
4. WorkflowService `:44395` running and Elsa tables migrated.

## Run order

Start services in this order (or use `Default` run profile in ABP Studio):
1. AuthServer `:44372`
2. IdentityService `:44392`
3. AdministrationService `:44323`
4. WorkflowService `:44395`
5. **Elsa Studio `:44396`** (this app) — `dotnet run` in this folder

## Package versions

| Package | Version |
|---------|---------|
| Elsa.Studio | 3.5.0 |
| Elsa.Studio.Core.BlazorWasm | 3.5.0 |
| Elsa.Studio.Shell | 3.5.0 |
| Elsa.Studio.Dashboard | 3.5.0 |
| Elsa.Studio.Workflows | 3.5.0 |
| Elsa.Studio.Login.BlazorWasm | 3.5.0 |

> If `Elsa.Studio.Login.BlazorWasm` is unavailable on NuGet, try `Elsa.Studio.Login` or `Elsa.Studio.OAuth2` (exact package name varies by Elsa 3.5 release).

## API deviation note

- `AddRemoteBackend(url)` — extension method from `Elsa.Studio.Core.BlazorWasm`. If the actual method name differs (e.g. `AddElsaStudio(opt => opt.BackendUrl = ...)`) adjust `Program.cs` accordingly.
- OIDC provider key is `"Oidc"` mapped in `wwwroot/appsettings.json` → `builder.Configuration.Bind("Oidc", options.ProviderOptions)`.

# Aspire AppHost — run hanhchinhso locally (one command)

CLI source of truth for starting ABP microservices **without ABP Studio**.

- Aspire SDK: **13.4.6**
- Infra: existing `etc/docker` (not Aspire-owned containers)
- Ports: **pinned** to match `appsettings` / OIDC / YARP (no Aspire reverse-proxy remap)

## Quick start

```bash
cd services/abp-blazor
./aspire/run.sh          # light (default)
./aspire/run.sh full     # all services + Elsa Studio
```

Or:

```bash
dotnet run --project aspire/hanhchinhso.AppHost --launch-profile http -- --profile light
```

Env alternative: `HCS_RUN_PROFILE=full`.

## Profiles

| Profile | Apps | Infra |
|---------|------|-------|
| **light** | AuthServer `44372`, Identity `44392`, Administration `44323`, Language `44391`, WebGateway `44398`, Blazor `44306` | postgres + redis + rabbitmq |
| **full** | light + AuditLogging `44302`, Gdpr `44348`, AIManagement `44318`, Organization `44370`, WorkflowService `44395`, ElsaStudio `44396` | `etc/docker/up.ps1` (all) |

## Keycloak (SSO)

Not started by AppHost. Separate:

```bash
cd services/directus-main
docker compose up -d keycloak
# http://localhost:5110  (admin / secret)
```

## Hard rules when adding a service

1. Add `ProjectReference` in `hanhchinhso.AppHost.csproj`.
2. Register with `AddPinnedHttpProject<Projects.…>(name, port)` — **always** `isProxied: false`.
3. Put it in `light` only if needed for core login UI; else `full`.
4. Optionally sync `etc/abp-studio/run-profiles/Default.abprun.json` for Studio users.

## Notes

- **Light:** Gateway YARP still has routes to full-only services → those paths return 502 until you use `full`.
- **Full:** WorkflowService may crash on known Elsa Identity DI issue (`AbpIdentityAccessTokenIssuer`) — AppHost still starts the rest; fix is outside this runner.
- Prefer `./aspire/run.sh` over raw `dotnet run` (sets infra + `ASPIRE_ALLOW_UNSECURED_TRANSPORT`). Profile `http` in launchSettings also sets that env for IDE runs.

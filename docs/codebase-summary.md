# Codebase Summary — BD Workspace

> Updated: 2026-07-24. Local SSO lab only.

## Meta repo

`bd-workspace` chứa docs, agent rules, và hai tree application local:

| Path | Stack | Role |
|------|-------|------|
| `services/directus-main-v11` | Directus v11 lab SoT (Node monorepo) | Clinical data + compose Keycloak SSO |
| `services/directus-main` | Directus v12 archive | Không dùng cho SSO lab |
| `services/abp-blazor` | ABP .NET 10 microservice | Digital admin app (Blazor + AuthServer + services) |

## Notable local infra

- Keycloak in Directus `docker-compose.yml` → host port **5110**  
- Directus compose also exposes Postgres, Redis, Minio, Maildev for debug  

## ABP solution highlights

- Solution name pattern: `hanhchinhso`  
- Apps: AuthServer (OpenIddict), Blazor, Elsa Studio WASM (:44396)  
- Services: identity, administration, audit-logging, gdpr, language, ai-management, **workflow-service** (Elsa Pro 3.5, :44395), …  
- Gateways: web BFF  
- **Local runner:** Aspire AppHost (`.NET 13.4.6`); `./aspire/run.sh [light|full]` — see [`aspire/README.md`](../services/abp-blazor/aspire/README.md)  

## Auth target state

Keycloak = central IdP; Directus + ABP = OIDC clients; Zimbra = LDAP/auth source.

## Legacy

Historical Task9 plans under `plans/` and old wiki pages are **not** part of the active codebase summary.

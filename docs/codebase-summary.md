# Codebase Summary — BD Workspace

> Updated: 2026-07-23. Local SSO lab only.

## Meta repo

`bd-workspace` chứa docs, agent rules, và hai tree application local:

| Path | Stack | Role |
|------|-------|------|
| `services/directus-main` | Directus (Node monorepo) | Clinical data app + compose Keycloak |
| `services/abp-blazor` | ABP .NET 10 microservice | Digital admin app (Blazor + AuthServer + services) |

## Notable local infra

- Keycloak in Directus `docker-compose.yml` → host port **5110**  
- Directus compose also exposes Postgres, Redis, Minio, Maildev for debug  

## ABP solution highlights

- Solution name pattern: `abptestwithsso`  
- Apps: AuthServer (OpenIddict), Blazor  
- Services: identity, administration, audit-logging, gdpr, language, ai-management, …  
- Gateways: web BFF  

## Auth target state

Keycloak = central IdP; Directus + ABP = OIDC clients; Zimbra = LDAP/auth source.

## Legacy

Historical Task9 plans under `plans/` and old wiki pages are **not** part of the active codebase summary.

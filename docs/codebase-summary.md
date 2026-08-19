# Codebase Summary — BD Workspace

> Updated: 2026-08-10. Local SSO lab only.

## Meta repo

`bd-workspace` chứa docs, agent rules, và các tree application local:

| Path | Stack | Role |
|------|-------|------|
| `services/directus-main-v11` | Directus v11 lab SoT (Node monorepo) | Clinical data + compose Keycloak SSO |
| `services/directus-main` | Directus v12 archive | Không dùng cho SSO lab |
| `services/HCS_web_free_license` | ABP Community / .NET 10 microservices | Runtime HCS hiện tại: Blazor UI, BFF gateway, AuthServer và domain services |
| `services/abp-blazor` | ABP .NET 10 microservice template | Tham chiếu lịch sử, không phải runtime HCS mặc định |

## Notable local infra

- Keycloak in Directus `docker-compose.yml` → host port **5110**  
- Directus compose also exposes Postgres, Redis, Minio, Maildev for debug  

## HCS Community runtime highlights

- Default runtime: Docker Compose from `services/HCS_web_free_license/`; browser entry is `https://hcs.localhost`.
- Browser flow: protected Blazor routes → Gateway `/bff/login` → AuthServer → Keycloak; the gateway allows deep-link returns only to configured UI origins.
- Primary HCS-specific navigation is Chat (`/chat`), guarded by the `Collaboration.Chat` policy/permission.
- Runtime details and safe startup/rollback: [`runbooks/hcs-docker-compose-handoff.md`](./runbooks/hcs-docker-compose-handoff.md).

## Auth target state

Keycloak = central IdP; Directus + HCS AuthServer/BFF = OIDC consumers; Zimbra = LDAP/auth source.

## Legacy

Historical Task9 plans under `plans/` and old wiki pages are **not** part of the active codebase summary.

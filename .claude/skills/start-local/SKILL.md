---
name: start-local
description: Start BD local lab services (Keycloak :5110; optionally remind how to run Directus + ABP). Use when the user says "start local", "chạy local", "start keycloak", or after setting up the BD SSO workspace.
---

# start-local (BD SSO Lab)

Launcher guidance for the **Bình Dương** local stack. Replaces the old Task9 UI:5053 / API:7093 script.

> Script `start-local.sh` cũ (Task9) **không còn đúng**. Dùng các lệnh dưới đây cho đến khi có script BD mới.

## When to use
- User: "start local" / "chạy local" / "start keycloak" / "start SSO lab".

## What to start

| Service | How | URL |
|---------|-----|-----|
| **Keycloak** | `docker compose` in Directus | http://localhost:5110 (admin/secret) |
| **Directus** | Theo `services/directus-main/readme.md` | Studio URL theo env |
| **ABP** | Infra `etc/docker` + `dotnet run` / ABP Studio | Theo launch profiles |

## Keycloak (minimum for SSO lab)

```bash
cd services/directus-main
docker compose up -d keycloak
# Optional infra for Directus:
# docker compose up -d postgres redis
```

## ABP dependencies

```bash
cd services/abp-blazor/etc/docker
# Follow README: up.ps1 / compose up
```

Then run AuthServer + Blazor per `services/abp-blazor/README.md` (pfx cert, `abp install-libs`).

## Directus app

Follow upstream Directus monorepo docs in `services/directus-main/readme.md` (pnpm). Configure OIDC → Keycloak realm after Keycloak is up.

## Agent notes
- Không start Task9 ports 5053/7093 — services không tồn tại trong workspace này.
- Sau start: báo URL Keycloak + nhắc hard-refresh khi test login.
- Zimbra LDAP có thể chưa có — dùng user Keycloak local cho POC.

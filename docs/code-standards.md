# Code Standards — BD Workspace

> Phase local SSO lab. Bổ sung khi team chốt convention chi tiết hơn.

## General

- Comments: English. User-facing agent chat: Vietnamese.  
- YAGNI / KISS / DRY.  
- Verify paths/APIs in code before documenting.

## Directus (`services/directus-main-v11`)

- Prefer upstream patterns (pnpm workspace, existing packages).  
- OIDC/env secrets: `.env` local, never commit.  
- Extensions: follow Directus extension layout nếu thêm custom.

## HCS Community (`services/HCS_web_free_license`)

- Follow ABP microservice template layout (`apps/`, `gateways/`, `services/`).  
- Do not commit private signing keys, certificates, `.env` files, or client secrets.
- Browser authentication is BFF-owned: protected UI routes must start login through `/bff/login`; return URLs must remain absolute HTTPS URLs on a configured UI origin.
- Keep browser tokens out of client code; use the secure HTTP-only BFF session and the configured gateway client.
- The custom main-menu contribution is limited to Chat and must retain its `Collaboration.Chat` permission guard; ABP administration visibility remains permission-driven.

## Historical ABP template (`services/abp-blazor`)

- Preserve its existing microservice layout when maintaining the archived/reference template.

## Workspace docs

- SoT architecture: `docs/workspace-architecture.md`.  
- Agent rules: `CLAUDE.md`.  
- After structural change: update README + wiki hot/index.

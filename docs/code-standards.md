# Code Standards — BD Workspace

> Phase local SSO lab. Bổ sung khi team chốt convention chi tiết hơn.

## General

- Comments: English. User-facing agent chat: Vietnamese.  
- YAGNI / KISS / DRY.  
- Verify paths/APIs in code before documenting.

## Directus (`services/directus-main`)

- Prefer upstream patterns (pnpm workspace, existing packages).  
- OIDC/env secrets: `.env` local, never commit.  
- Extensions: follow Directus extension layout nếu thêm custom.

## ABP (`services/abp-blazor`)

- Follow ABP microservice template layout (`apps/`, `gateways/`, `services/`).  
- Do not commit private signing keys; `openiddict.pfx` generate local per README.  
- External IdP config: keep realm/client IDs in appsettings / user secrets, not hardcoded secrets in git.

## Workspace docs

- SoT architecture: `docs/workspace-architecture.md`.  
- Agent rules: `CLAUDE.md`.  
- After structural change: update README + wiki hot/index.

---
phase: 1
title: "Port lab artifacts"
status: completed
effort: 1h
dependsOn: []
---

# Phase 01 — Port lab artifacts

## Context

- Brainstorm: [`plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md`](../../reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md)
- Source compose: `services/directus-main/docker-compose.bd-lab.yml`
- Source gate: `services/directus-main/bd-lab-extensions/directus-extension-bd-app-gate/`
- Target root: `services/directus-main-v11/`
- v11 OpenID hooks `auth.create` / `auth.update` already exist — extension port-as-is

## Overview

Copy compose lab + app-gate extension + `.env.sso.example` vào v11. Adapt env: **drop** `BD_LAB_ALLOW_SSO`; rename volumes/image; fix stale comment `bd-app-directus` → `bd-app-axis`.

## Requirements

- Compose: database, cache, keycloak, directus (build local Dockerfile)
- Mount extension at `/directus/extensions/directus-extension-bd-app-gate`
- OpenID env keys same as v12 lab (client `directus`, issuer via `keycloak:5110` DNS)
- ROLE_MAPPING / DEFAULT_ROLE_ID = placeholders hoặc comment “fill phase 2”
- No license bypass env/code

## Related files

| Action | Path |
|--------|------|
| Create | `services/directus-main-v11/docker-compose.bd-lab.yml` |
| Create | `services/directus-main-v11/bd-lab-extensions/directus-extension-bd-app-gate/index.js` |
| Create | `services/directus-main-v11/bd-lab-extensions/directus-extension-bd-app-gate/package.json` |
| Create | `services/directus-main-v11/.env.sso.example` |
| Read | `services/directus-main-v11/Dockerfile` (must exist for build) |

## Implementation steps

1. Copy extension directory → v11; keep `host: ^11.0.0`, group `bd-app-axis`.
2. Copy `.env.sso.example` → v11 (no `BD_LAB_ALLOW_SSO`).
3. Copy `docker-compose.bd-lab.yml` → v11 and edit:
   - Header comments: path `directus-main-v11`
   - Image: `bd-axis-v11:local`
   - Volumes: `bd_axis_v11_pg`, `bd_axis_v11_uploads`, `bd_axis_v11_extensions`
   - **Remove** `BD_LAB_ALLOW_SSO`
   - Comment mount: require group `bd-app-axis` (not `bd-app-directus`)
   - Keep AUTH_* block; leave ROLE_MAPPING as TODO placeholders or empty with comment
   - Keep KC hostname `localhost:5110` + backchannel dynamic
4. Sanity: `grep -r BD_LAB_ALLOW_SSO services/directus-main-v11` → no hits in new files.

## Todo

- [x] Extension copied
- [x] `.env.sso.example` copied
- [x] Compose adapted (no license bypass, new volumes/image)
- [x] Comment `bd-app-axis` fixed

## Success criteria

- [x] Files exist under v11 paths above
- [x] Compose YAML valid (`docker compose -f ... config`)
- [x] Zero `BD_LAB_ALLOW_SSO` in v11 lab artifacts

## Risks

| Risk | Mitigation |
|------|------------|
| Dockerfile v11 build differs | Phase 2 catches; don't change Dockerfile unless build fails |
| Copy UUID từ v12 | Don't — phase 2 seeds fresh |

## Next

Phase 02 — build/up + roles + mapping.

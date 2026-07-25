---
phase: 2
title: "Boot + roles + ROLE_MAPPING"
status: completed
effort: 1.5h
dependsOn: [1]
---

# Phase 02 — Boot + roles + ROLE_MAPPING

## Context

- Phase 01 artifacts ready under `services/directus-main-v11`
- Bootstrap script: `scripts/keycloak_bootstrap_bd_realm.py` (reuse)
- Runbook pattern: `docs/runbooks/local-sso-lab.md` (still points v12 until phase 4)

## Overview

Stop v12 lab if running (port conflict `:5110`/`:8055`). Build + up v11 compose. Bootstrap KC realm. Create 4 Studio roles; paste UUIDs into compose `AUTH_KEYCLOAK_*`; recreate Directus container.

## Requirements

- Stack healthy: PG, Redis, Keycloak, Directus ping
- Local admin: `admin@local.dev` / `admin123456` (from compose)
- Roles: Admin, BacSi, LanhDao, NhanVien (names match lab convention)
- `AUTH_KEYCLOAK_DEFAULT_ROLE_ID` = NhanVien UUID
- `AUTH_KEYCLOAK_ROLE_MAPPING` with `json:` prefix + 4 groups
- KC realm `bd` + client `directus` + groups claim

## Implementation steps

1. **Stop v12 lab** (if up):
   ```bash
   cd services/directus-main
   docker compose -f docker-compose.bd-lab.yml down
   ```
2. **Build + up v11:**
   ```bash
   cd services/directus-main-v11
   docker compose -f docker-compose.bd-lab.yml build directus
   docker compose -f docker-compose.bd-lab.yml up -d
   ```
3. Wait health: `http://localhost:8055/server/ping`, KC `http://localhost:5110`
4. **Bootstrap KC:**
   ```bash
   KEYCLOAK_URL=http://127.0.0.1:5110 python3 ../../scripts/keycloak_bootstrap_bd_realm.py
   ```
5. Login Studio local admin → Access Control → create 4 roles if missing → copy UUIDs (query `axis_roles` OK).
6. Update compose `AUTH_KEYCLOAK_DEFAULT_ROLE_ID` + `AUTH_KEYCLOAK_ROLE_MAPPING`.
7. `docker compose -f docker-compose.bd-lab.yml up -d --force-recreate directus`
8. Confirm OpenID discovery in logs (no fatal exit). Optional: `AUTH_KEYCLOAK_ISSUER_DISCOVERY_MUST_SUCCEED=false` already in compose.

## Todo

- [x] v12 stack down
- [x] v11 image built + containers up
- [x] KC bootstrap OK
- [x] 4 roles + UUIDs in compose
- [x] Directus recreated with mapping

## Success criteria

- [x] Studio reachable `:8055`
- [x] Provider Keycloak listed (or env loaded — verify login page / auth providers)
- [x] Mapping JSON valid (object not array)

## Risks

| Risk | Mitigation |
|------|------------|
| Build OOM / long time | Increase Docker memory; retry; check Dockerfile Node 22 |
| Tables `axis_*` vs `directus_*` | Must **build fork** image — never pull upstream-only image for this lab |
| Port 5120/5121 conflict | Align with compose; stop other stacks |
| Discovery fail Axis→KC | Keep issuer `http://keycloak:5110/...` inside Docker network |

## Security

- Lab secrets only (`bd-directus-lab-secret`); do not commit real prod secrets
- `.env` local overrides stay untracked

## Next

Phase 03 — smoke login + gate deny.

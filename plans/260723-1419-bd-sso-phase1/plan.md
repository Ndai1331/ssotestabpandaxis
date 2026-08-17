---
title: "BD SSO Phase 1 — Keycloak + Directus + ABP"
description: "POC local: Keycloak realm, Directus OpenID, ABP AuthServer federate Keycloak (Approach A), 4 roles, E2E SSO"
status: completed
priority: P1
effort: 12h
branch: local
tags: ["sso", "keycloak", "directus", "abp", "oidc", "bd"]
blockedBy: []
blocks: []
created: "2026-07-23"
createdBy: "ck:plan"
completed: "2026-07-23"
source: "plans/reports/brainstorm-260723-1415-bd-sso-login-flow.md"
handoff: "docs/handoff/phase1-sso-context.md"
---

# BD SSO Phase 1 — Keycloak + Directus + ABP

## Status: COMPLETED (2026-07-23)

**AI context handoff:** [`docs/handoff/phase1-sso-context.md`](../../docs/handoff/phase1-sso-context.md)  
**Runbook:** [`docs/runbooks/local-sso-lab.md`](../../docs/runbooks/local-sso-lab.md)

## Overview

Implement POC SSO theo design đã duyệt ([[SSO Phase 1 Approach A]]):

- Keycloak local `:5110` = IdP (users local, chưa Zimbra)
- Directus `:8055` = OIDC client `directus`
- ABP AuthServer `:44372` federate Keycloak; Blazor `:44306` (**Approach A**)
- 4 roles ↔ groups `bd-admin|bacsi|lanhdao|nhanvien`
- Host: **localhost ports** only; lab `prompt=login`

## Phases

| Phase | Name | Status |
|-------|------|--------|
| 1 | [Keycloak realm & clients](./phase-01-keycloak-realm.md) | Done |
| 2 | [Directus OpenID + roles](./phase-02-directus-openid.md) | Done |
| 3 | [ABP AuthServer Keycloak](./phase-03-abp-keycloak-external.md) | Done |
| 4 | [E2E SSO verify + runbook](./phase-04-e2e-runbook.md) | Done |

## Implementation progress

- [x] Keycloak + bootstrap + RS256 + groups claim
- [x] Directus compose lab + ROLE_MAPPING UUIDs + prompt=login
- [x] ABP OIDC Keycloak + role map + permission seed + prompt=login
- [x] Runbook + AI handoff doc

## Next

Phase 2: Zimbra LDAP (chưa plan).
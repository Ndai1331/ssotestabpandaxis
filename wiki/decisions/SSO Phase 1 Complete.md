---
type: decision
title: "SSO Phase 1 Complete"
updated: 2026-07-23
---

# SSO Phase 1 Complete

Phase 1 POC **DONE** (local lab 2026-07-23).

**Handoff AI:** [[phase1-sso-context]] → `docs/handoff/phase1-sso-context.md`  
**Runbook:** `docs/runbooks/local-sso-lab.md`  
**Decision gốc:** [[SSO Phase 1 Approach A]]

## Delivered
- Keycloak realm `bd` + bootstrap script
- Directus OpenID + ROLE_MAPPING 4 UUID + `prompt=login`
- ABP AuthServer external Keycloak + role map + permission seed mẫu
- ABP AuthServer `prompt=login`

## Not in Phase 1
Zimbra LDAP, SLO, prod hosts/TLS, CI.

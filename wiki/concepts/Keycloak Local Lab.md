---
type: concept
title: "Keycloak Local Lab"
updated: 2026-07-23
---

# Keycloak Local Lab

## Start + bootstrap
```bash
cd services/directus-main && docker compose up -d keycloak
python3 scripts/keycloak_bootstrap_bd_realm.py
```

| | |
|--|--|
| URL | http://localhost:5110 |
| Admin | admin / secret |
| Realm | `bd` |
| Discovery | `/realms/bd/.well-known/openid-configuration` |

## Clients
- `directus` → callback `http://localhost:8055/auth/login/keycloak/callback`
- `abp-auth` → `http://localhost:44372/signin-oidc`

## Groups / users
`bd-admin|bacsi|lanhdao|nhanvien` + emails `@benhvien.vn` / password `Passw0rd!`

Runbook: `docs/runbooks/local-sso-lab.md`  
Decision: [[SSO Phase 1 Approach A]]

---
phase: 2
title: "Directus OpenID + roles"
status: pending
effort: 3h
dependsOn: [1]
---

# Phase 02 — Directus OpenID + roles

## Goal

Directus login qua Keycloak; auto-create user; map 4 groups → 4 roles.

## Prerequisites

- Phase 1 done (realm, client `directus`, secret, groups claim)
- Directus API chạy `:8055`, App `:8080` (theo `services/directus-main/AGENTS.md`)
- Postgres (compose `:5100`) hoặc DB đang dùng cho local Directus

## Steps

### 1. Bootstrap Directus (nếu chưa)

- Chạy Directus theo upstream monorepo (pnpm) với `PUBLIC_URL=http://localhost:8055`
- Tạo **local admin** email/password (giữ lại — không `AUTH_DISABLE_DEFAULT`)

### 2. Tạo 4 roles trong Studio

Login local admin → Settings → Access Control / Roles:

| Role name | Mục đích POC |
|-----------|--------------|
| `Admin` | Full (có thể dùng role Admin sẵn có) |
| `BacSi` | Quyền đọc/ghi clinical tối thiểu (placeholder policies OK) |
| `LanhDao` | Read-heavy |
| `NhanVien` | Basic / default |

Copy **role UUID** từng role.

### 3. Env OpenID (local `.env` — không commit secrets)

```bash
AUTH_PROVIDERS="keycloak"

AUTH_KEYCLOAK_DRIVER="openid"
AUTH_KEYCLOAK_CLIENT_ID="directus"
AUTH_KEYCLOAK_CLIENT_SECRET="<from-phase-1>"
AUTH_KEYCLOAK_ISSUER_URL="http://localhost:5110/realms/bd/.well-known/openid-configuration"
AUTH_KEYCLOAK_IDENTIFIER_KEY="email"
AUTH_KEYCLOAK_ALLOW_PUBLIC_REGISTRATION="true"
AUTH_KEYCLOAK_REQUIRE_VERIFIED_EMAIL="false"
AUTH_KEYCLOAK_DEFAULT_ROLE_ID="<uuid-NhanVien>"
AUTH_KEYCLOAK_GROUP_CLAIM_NAME="groups"
# MUST use json: prefix — array sẽ crash driver
AUTH_KEYCLOAK_ROLE_MAPPING="json:{\"bd-admin\":\"<uuid-Admin>\",\"bd-bacsi\":\"<uuid-BacSi>\",\"bd-lanhdao\":\"<uuid-LanhDao>\",\"bd-nhanvien\":\"<uuid-NhanVien>\"}"
AUTH_KEYCLOAK_SCOPE="openid profile email"
```

Optional: `AUTH_KEYCLOAK_SYNC_USER_INFO=true`

### 4. Restart Directus API

Verify logs: OpenID discovery success (không exit vì discovery fail).

### 5. Test login từng user

1. Logout Studio  
2. Login page → provider **keycloak**  
3. Login `admin@benhvien.vn` → vào Studio role Admin  
4. Lặp `bacsi@`, `lanhdao@`, `nhanvien@` → đúng role  

Check DB/`directus_users`: `provider=keycloak`, `external_identifier` = email.

### 6. Document env template

Thêm `services/directus-main/.env.sso.example` (secrets placeholder) + note trong runbook phase 4.

## Code touchpoints (read-only reference)

- Driver: `services/directus-main/api/src/auth/drivers/openid.ts`  
- Router callback: `createOpenIDAuthRouter` → `/auth/login/:provider/callback`  
- Config load: `AUTH_<NAME>_` prefix via `getConfigFromEnv`

**Không** fork driver trừ khi bug — ưu tiên config.

## Success criteria

- [ ] Nút/provider Keycloak hiện trên login  
- [ ] 4 users tạo/link đúng role  
- [ ] Local admin vẫn login được  
- [ ] `.env.sso.example` không chứa secret thật  

## Risks

| Risk | Fix |
|------|-----|
| INVALID_CREDENTIALS + "public registration" | `ALLOW_PUBLIC_REGISTRATION=true` |
| RS512 alg error | Phase 1 RS256 |
| ROLE_MAPPING array | `json:` prefix |
| Callback 400 redirect_uri | Align KC client URI với `PUBLIC_URL` |

## Next

Phase 03 — ABP AuthServer `AddOpenIdConnect`.

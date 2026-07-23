# Brainstorm — BD SSO Login Flow (Phase 1)

**Date:** 2026-07-23  
**Status:** Approved  
**Project:** BD SSO Lab (local)

---

## Problem

Cần POC đăng nhập Zimbra→Keycloak→Directus+ABP theo guideline. Phase hiện tại: **local only**, chưa Zimbra LDAP, chưa GitHub.

User flow mục tiêu:
1. Directus (`localhost`) → Keycloak (`:5110`) → auth code → tạo/link `directus_users` + role map
2. ABP (`localhost`) → cùng Keycloak session → SSO không nhập lại MK → tạo/link `IdentityUser`

## Decisions (user approved 2026-07-23)

| # | Quyết định | Giá trị |
|---|------------|---------|
| D1 | ABP IdP strategy | **Approach A** — AuthServer OpenIddict federate Keycloak (external OIDC) |
| D2 | Identity source POC | **Keycloak local users** trước; Zimbra LDAP = phase sau |
| D3 | Hostnames | **localhost ports** (không `/etc/hosts` `*.benhvien.vn` trong phase 1) |
| D4 | Roles POC | **4 roles:** admin, bác sĩ, lãnh đạo, nhân viên |

## Approaches evaluated

| | Approach | Verdict |
|--|----------|---------|
| A | AuthServer + external Keycloak | **Chọn** — khớp `ConfigureExternalProviders` sẵn có; ít đụng microservice |
| B | Blazor/apps trust Keycloak trực tiếp | Reject phase 1 — sửa auth toàn stack ABP |
| C | Thay OpenIddict bằng Keycloak | Reject — overkill lab |

## Role model (KC groups → apps)

Keycloak **realm roles hoặc groups** (khuyến nghị **groups** cho map Directus `ROLE_MAPPING`):

| KC group | Directus role (tạo trong Studio) | ABP role |
|----------|----------------------------------|----------|
| `bd-admin` | Admin (full) | `admin` |
| `bd-bacsi` | Bác sĩ (scoped clinical) | `bacsi` |
| `bd-lanhdao` | Lãnh đạo (read/approve) | `lanhdao` |
| `bd-nhanvien` | Nhân viên (basic) | `nhanvien` |

POC: 4 user KC, mỗi user 1 group. User đa group: lấy role ưu tiên cao nhất (admin > lãnh đạo > bác sĩ > nhân viên) — document trong plan implement.

## Architecture (phase 1)

```
Browser
  │
  ├─ Directus :8055?  ──OIDC client "directus"──► Keycloak :5110 /realms/bd
  │                      create/link directus_users + ROLE_MAPPING
  │
  └─ ABP Blazor ──► AuthServer ──external OIDC "Keycloak"──► cùng realm
                       create/link IdentityUser + role claims
```

Prod names (docs only): `axis` / `hanhchinhso` / `sso`.benhvien.vn → map sau khi có reverse proxy.

## Implementation considerations

### Keycloak
- Realm `bd`, token signature **RS256**
- Clients: `directus` (confidential), `abp-auth` (confidential)
- Groups + 4 test users
- Group mapper → token claim `groups` (Directus đọc claim này)

### Directus
- Built-in `openid` driver (`api/src/auth/drivers/openid.ts`)
- Env: `AUTH_PROVIDERS=keycloak`, issuer well-known, `ALLOW_PUBLIC_REGISTRATION=true`, `IDENTIFIER_KEY=email`, `GROUP_CLAIM_NAME=groups`, `ROLE_MAPPING` JSON
- Tạo 4 Directus roles + UUID đưa vào ROLE_MAPPING

### ABP
- `abptestwithssoAuthServerModule.ConfigureExternalProviders`: thêm `AddOpenIdConnect("Keycloak", …)`
- Authority = `http://localhost:5110/realms/bd`
- Redirect URI khớp client `abp-auth`
- Auto-provision IdentityUser; map groups → roles (permission seed tối thiểu)

### Out of scope phase 1
- Zimbra LDAP User Federation
- `/etc/hosts` / TLS prod hostnames
- Approach B/C
- Full ABP permission matrix nghiệp vụ
- Logout SSO đồng bộ (front-channel) — có thể phase 1.5

## Risks

| Risk | Mitigation |
|------|------------|
| Dual hop ABP (Blazor→AuthServer→KC) | Accept DX; document trong runbook |
| Directus alg RS512 mismatch | Force realm RS256 |
| Role UUID Directus đổi sau recreate DB | Document seed step; dùng env ROLE_MAPPING |
| User thiếu group → default role | `DEFAULT_ROLE_ID` = nhân viên |
| Cookie SameSite localhost multi-port | Test Chrome; cùng host `localhost` thường OK |

## Success metrics

- [ ] Login Directus qua KC với từng role (4 users)
- [ ] Role trong Directus khớp group KC
- [ ] Mở ABP sau Directus: **không** nhập lại password
- [ ] IdentityUser tồn tại + role ABP khớp
- [ ] Logout KC (admin console / end-session) → session app hết (best-effort)

## Next steps

1. `/ck:plan` → plan phases: KC → Directus OIDC → ABP external → E2E verify  
2. Implement theo cook khi user bảo làm  
3. Phase 2: Zimbra LDAP federation  

## Unresolved questions

- Port chính xác Directus/ABP local khi chạy lần đầu (ghi vào plan khi measure).  
- ABP role names exact string trong Identity seed (admin vs Admin) — chốt lúc implement.  
- Có tắt login local Directus (`AUTH_DISABLE_DEFAULT`) ngay phase 1 không? Gợi ý: **giữ local admin** cho bootstrap, tắt sau khi SSO ổn.

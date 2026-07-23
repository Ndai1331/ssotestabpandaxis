---
phase: 1
title: "Keycloak realm & clients"
status: pending
effort: 2h
---

# Phase 01 — Keycloak realm & clients

## Goal

Keycloak chạy local, realm `bd` sẵn sàng cho Directus + ABP AuthServer.

## Steps

### 1. Start Keycloak

```bash
cd /Users/user/Documents/bd-workspace/services/directus-main
docker compose up -d keycloak
# http://localhost:5110 — admin / secret
```

Verify: admin console load OK.

### 2. Realm

- Create realm: **`bd`**
- Realm Settings → Tokens → Default Signature Algorithm: **RS256**
- (Optional) Login theme default

### 3. Groups

Create groups:

- `bd-admin`
- `bd-bacsi`
- `bd-lanhdao`
- `bd-nhanvien`

### 4. Test users (mỗi user 1 group)

| Username / Email | Password (lab) | Group |
|------------------|----------------|-------|
| `admin@benhvien.vn` | `Passw0rd!` | bd-admin |
| `bacsi@benhvien.vn` | `Passw0rd!` | bd-bacsi |
| `lanhdao@benhvien.vn` | `Passw0rd!` | bd-lanhdao |
| `nhanvien@benhvien.vn` | `Passw0rd!` | bd-nhanvien |

Email verified = ON. Required actions = none.

### 5. Client `directus` (confidential)

| Setting | Value |
|---------|-------|
| Client ID | `directus` |
| Client authentication | ON |
| Standard flow | ON |
| Direct access grants | OFF (POC) |
| Valid redirect URIs | `http://localhost:8055/auth/login/keycloak/callback` |
| Web origins | `http://localhost:8055`, `http://localhost:8080` |
| Copy **Client secret** | → lưu cho phase 2 env (không commit) |

> Nếu Directus callback path khác version — verify từ docs/`openid` router (`/auth/login/<provider>/callback`). Adjust URI nếu 404.

### 6. Client `abp-auth` (confidential)

| Setting | Value |
|---------|-------|
| Client ID | `abp-auth` |
| Client authentication | ON |
| Standard flow | ON |
| Valid redirect URIs | `http://localhost:44372/signin-oidc` (và `/signin-keycloak` nếu ABP đặt path riêng — chỉnh phase 3) |
| Web origins | `http://localhost:44372`, `http://localhost:44306` |
| Copy client secret | → phase 3 user-secrets / appsettings.Development (không commit secret) |

### 7. Protocol mapper — groups claim

Trên cả 2 clients (hoặc realm default client scopes):

- Mapper type: Group Membership  
- Token Claim Name: **`groups`**  
- Full group path: OFF (chỉ tên `bd-admin` …)  
- Add to ID token: ON  
- Add to access token: ON  
- Add to userinfo: ON  

### 8. Smoke

- Open `http://localhost:5110/realms/bd/.well-known/openid-configuration` → 200 JSON  
- Login account console với `admin@benhvien.vn` OK  

## Deliverables

- [ ] Realm `bd` + RS256  
- [ ] 4 groups + 4 users  
- [ ] Clients `directus` + `abp-auth` + secrets (local only)  
- [ ] `groups` claim present in token (test via KC token endpoint hoặc jwt.io sau login)  

## Risks

- Redirect URI sai → fix ở phase 2/3 khi biết exact callback  
- Image Keycloak `latest` breaking — pin version trong compose nếu cần (optional)  

## Next

Phase 02 — Directus OpenID env + roles.

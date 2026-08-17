# BD SSO Phase 1 — AI Context Handoff

> **Dán file này (hoặc đường dẫn) vào prompt chat mới** để AI hiểu ngữ cảnh Phase 1 đã xong.  
> Workspace: `/Users/user/Documents/bd-workspace`  
> Ngày hoàn thành lab: **2026-07-23** · Directus lab SoT chuyển **v11** (cook 2026-07-25): `services/directus-main-v11`  
> Runbook vận hành: [`docs/runbooks/local-sso-lab.md`](../runbooks/local-sso-lab.md)  
> v12 archive: `services/directus-main/ARCHIVE.md`

---

## 1. Mục tiêu Phase 1 (đã đạt)

POC **local-only** SSO: một tài khoản Keycloak login được **Directus** + **ABP Blazor** qua OIDC.

```
Browser
  ├─ Directus :8055  ──OIDC client "directus"──► Keycloak :5110 /realms/bd
  └─ Blazor :44306 → AuthServer :44372 ──external OIDC "Keycloak"──► cùng realm
```

**Approach A (đã chọn):** ABP AuthServer (OpenIddict) federate Keycloak; Blazor vẫn tin AuthServer.  
**Không làm:** Approach B/C, Zimbra LDAP, `*.benhvien.vn` /etc/hosts, GitHub/CI, SLO (logout đồng bộ).

---

## 2. Quyết định đã khóa

| ID | Quyết định | Giá trị |
|----|------------|---------|
| D1 | ABP IdP | Approach A — AuthServer + external Keycloak |
| D2 | User source POC | Keycloak local users (Zimbra = Phase 2) |
| D3 | Host | `localhost` ports only |
| D4 | Roles | 4: admin / bác sĩ / lãnh đạo / nhân viên |
| D5 | KC mapping | **Groups** + claim `groups` (không realm roles). App gate: `bd-app-axis` / `bd-app-hcs` |
| D6 | Lab logout UX | `prompt=login` (ép form KC; chưa SLO) |

---

## 3. Ports & credentials

| Service | URL | Auth |
|---------|-----|------|
| Keycloak | http://localhost:5110 | admin / secret |
| Directus Studio | http://localhost:8055 | local: `admin@local.dev` / `admin123456` |
| ABP Blazor | http://localhost:44306 | local: `admin@abp.io` / `Abc@123` |
| ABP AuthServer | http://localhost:44372 | — |
| ABP WebGateway | http://localhost:44398 | — |
| ABP Identity | http://localhost:44392 | — |
| ABP Administration | http://localhost:44323 | — |

**KC clients (lab secrets):**

- `directus` / `bd-directus-lab-secret`
- `abp-auth` / `bd-abp-auth-lab-secret`

**KC test users** (password `Passw0rd!`):

| Email | Role group | App groups | Directus role UUID (v11 lab volume) | ABP role |
|-------|------------|------------|--------------------------------------|----------|
| admin@benhvien.vn | bd-admin | bd-app-axis + bd-app-hcs | `56cb789a-2e27-4bb9-b737-bf12d3923191` | admin |
| bacsi@benhvien.vn | bd-bacsi | cả 2 | `4183c3a9-2882-4714-9449-e7ba9e9c5085` | bacsi |
| lanhdao@benhvien.vn | bd-lanhdao | cả 2 | `ca499eb5-8931-4158-8c26-a49a6164302a` | lanhdao |
| nhanvien@benhvien.vn | bd-nhanvien | cả 2 | `41457ca6-1bf6-4eaf-a796-1cafafe2dfe3` | nhanvien |

**App gate:** thiếu `bd-app-axis` → Directus reject (hook); thiếu `bd-app-hcs` → AuthServer `context.Fail`.  
Default role khi *đã có* app group nhưng thiếu role group = **nhanvien**.  
Priority multi-group: **admin > lanhdao > bacsi > nhanvien**.

---

## 4. File / path quan trọng

| Mục | Path |
|-----|------|
| Compose lab (Directus v11+KC+PG+Redis) | `services/directus-main-v11/docker-compose.bd-lab.yml` |
| App gate extension | `services/directus-main-v11/bd-lab-extensions/directus-extension-bd-app-gate/` |
| Env SSO example | `services/directus-main-v11/.env.sso.example` |
| Bootstrap realm | `scripts/keycloak_bootstrap_bd_realm.py` |
| ABP Keycloak config | `services/abp-blazor/apps/auth-server/.../appsettings.Development.json` |
| ABP OIDC wire-up | `.../hanhchinhsoAuthServerModule.cs` (`AddOpenIdConnect`) |
| Group→role mapper | `.../KeycloakGroupRoleMapper.cs`, `KeycloakOpenIdConnectEvents.cs` |
| ABP role seed | `.../IdentityServiceDataSeeder.cs` |
| ABP permission seed BD | `.../AdministrationServiceDataSeeder.cs` (`SeedBdRolePermissionsAsync`) |
| Runbook | `docs/runbooks/local-sso-lab.md` |
| Brainstorm | `plans/reports/brainstorm-260723-1415-bd-sso-login-flow.md` |
| Plan | `plans/260723-1419-bd-sso-phase1/` |
| Permission seed plan | `plans/260723-1530-abp-role-permission-seed/plan.md` |
| SoT kiến trúc | `docs/workspace-architecture.md` |
| Diagram | `system-sso-guideline.png` |

---

## 5. Cold start (nhanh)

```bash
# Directus v11 + Keycloak (lab SoT)
cd services/directus-main-v11
docker compose -f docker-compose.bd-lab.yml up -d
KEYCLOAK_URL=http://127.0.0.1:5110 python3 ../../scripts/keycloak_bootstrap_bd_realm.py

# ABP infra
cd ../abp-blazor/etc/docker
docker network create hanhchinhso 2>/dev/null || true
docker compose -f containers/postgresql.yml up -d
docker compose -f containers/redis.yml up -d
docker compose -f containers/rabbitmq.yml up -d

# ABP apps (terminal riêng)
cd ../..   # services/abp-blazor
dotnet run --project services/identity/hanhchinhso.IdentityService
dotnet run --project services/administration/hanhchinhso.AdministrationService
dotnet run --project apps/auth-server/hanhchinhso.AuthServer
dotnet run --project gateways/web/hanhchinhso.WebGateway
dotnet run --project apps/blazor/hanhchinhso.Blazor
```

> Keycloak `start-dev` mất realm khi recreate container → **luôn re-run bootstrap**.

---

## 6. Đã làm kỹ thuật (checklist)

### Keycloak
- [x] Realm `bd`, RS256, login email
- [x] Groups + 4 users + clients + mapper claim `groups`
- [x] App entitlement groups `bd-app-axis` / `bd-app-hcs` (lab users có cả 2)
- [x] Hostname lab: `KC_HOSTNAME=http://localhost:5110`, bind `127.0.0.1:5110`, `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`
- [x] Directus discovery nội bộ: `http://keycloak:5110/...` (token backchannel); browser auth = `localhost:5110`

### Directus
- [x] OpenID provider `keycloak` trong `docker-compose.bd-lab.yml`
- [x] `ROLE_MAPPING` UUID 4 roles + `DEFAULT_ROLE_ID` nhanvien
- [x] Hook `bd-lab-extensions/directus-extension-bd-app-gate` — require `bd-app-axis`
- [x] `AUTH_KEYCLOAK_PARAMS: json:{"prompt":"login"}` — tránh silent SSO sau logout

### ABP
- [x] External OIDC Keycloak trên AuthServer
- [x] Map `groups` → roles Identity; seed roles `bacsi|lanhdao|nhanvien`
- [x] Gate `bd-app-hcs` trong `KeycloakOpenIdConnectEvents` (fail nếu thiếu)
- [x] Permission grants mẫu cho 3 role (admin = full)
- [x] `options.Prompt = "login"` + event redirect — cùng lý do Directus
- [x] Cert `openiddict.pfx` + `abp install-libs` (Node 24)

### Phân quyền ABP lab

| Role | Quyền |
|------|--------|
| admin | Full |
| lanhdao | Dashboard, Users/Roles view, SecurityLogs, Sessions, AuditLogs |
| bacsi | Dashboard, UserLookup, ViewDetails, AI Workspaces/Playground |
| nhanvien | Dashboard only |

---

## 7. Bài học / pitfall (đừng lặp)

1. **Không** dùng `host.docker.internal` / hostname `keycloak` trên **browser** → “This site can’t be reached” / HTTPS required.
2. Logout app **không** xóa cookie KC → phải `prompt=login` (lab) hoặc logout KC / SLO (sau).
3. Directus `ROLE_MAPPING` **bắt buộc** prefix `json:`.
4. Realm `start-dev` ephemeral → bootstrap lại sau recreate.
5. Workspace **không phải Task9** — bỏ CPD/BOKT/`[WEB]`/`[API]`/Metabase MCP.
6. Directus permissions (collection CRUD) ≠ ABP permissions (module PermissionDefinition) — map **role/group** thôi, không sync policy 1:1.

---

## 8. E2E mong đợi

1. Directus → Keycloak → login `*@benhvien.vn` → đúng role Studio.
2. Tab mới Blazor → Login → Keycloak → **có thể không hỏi password** nếu vẫn cùng session KC *và* chưa bấm flow có `prompt=login` từ đầu; với lab hiện tại mỗi lần bấm Keycloak đều hiện form (cố ý).
3. Muốn test SSO “không nhập lại MK”: tạm **tắt** `prompt=login` / dùng Incognito 1 lần login rồi mở app thứ 2 trước khi logout.

> Ghi chú: Phase 1 ưu tiên đổi user dễ test → bật `prompt=login`. SSO silent giữa 2 app vẫn đúng OIDC khi cùng cookie KC và không gửi `prompt=login`.

---

## 9. Phase 2+ (chưa làm)

- Zimbra LDAP User Federation vào Keycloak
- Host/TLS `*.benhvien.vn`
- Federated / synchronized logout (SLO)
- Full permission matrix nghiệp vụ
- GitHub / CI / remote deploy

---

## 10. Prompt mẫu cho chat mới

```
Đọc context Phase 1 BD SSO đã xong:
- docs/handoff/phase1-sso-context.md
- docs/runbooks/local-sso-lab.md
- wiki/hot.md

Workspace: bd-workspace (Directus + ABP + Keycloak), KHÔNG phải Task9.
Approach A: AuthServer federate Keycloak. Realm bd, groups bd-*.
Localhost ports. prompt=login đang bật cho lab.

Tiếp theo tôi muốn: <mô tả Phase 2 / fix / feature>
```

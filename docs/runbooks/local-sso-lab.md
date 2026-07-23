# Local SSO Lab Runbook — BD (Keycloak + Directus + ABP)

> Phase 1 POC. Hosts = localhost ports. Zimbra LDAP = later.

## Ports

| Service | URL |
|---------|-----|
| Keycloak | http://localhost:5110 (admin / secret) |
| Directus API | http://localhost:8055 |
| Directus App | http://localhost:8080 |
| ABP AuthServer | http://localhost:44372 |
| ABP Blazor | http://localhost:44306 |

## 1. Start Keycloak + bootstrap realm

```bash
cd services/directus-main
docker compose -f docker-compose.bd-lab.yml up -d
KEYCLOAK_URL=http://127.0.0.1:5110 python3 ../../scripts/keycloak_bootstrap_bd_realm.py
```

**Test users** (password `Passw0rd!`):

| Email | Role group | App groups (lab default) | App role |
|-------|------------|--------------------------|----------|
| admin@benhvien.vn | bd-admin | bd-app-axis + bd-app-hcs | admin |
| bacsi@benhvien.vn | bd-bacsi | cả 2 | bacsi |
| lanhdao@benhvien.vn | bd-lanhdao | cả 2 | lanhdao |
| nhanvien@benhvien.vn | bd-nhanvien | cả 2 | nhanvien |

**App access (entitlement):**

| Group | Ý nghĩa |
|-------|---------|
| `bd-app-axis` | Được vào Directus (hook gate) |
| `bd-app-hcs` | Được vào ABP (AuthServer fail nếu thiếu) |
| `bd-admin` / `bd-bacsi` / `bd-lanhdao` / `bd-nhanvien` | Role *trong* app |

Chỉ Directus: gán `bd-app-axis` + 1 role group, **bỏ** `bd-app-hcs`.  
Chỉ ABP: ngược lại.  
User mới không group → **không** vào app; có app group nhưng thiếu role → default **nhanvien**.

**Clients / secrets (lab):**

- `directus` / `bd-directus-lab-secret`
- `abp-auth` / `bd-abp-auth-lab-secret`

Discovery: http://localhost:5110/realms/bd/.well-known/openid-configuration

## 2. Directus OpenID (Docker — khuyến nghị lab)

Dùng **một** compose lab (đã gồm Keycloak + Directus):

```bash
cd services/directus-main
docker compose -f docker-compose.bd-lab.yml up -d
# Chờ KC sẵn sàng (~15s) rồi bootstrap realm
KEYCLOAK_URL=http://127.0.0.1:5110 python3 ../../scripts/keycloak_bootstrap_bd_realm.py
```

| | |
|--|--|
| Studio | http://localhost:8055 |
| Local admin | `admin@local.dev` / `admin123456` |
| Keycloak | **http://localhost:5110** (admin / secret) |

> Không cần `/etc/hosts`. Không mở `host.docker.internal` / `keycloak` trên browser — chỉ `localhost:5110`.

Role UUID đã map sẵn trong `docker-compose.bd-lab.yml` (`AUTH_KEYCLOAK_ROLE_MAPPING`).

Callback: `http://localhost:8055/auth/login/keycloak/callback`

### Alt: monorepo pnpm (upstream)

Xem `services/directus-main/AGENTS.md` — `pnpm install && pnpm build`, rồi `cd api && pnpm dev` (:8055).

## 3. ABP AuthServer → Keycloak (Approach A)

Keycloak lab phải đang chạy (`:5110`). Cert + `abp install-libs` trong session trước đã xong — bỏ qua nếu file còn.

### 3.1 Infra Docker (Postgres / Redis / RabbitMQ / …)

```bash
cd services/abp-blazor/etc/docker
# macOS: chạy từng file hoặc dùng pwsh
docker network create abptestwithsso 2>/dev/null || true
docker compose -f containers/postgresql.yml up -d
docker compose -f containers/redis.yml up -d
docker compose -f containers/rabbitmq.yml up -d
# (tuỳ chọn) language/audit/AI: elasticsearch, kibana, ollama, pgvector, grafana, prometheus
# Hoặc full: pwsh ./up.ps1
```

### 3.2 Chạy services (terminal riêng, thứ tự gợi ý)

```bash
cd services/abp-blazor

# Identity (seed roles bacsi/lanhdao/nhanvien)
dotnet run --project services/identity/abptestwithsso.IdentityService

# Administration
dotnet run --project services/administration/abptestwithsso.AdministrationService

# AuthServer — có nút Keycloak
dotnet run --project apps/auth-server/abptestwithsso.AuthServer

# WebGateway
dotnet run --project gateways/web/abptestwithsso.WebGateway

# Blazor UI
dotnet run --project apps/blazor/abptestwithsso.Blazor
```

| App | URL |
|-----|-----|
| Blazor | http://localhost:44306 |
| AuthServer | http://localhost:44372 |
| WebGateway | http://localhost:44398 |
| Identity | http://localhost:44392 |

Keycloak đã cấu hình sẵn trong `apps/auth-server/.../appsettings.Development.json`  
(`Authority` `http://localhost:5110/realms/bd`, client `abp-auth`).

Redirect KC: `http://localhost:44372/signin-oidc`

### 3.3 Login test

1. Mở http://localhost:44306 → Login  
2. Chọn **Keycloak**  
3. User lab: `bacsi@benhvien.vn` / `Passw0rd!` (hoặc user vừa tạo trên KC)  
4. Sau login, Identity map group `bd-*` → role ABP.

### 3.4 Phân quyền mẫu (giống Directus scope)

| Role ABP | Quyền lab |
|----------|-----------|
| `admin` | Full (seed mặc định) |
| `lanhdao` | Dashboard, xem Users/Roles, SecurityLogs, Sessions, AuditLogs |
| `bacsi` | Dashboard, UserLookup, Users.ViewDetails, AI Workspaces/Playground |
| `nhanvien` | Dashboard only |

Seed: `AdministrationServiceDataSeeder.SeedBdRolePermissionsAsync`. Chỉnh thêm: **Identity → Roles → Permissions**.

> Logout/login lại sau đổi grant (Redis có thể cache).

> Nếu thiếu service phụ (Language / Audit…), Blazor vẫn login được; một số menu/module có thể lỗi — với POC SSO chỉ cần Identity + Admin + AuthServer + Gateway + Blazor.

## 4. E2E SSO check

1. Incognito → Directus → login `bacsi@benhvien.vn` via Keycloak.
2. New tab → Blazor `:44306` → Login → Keycloak → **should not ask password**.
3. Repeat for other 3 users / roles.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Directus RS512 JWT error | Realm default alg RS256 (bootstrap sets it) |
| ROLE_MAPPING array error | Use `json:{...}` prefix |
| redirect_uri mismatch | Align KC client URIs with PUBLIC_URL / CallbackPath |
| Keycloak button missing on ABP | Check `Keycloak:Authority` + `ClientId` in Development settings |
| Logout rồi login lại vẫn user cũ (Directus/ABP) | SSO cookie KC còn sống — lab dùng `prompt=login` (Directus `AUTH_KEYCLOAK_PARAMS`, ABP AuthServer `options.Prompt`) |
| Login Directus fail / Access denied group | User thiếu `bd-app-axis` — gán group trên KC hoặc re-bootstrap |
| Login ABP Keycloak fail (entitlement) | User thiếu `bd-app-hcs` — gán group trên KC; restart AuthServer nếu vừa đổi code |
| Discovery fail Directus exit | Ensure KC up before Directus start |

## Re-bootstrap

```bash
python3 scripts/keycloak_bootstrap_bd_realm.py
```

Idempotent for groups/users/clients/mappers.

## Out of scope (phase 2+)

- Zimbra LDAP User Federation  
- `*.benhvien.vn` hosts / TLS  
- Full ABP permission matrix  
- Synchronized logout (SLO)  

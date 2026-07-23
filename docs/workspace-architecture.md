# BD Platform — Workspace Architecture

> **Mục đích:** Single source of truth cho kiến trúc lab SSO Bình Dương (Directus + ABP qua Keycloak).  
> **Cập nhật:** 2026-07-23  
> **Workspace:** `bd-workspace` (local meta folder — chưa GitHub)  
> **Diagram:** [`../system-sso-guideline.png`](../system-sso-guideline.png)

---

## 1. Tổng quan

Lab thử nghiệm **một lần đăng nhập bằng mail Zimbra** cho hai hệ thống:

| Hệ thống | Vai trò nghiệp vụ | Code |
|----------|-------------------|------|
| **Directus** | Quản lý dữ liệu lâm sàng (Clinical Data Management) | `services/directus-main/` |
| **ABP Framework** | Hành chính số — văn bản, phê duyệt | `services/abp-blazor/` |

**Keycloak** là Identity Provider trung tâm (OIDC/OAuth2).  
**Zimbra** là nguồn account + xác thực mật khẩu (LDAP/AD hoặc Zimbra Auth Token).

Giai đoạn: **local only** — chưa CI/CD, chưa remote deploy.

---

## 2. Workspace layout

```
bd-workspace/
├── services/
│   ├── directus-main/          ← Directus monorepo + docker-compose (Keycloak :5110)
│   └── abp-blazor/             ← ABP microservice template (abptestwithsso)
├── docs/                       ← Architecture & PDR
├── wiki/                       ← Second brain BD
├── plans/                      ← Agent plans (Task9 cũ = archive)
├── .claude/ / .agents/ / .opencode/  ← Agent tooling (copied, rules đã rewrite BD)
├── CLAUDE.md / AGENTS.md / SKILLS.md / README.md / llms.txt
└── system-sso-guideline.png
```

Không còn: `services/ui`, `api`, `agent`, `worker`, `n8n`, `metabase` (Task9).

---

## 3. Kiến trúc SSO

```
                    ┌──────────────────────┐
                    │   Người dùng         │
                    │   NV / BS / Lãnh đạo │
                    └──────────┬───────────┘
                               │ mở Directus hoặc ABP
                               ▼
┌──────────────┐    redirect    ┌─────────────────────────┐
│ Directus     │───────────────►│                         │
│ (OIDC client)│◄───────────────│      Keycloak           │
└──────────────┘   token        │   (SSO / IdP)           │
                                │  • User Federation      │
┌──────────────┐    redirect    │  • Roles / Groups       │
│ ABP          │───────────────►│  • OIDC tokens          │
│ (OIDC client)│◄───────────────│                         │
└──────────────┘   token        └───────────┬─────────────┘
                                            │ LDAP / Auth
                                            ▼
                                ┌─────────────────────────┐
                                │  Zimbra Mail Server     │
                                │  (LDAP / AD)            │
                                └─────────────────────────┘
```

### 3.1 Luồng đăng nhập (7 bước)

1. User truy cập Directus hoặc ABP  
2. Hệ thống redirect → Keycloak  
3. Keycloak yêu cầu xác thực qua Zimbra (email/password)  
4. Zimbra xác thực thành công → trả về Keycloak  
5. Keycloak tạo session, cấp **ID Token** + **Access Token**  
6. Redirect về hệ thống gốc kèm token  
7. Hệ thống verify token → tạo session đăng nhập local  

### 3.2 Đồng bộ user & group

```
Zimbra LDAP/AD ──User Federation──► Keycloak ──map──► Directus roles
                                         └──map──► ABP Identity roles
```

- Sync: email, tên, phòng ban, group, trạng thái active/inactive  
- Mapping: Zimbra Groups/Departments → Keycloak Roles/Groups → role nội bộ từng app  

### 3.3 Database tách biệt

| DB | Chủ | Nội dung |
|----|-----|----------|
| Keycloak DB | Keycloak | Users, groups, clients, sessions (identity) |
| Directus DB | Directus | Collections, roles, permissions (app data) |
| ABP DB(s) | ABP services | IdentityUsers, Roles, Orgs, Workflows, documents |

Mỗi app tự quản lý authorization nội bộ sau khi nhận claims từ token.

---

## 4. Thành phần kỹ thuật

### 4.1 Keycloak (local)

| Mục | Giá trị |
|-----|---------|
| Image | `quay.io/keycloak/keycloak` (trong compose Directus) |
| Port | **5110** → container 8080 |
| Admin | `admin` / `secret` |
| Mode | `start-dev` |
| File | `services/directus-main/docker-compose.yml` service `keycloak` |
| Bootstrap | `python3 scripts/keycloak_bootstrap_bd_realm.py` → realm `bd` |
| Runbook | `docs/runbooks/local-sso-lab.md` |

Lab secrets (local only): `directus`/`bd-directus-lab-secret`, `abp-auth`/`bd-abp-auth-lab-secret`.

Đã cấu hình (lab):

- Realm `bd`, RS256  
- Clients Directus + ABP  
- Protocol mapper `groups`  
- 4 users test  

Còn lại:

- User Federation (LDAP Zimbra) — phase 2  
- Directus ROLE_MAPPING UUID sau khi tạo roles trong Studio  

### 4.2 Directus

- Path: `services/directus-main/`  
- Vai trò: OIDC client của Keycloak  
- Infra debug compose: Postgres `:5100`, Redis `:5105`, Keycloak `:5110`, …  
- Env mẫu: `services/directus-main/.env.sso.example`  
- Việc cần làm: copy env SSO, tạo 4 roles Studio, điền UUID vào `ROLE_MAPPING`  

### 4.3 ABP Framework (`abptestwithsso`)

| Thành phần | Path |
|------------|------|
| AuthServer (OpenIddict) | `apps/auth-server/` |
| Blazor UI | `apps/blazor/` |
| Web Gateway | `gateways/web/` |
| Microservices | `services/` (identity, administration, audit-logging, gdpr, …) |
| Docker deps | `etc/docker/` |

Việc cần làm cho SSO BD:

- Thêm **external OpenID Connect** provider trỏ Keycloak (hoặc federation tương đương)  
- Align redirect URIs / client credentials với Keycloak client  
- Map claims → ABP roles / organization units  

Pre-req theo upstream: .NET 10+, Node 18/20, Docker, Redis; generate `openiddict.pfx`; `abp install-libs`.

---

## 5. Cấu hình chính (checklist)

### Zimbra

- [ ] Bật LDAP Service **hoặc** Zimbra Auth Token  
- [ ] Có endpoint LDAP cho Keycloak User Federation  

### Keycloak

- [ ] Tạo Realm  
- [ ] User Federation LDAP/Zimbra (hoặc user local cho POC)  
- [ ] Client Directus + Client ABP  
- [ ] Mappers email / name / groups / roles  

### Directus & ABP

- [ ] Cấu hình OIDC nhận token từ Keycloak  
- [ ] Map role Keycloak → permission nội bộ  
- [ ] Test login + SSO giữa 2 app  

---

## 6. Lợi ích mô hình (theo guideline)

- Đăng nhập một lần dùng cho cả Directus + ABP  
- Dùng sẵn account mail Zimbra  
- Quản lý user/group/role tập trung tại Keycloak  
- Mở rộng thêm hệ thống mới chỉ cần OIDC client  
- Tăng bảo mật, audit log tập trung  

---

## 7. Môi trường & Git

| Mục | Hiện tại |
|-----|----------|
| Chạy | Local Docker + process Directus/ABP |
| GitHub | **Chưa** |
| CI/CD | **Chưa** |
| Deploy remote | **Chưa** |

Khi có GitHub sau này: bổ sung mục branch strategy + CI vào doc này; không copy mù flow Task9.

---

## 8. Agent guidance

1. Đọc `CLAUDE.md` + file này trước khi đổi auth.  
2. Không tham chiếu Task9 services/ports/rules.  
3. Đổi OIDC → restart Keycloak + app client + báo user hard-refresh.  
4. Cập nhật `wiki/hot.md` khi chốt quyết định SSO đáng nhớ.  

---

## 9. Tài liệu liên quan

- [`../system-sso-guideline.png`](../system-sso-guideline.png)  
- [`project-overview-pdr.md`](./project-overview-pdr.md)  
- [`system-architecture.md`](./system-architecture.md)  
- [`../services/directus-main/readme.md`](../services/directus-main/readme.md)  
- [`../services/abp-blazor/README.md`](../services/abp-blazor/README.md)  

# BD Platform — Workspace Architecture

> **Mục đích:** Single source of truth cho kiến trúc lab SSO Bình Dương (Directus + HCS Community qua Keycloak).
> **Cập nhật:** 2026-08-10
> **Workspace:** `bd-workspace` (local meta folder — chưa GitHub)  
> **Diagram:** [`../system-sso-guideline.png`](../system-sso-guideline.png)

---

## 1. Tổng quan

Lab thử nghiệm **một lần đăng nhập bằng mail Zimbra** cho hai hệ thống:

|| Hệ thống | Vai trò nghiệp vụ | Code |
|----------|-------------------|------|
|| **Directus** | Quản lý dữ liệu lâm sàng (Clinical Data Management) | `services/directus-main-v11/` (lab SoT; v12 `directus-main` = archive) |
|| **HCS Community** | Hành chính số — văn bản, công việc, trao đổi | `services/HCS_web_free_license/` (runtime mặc định) |

**Keycloak** là Identity Provider trung tâm (OIDC/OAuth2).  
**Zimbra** là nguồn account + xác thực mật khẩu (LDAP/AD hoặc Zimbra Auth Token).

Giai đoạn: **local only** — chưa CI/CD, chưa remote deploy.

---

## 2. Workspace layout

```
bd-workspace/
├── services/
│   ├── directus-main-v11/      ← Lab SoT Directus (v11) + docker-compose.bd-lab (Keycloak :5110)
│   ├── directus-main/          ← Archive v12 (MSCL) — không dùng cho SSO lab
│   ├── HCS_web_free_license/   ← HCS Community runtime (Blazor + BFF + AuthServer)
│   └── abp-blazor/             ← ABP microservice template lịch sử (hanhchinhso)
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
                               │ mở Directus hoặc HCS
                               ▼
┌──────────────┐    redirect    ┌─────────────────────────┐
│ Directus     │───────────────►│                         │
│ (OIDC client)│◄───────────────│      Keycloak           │
└──────────────┘   token        │   (SSO / IdP)           │
                                │  • User Federation      │
┌──────────────┐    external    │  • Roles / Groups       │
│ HCS AuthServer│──────────────►│  • OIDC tokens          │
└──────┬───────┘    OIDC        └───────────┬─────────────┘
       │ authority                            │ LDAP / Auth
       ▼                                      ▼
┌──────────────┐                    ┌─────────────────────────┐
│ HCS Gateway  │                    │  Zimbra Mail Server     │
│ + Blazor UI  │                    │  (LDAP / AD)            │
└──────────────┘                    └─────────────────────────┘
```

### 3.1 Luồng đăng nhập HCS (8 bước)

1. User truy cập HCS root hoặc một HTTPS deep link.
2. Blazor route yêu cầu xác thực và chuyển browser tới Gateway `/bff/login`.
3. Gateway khởi tạo OIDC challenge tới HCS AuthServer.
4. AuthServer dùng Keycloak làm external OIDC provider.
5. Keycloak yêu cầu xác thực qua Zimbra (khi User Federation đã được cấu hình) và tạo SSO session.
6. Callback trả về AuthServer, rồi Gateway tạo BFF session cookie HTTP-only.
7. Gateway chỉ chấp nhận return URL có origin đã cấu hình; URL ngoài origin quay về UI origin mặc định.
8. Browser trở về deep link ban đầu; authorization nội bộ vẫn kiểm tra policy/quyền của route.

### 3.2 Đồng bộ user & group

```
Zimbra LDAP/AD ──User Federation──► Keycloak ──map──► Directus roles
                                         └──map──► HCS roles / permissions
```

- Sync: email, tên, phòng ban, group, trạng thái active/inactive  
- Mapping: Zimbra Groups/Departments → Keycloak Roles/Groups → role nội bộ từng app  

### 3.3 Database tách biệt

|| DB | Chủ | Nội dung |
||----|-----|----------|
|| Keycloak DB | Keycloak | Users, groups, clients, sessions (identity) |
|| Directus DB | Directus | Collections, roles, permissions (app data) |
|| hanhchinhso_Identity | Identity Service | IdentityUsers, Roles, Orgs |
|| hanhchinhso_Administration | Administration Service | Permissions, tenants, audit |
|| hanhchinhso_Workflow | WorkflowService | Elsa 3.5 workflow definitions, instances, execution logs |
|| ABP other DBs | Other services | Language, AI, GDPR, … |

Mỗi app tự quản lý authorization nội bộ sau khi nhận claims từ token.

---

## 4. Thành phần kỹ thuật

### 4.1 Keycloak (local)

|| Mục | Giá trị |
||-----|---------|
|| Image | `quay.io/keycloak/keycloak` (trong compose Directus) |
|| Port | **5110** → container 8080 |
|| Admin | `admin` / `secret` |
|| Mode | `start-dev` |
|| File | `services/directus-main-v11/docker-compose.bd-lab.yml` service `keycloak` |
|| Bootstrap | `python3 scripts/keycloak_bootstrap_bd_realm.py` → realm `bd` |
|| Runbook | `docs/runbooks/local-sso-lab.md` |

Thông tin xác thực lab được quản lý ngoài tài liệu và không được commit.

Đã cấu hình (lab):

- Realm `bd`, RS256  
- Clients Directus + HCS AuthServer
- Protocol mapper `groups`  
- 4 users test  

Còn lại:

- User Federation (LDAP Zimbra) — phase 2  

### 4.2 Directus

- Path (lab SoT): `services/directus-main-v11/` (11.13.4 — không runtime license SSO gate)
- Archive: `services/directus-main/` (v12 / MSCL — không chạy lab)
- Vai trò: OIDC client của Keycloak  
- Compose lab: `docker-compose.bd-lab.yml` (PG `:5120`, Redis `:5121`, Keycloak `:5110`, Axis `:8055`)
- Env mẫu: `services/directus-main-v11/.env.sso.example`  
- App gate: `bd-lab-extensions/directus-extension-bd-app-gate` (`bd-app-axis`)
- ROLE_MAPPING UUID đã fill trong compose lab sau bootstrap roles Studio 

### 4.3 HCS Community (`HCS_web_free_license`)

|| Thành phần | Path | Port |
|------------|------|------|
|| AuthServer | `apps/auth-server/HCS.AuthServer/` | **44401** |
|| Web Gateway / BFF | `gateways/web/HCS.WebGateway/` | **44402** |
|| Blazor UI | `src/HCS.Blazor/` + `src/HCS.Blazor.Client/` | **44403** |
|| Domain services | `services/` | Platform **44411**, Organization **44412**, Document **44413**, Work Management **44414**, Collaboration **44415** |
|| Docker runtime | `docker-compose.yml` | Browser `https://hcs.localhost` |

- `/` requires authentication; `/login` is anonymous only to start the same BFF flow.
- The sole HCS-specific main-menu item is Chat (`/chat`), protected by `Collaboration.Chat`; standard Administration entries remain permission-driven.
- Docker Compose is the default runtime. See [`runbooks/hcs-docker-compose-handoff.md`](./runbooks/hcs-docker-compose-handoff.md) for safe startup and rollback.

---

## 5. Cấu hình chính (checklist)

### Zimbra

- [ ] Bật LDAP Service **hoặc** Zimbra Auth Token  
- [ ] Có endpoint LDAP cho Keycloak User Federation  

### Keycloak

- [ ] Tạo Realm  
- [ ] User Federation LDAP/Zimbra (hoặc user local cho POC)  
- [ ] Clients Directus + HCS AuthServer
- [ ] Mappers email / name / groups / roles  

### Directus & HCS

- [ ] Directus OIDC và HCS AuthServer external OIDC nhận token từ Keycloak
- [ ] BFF return URL chỉ chấp nhận origin UI đã cấu hình
- [ ] Map role Keycloak → permission nội bộ  
- [ ] Test login + SSO giữa 2 app  

---

## 6. Lợi ích mô hình (theo guideline)

- Đăng nhập một lần dùng cho cả Directus + HCS
- Dùng sẵn account mail Zimbra  
- Quản lý user/group/role tập trung tại Keycloak  
- Mở rộng thêm hệ thống mới chỉ cần OIDC client  
- Tăng bảo mật, audit log tập trung  

---

## 7. Môi trường & Git

|| Mục | Hiện tại |
||-----|----------|
|| Chạy | Local Docker + process Directus/HCS |
|| GitHub | **Chưa** |
|| CI/CD | **Chưa** |
|| Deploy remote | **Chưa** |

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

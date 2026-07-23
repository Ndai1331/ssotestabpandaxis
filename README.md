# BD Workspace — Bình Dương SSO Lab

Workspace thử nghiệm **SSO đăng nhập bằng mail Zimbra** cho 2 hệ thống (**Directus** + **ABP Framework**) qua **Keycloak**.

> **Repo:** https://github.com/Ndai1331/ssotestabpandaxis  
> **Giai đoạn:** local lab + GitHub source. Chưa CI/CD / remote deploy.  
> Diagram: [`system-sso-guideline.png`](./system-sso-guideline.png)

## Mục tiêu

Một tài khoản Zimbra → đăng nhập được cả Directus (quản lý dữ liệu lâm sàng) và ABP (hành chính số) qua Keycloak (OIDC/OAuth2).

## Quick Start (local)

```bash
# 1. Keycloak + infra Directus (Postgres, Redis, …)
cd services/directus-main
docker compose up -d keycloak postgres redis

# Keycloak admin UI: http://localhost:5110
# Admin: admin / secret

# 2. Directus (xem readme trong services/directus-main)
# 3. ABP (infra + apps — xem services/abp-blazor/README.md)
cd ../abp-blazor/etc/docker
# chạy up.ps1 / docker compose theo README ABP
```

Chi tiết: [`docs/workspace-architecture.md`](./docs/workspace-architecture.md)

## Cấu trúc

```
bd-workspace/                          ← Meta workspace (docs + agent rules)
│
├── services/
│   ├── directus-main/                 ← Directus (Clinical Data Management)
│   │                                   + docker-compose có Keycloak :5110
│   └── abp-blazor/                    ← ABP microservice (Digital Administration)
│                                       AuthServer OpenIddict + Blazor UI
│
├── docs/                              ← Kiến trúc & PDR
│   └── workspace-architecture.md      ← Single source of truth
│
├── wiki/                              ← Second brain (BD knowledge)
├── plans/                             ← Kế hoạch / báo cáo agent
├── system-sso-guideline.png           ← Mô hình SSO Zimbra → Keycloak → apps
│
├── CLAUDE.md                          ← Quy tắc AI agent (Tiếng Việt)
├── AGENTS.md                          ← OpenCode / ClaudeKit guidance
├── SKILLS.md                          ← Skill routing & quy trình local
└── README.md                          ← This file
```

## Services

| Service | Path | Tech | Vai trò SSO | Local notes |
|---------|------|------|-------------|-------------|
| **Keycloak** | (Docker trong `directus-main`) | Keycloak | IdP trung tâm — OIDC tokens | `:5110` |
| **Directus** | `services/directus-main/` | Node.js / Directus | OIDC client → Keycloak | Studio + API |
| **ABP** | `services/abp-blazor/` | .NET 10 / ABP microservice | OIDC client → Keycloak (qua AuthServer/OpenIddict hoặc external IdP) | Blazor + gateways + microservices |

## Kiến trúc SSO (tóm tắt)

```
Người dùng (NV / BS / Lãnh đạo)
        │
        ▼
┌───────────────┐     LDAP / Auth      ┌────────────────────┐
│ Zimbra Mail   │◄────────────────────►│ Keycloak (SSO IdP) │
│ (LDAP/AD)     │   User Federation    │ Realm + Clients    │
└───────────────┘                      └─────┬──────┬───────┘
                                             │ OIDC │
                              ┌──────────────┘      └──────────────┐
                              ▼                                    ▼
                     ┌─────────────────┐                  ┌─────────────────┐
                     │ Directus        │                  │ ABP Framework   │
                     │ (Clinical Data) │                  │ (Digital Admin) │
                     └─────────────────┘                  └─────────────────┘
```

Luồng đăng nhập: App → Keycloak → Zimbra (xác thực) → Keycloak cấp token → App tạo session.

## Cho AI Agents

Đọc theo thứ tự:

1. [`CLAUDE.md`](./CLAUDE.md) — quy tắc bắt buộc
2. [`docs/workspace-architecture.md`](./docs/workspace-architecture.md) — kiến trúc
3. [`wiki/hot.md`](./wiki/hot.md) — cache facts gần nhất
4. [`SKILLS.md`](./SKILLS.md) — skill routing

## Lưu ý quan trọng

- **Không** còn stack Task9 (ui/api/agent/worker/n8n/metabase…). File `plans/`, wiki cũ Task9 nếu còn là **archive** — không dùng làm nguồn sự thật cho BD.
- Chưa có remote GitHub → không deploy, không GHA, không force-push policy remote.
- Ưu tiên thử nghiệm SSO local: Keycloak realm/clients + OIDC mapping Directus/ABP.

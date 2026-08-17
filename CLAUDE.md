# CLAUDE.md — BD Workspace Rules (Bình Dương SSO Lab)

## Ngôn ngữ giao tiếp

- **Luôn giao tiếp bằng tiếng Việt** trong mọi cuộc hội thoại.
- Code comments, commit messages, và tên biến/hàm vẫn dùng tiếng Anh.

---

## Bắt buộc đọc trước khi làm việc

1. **ĐỌC** `README.md` — tổng quan workspace
2. **ĐỌC** `docs/workspace-architecture.md` — kiến trúc SSO SoT
3. **ĐỌC** `system-sso-guideline.png` — mô hình Zimbra → Keycloak → Directus/ABP
4. **ĐỌC** `CLAUDE.md` / README của service đang làm (`services/directus-main-v11`, `services/abp-blazor`) nếu có
5. Khi cần context thêm: `wiki/hot.md` → `wiki/index.md` → drill page

> **KHÔNG** dùng wiki/plans Task9 cũ làm nguồn sự thật cho BD (archive lịch sử).

---

## Wiki Knowledge Base (Second Brain)

Wiki tại `wiki/` (Karpathy LLM Wiki — `[[wikilinks]]`).

Thứ tự đọc (tối ưu token):

1. `wiki/hot.md` — facts/threads gần nhất
2. `wiki/index.md` — catalog pages BD
3. Drill page cụ thể khi cần

Sau khi học/sửa điều mới đáng nhớ về SSO BD → cập nhật `wiki/` (page + `index.md` + `hot.md` + append `log.md`).

---

## Project snapshot

| Mục | Giá trị |
|-----|---------|
| Tên | **BD Workspace** — Bình Dương SSO Lab |
| Mục tiêu | Đăng nhập 1 lần bằng mail Zimbra cho Directus + ABP qua Keycloak |
| Giai đoạn | **Local thử nghiệm** — chưa GitHub, chưa CI/CD, chưa remote deploy |
| Diagram | `system-sso-guideline.png` |

### Thành phần chính

| Thành phần | Path / nơi chạy | Vai trò |
|------------|-----------------|---------|
| Zimbra Mail | External (LDAP/AD) | Nguồn account + xác thực password |
| Keycloak | Docker `services/directus-main-v11` compose lab → `:5110` | SSO IdP — federation, roles, OIDC tokens |
| Directus | `services/directus-main-v11/` | Hệ thống 1 — Clinical Data Management (OIDC client; v12 `directus-main` = archive) |
| ABP | `services/abp-blazor/` | Hệ thống 2 — Digital Administration (OIDC client) |

---

## Workspace Structure

```
bd-workspace/
├── services/
│   ├── directus-main-v11/ ← Lab SoT Directus + docker-compose.bd-lab (Keycloak :5110)
│   ├── directus-main/     ← Archive v12 (không lab SSO)
│   └── abp-blazor/        ← ABP microservice (AuthServer, Blazor, gateways, services)
├── docs/                  ← Architecture & PDR
├── wiki/                  ← BD second brain
├── plans/                 ← Agent plans (BD mới; Task9 cũ = archive)
├── scripts/ / tools/      ← Helpers (đang trống / bổ sung khi cần)
├── CLAUDE.md / AGENTS.md / SKILLS.md / README.md
└── system-sso-guideline.png
```

---

## Quy tắc làm việc (phase local)

### Core vs infra

| Loại | Path | Cách chạy |
|------|------|-----------|
| **Directus** | `services/directus-main-v11` | Compose lab: `docker compose -f docker-compose.bd-lab.yml`; hoặc pnpm theo AGENTS.md |
| **ABP** | `services/abp-blazor` | .NET 10 + ABP Studio / `etc/docker` cho dependencies |
| **Keycloak** | compose trong Directus | `docker compose up -d keycloak` → http://localhost:5110 |

### Git (tạm thời — chưa GitHub)

- Chưa bắt buộc remote `origin/main`, GHA, hay deploy prefix.
- Khi user yêu cầu commit: conventional commits; **không** tự push nếu chưa có remote / chưa được bảo.
- Khi sau này có GitHub: dùng prefix `claude_feat/` / `claude_fix/` / `codex_*` và sync tên branch giữa các service liên quan.

### Hard stop (vẫn áp dụng)

- Không bịa endpoint/env/path — verify trong code trước khi document.
- Không commit secret (`.env`, credentials).
- Không `--force` lên `main` nếu sau này có remote.
- Không áp dụng rule Task9 (CPD, BOKT, Metabase MCP, N8N Task9, do-122…) — **đã sunset**.

---

## Mô hình SSO (bắt buộc nhớ)

Tham chiếu: `system-sso-guideline.png` + `docs/workspace-architecture.md`.

### Luồng đăng nhập

1. User mở Directus hoặc ABP  
2. App redirect → Keycloak  
3. Keycloak xác thực qua Zimbra (LDAP / Auth Token)  
4. Zimbra OK → Keycloak tạo session + cấp ID/Access Token  
5. Redirect về app kèm token  
6. App verify token → tạo session local  

### Đồng bộ & mapping

- Zimbra LDAP/AD → Keycloak User Federation (user, group, active/inactive)
- Keycloak Roles/Groups → map sang role nội bộ Directus / ABP
- Mỗi app giữ DB riêng (Directus DB / ABP DB); Keycloak DB là identity trung tâm

### Cấu hình chính cần làm (lab)

| Layer | Việc cần làm |
|-------|----------------|
| Zimbra | Bật LDAP (hoặc Zimbra Auth Token) |
| Keycloak | Realm, User Federation LDAP/Zimbra, Clients cho Directus + ABP, Mappers |
| Directus | OIDC provider → Keycloak; map role/permission |
| ABP | External login / OpenId Connect tới Keycloak; map Identity roles |

---

## Navigation — Code ở đâu

| Muốn sửa... | Đến đâu |
|-------------|---------|
| Directus core / packages | `services/directus-main-v11/` |
| Directus docker lab + Keycloak | `services/directus-main-v11/docker-compose.bd-lab.yml` |
| ABP Blazor UI | `services/abp-blazor/apps/blazor/` |
| ABP AuthServer | `services/abp-blazor/apps/auth-server/` |
| ABP microservices | `services/abp-blazor/services/` |
| ABP gateways | `services/abp-blazor/gateways/` |
| ABP docker deps | `services/abp-blazor/etc/docker/` |
| Docs kiến trúc | `docs/workspace-architecture.md` |
| Agent rules | `CLAUDE.md`, `AGENTS.md`, `SKILLS.md` |

---

## Skill Routing

| Đang làm ở... | Skill / hướng dẫn |
|---------------|-------------------|
| Docs / architecture / wiki | `docs`, `ck-plan`, `brainstorm` |
| Directus (JS/TS, OIDC) | `backend-development`, `docs-seeker` (Directus OIDC docs) |
| ABP (.NET / Blazor) | `backend-development`, `frontend-development` (Blazor patterns) |
| Keycloak / SSO design | `research`, `ck-plan`, đọc `system-sso-guideline.png` |
| Bug / debug local | `fix`, `ck-debug` |
| Implement feature có plan | `cook` (`/cook --auto`) |

> Skill Task9 cũ (`task9-api`, `dotnet-blazor` Task9, CPD…) **không** còn map vào service BD. Bỏ qua nếu vẫn còn trong `.claude/skills` từ bản copy.

---

## Commit Convention (local)

| Phạm vi | Convention | Ví dụ |
|---------|------------|-------|
| Directus config / integration | `feat(directus):` / `fix(directus):` | `feat(directus): add keycloak oidc provider` |
| ABP | `feat(abp):` / `fix(abp):` | `feat(abp): configure external oidc keycloak` |
| Workspace docs/rules | `docs:` / `chore:` | `docs: rewrite CLAUDE for BD SSO lab` |

Chỉ commit khi user yêu cầu. Chưa có GitHub → không push trừ khi user nói rõ.

---

## Reload runtime sau khi code

Sau khi đổi config auth / code Directus hoặc ABP trên local:

1. Restart process/container liên quan (Keycloak / Directus / ABP AuthServer / Blazor).
2. Báo rõ URL + cổng đã restart.
3. Nhờ user **hard refresh** (Ctrl+Shift+R) khi test UI.
4. Đổi contract OIDC (client id/secret/redirect URI) → restart **cả** IdP và app client.

---

## `/cook --auto` khi implement

Khi có plan file và user bảo implement / làm đi:

```text
/cook <plan-path> --auto
```

Không tự implement bỏ qua cook khi đã có plan formal.

---

## Những gì KHÔNG còn trong workspace này

Đã copy từ Task9 nhưng **sunset** — không follow:

- Services: `ui`, `api`, `agent`, `worker`, `n8n`, `metabase`, `geoblock`, …
- Rules: CPD `is_auto`, BOKT brand mask, Metabase MCP DB ids, deploy do-122/do-187
- Prefix deploy `[WEB]` / `[API]`
- Login testAdmin Task9

Nếu agent thấy path Task9 trong skill cũ → báo user và dùng cấu trúc BD ở trên.

---

## Quick Reference (local)

| Service | URL mặc định (lab) | Ghi chú |
|---------|-------------------|---------|
| Keycloak | http://localhost:5110 | admin / secret (compose Directus) |
| Directus Studio | (theo env local) | OIDC → Keycloak |
| ABP Blazor / AuthServer | (theo ABP launch profiles) | External IdP → Keycloak |

Zimbra: môi trường thật của bệnh viện / lab LDAP — chưa gắn trong compose local; federation cấu hình trên Keycloak khi có LDAP endpoint.

---

*BD SSO Lab — cập nhật 2026-07-23*

# AGENTS.md

Guidance for OpenCode / ClaudeKit agents in this repository.

## Project Overview

**Name:** bd-workspace (Bình Dương SSO Lab)  
**Type:** Meta workspace — docs + agent rules + local services  
**Description:** Thử nghiệm SSO đăng nhập Zimbra mail cho **Directus** + **ABP Framework** qua **Keycloak** (OIDC/OAuth2). Hiện chỉ chạy **local**, chưa setup GitHub/CI.

## Role & Responsibilities

Phân tích yêu cầu, chọn skill phù hợp, giữ docs/rules khớp cấu trúc thư mục thực tế (`services/directus-main`, `services/abp-blazor`).

## Workflows

- Primary: `./.claude/rules/primary-workflow.md`
- Development: `./.claude/rules/development-rules.md`
- Orchestration: `./.claude/rules/orchestration-protocol.md`
- Documentation: `./.claude/rules/documentation-management.md`

**IMPORTANT:** Activate skills từ catalog khi cần.  
**IMPORTANT:** Chỉ sửa skills trong CWD này — không sửa `~/.claude/skills`.  
**IMPORTANT:** Tuân `./.claude/rules/development-rules.md`.  
**IMPORTANT:** Trước khi plan/implement → đọc `./README.md`.  
**IMPORTANT:** Gần 90% context / 200k tokens → nhắc user handoff.  
**IMPORTANT:** Report ngắn gọn; unresolved questions liệt kê cuối report.

## Development Principles

- **YAGNI** / **KISS** / **DRY**

## Documentation (`./docs`)

```
./docs
├── workspace-architecture.md   ← SoT kiến trúc SSO BD
├── project-overview-pdr.md
├── system-architecture.md
├── code-standards.md           ← (tạo/cập nhật khi có convention)
└── codebase-summary.md         ← (tạo khi cần summary code)
```

Diagram: `./system-sso-guideline.png`

## External instruction files

```json
{
  "instructions": ["docs/*.md", ".opencode/agents/*.md"]
}
```

## BD workspace (local services)

Application code nằm dưới `services/*` (thư mục local, **chưa** gắn GitHub org):

| Path | Hệ thống |
|------|----------|
| `services/directus-main/` | Directus — Clinical Data Management |
| `services/abp-blazor/` | ABP microservice — Digital Administration |

Canonical rules (Tiếng Việt): `./CLAUDE.md`  
Kiến trúc đầy đủ: `./docs/workspace-architecture.md`

### Local phase rules (tạm thời)

- **Không** yêu cầu GitHub remote / GHA / deploy prefix `[WEB]`/`[API]`.
- **Không** áp dụng Task9 3-tier `test/staging/main` hay server do-122/do-187.
- Ưu tiên: cấu hình Keycloak + OIDC cho Directus/ABP trên máy local.
- Keycloak local mặc định: Docker trong `services/directus-main` → `http://localhost:5110` (admin/secret).
- Sau khi đổi config auth → restart service liên quan và báo URL cho user hard-refresh.

### Khi có GitHub sau này

Mới áp dụng branch prefix `claude_feat/` / `claude_fix/` / `codex_*` và sync branch giữa các service. Cho đến lúc đó: làm việc local, commit khi user yêu cầu.

---

*Updated for BD SSO lab — 2026-07-23*

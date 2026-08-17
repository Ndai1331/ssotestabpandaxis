---
type: decision
title: "Submodule Worktree Split"
created: 2026-06-27
updated: 2026-06-27
tags:
  - git
  - workflow
  - submodule
status: active
decision_date: 2026-06-13
related:
  - "[[Git Flow 3 Tầng]]"
  - "[[Task9 Platform Overview]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Decision: Submodule Worktree Split

**Quyết định:** Chỉ **Core services** (`ui`, `api`) dùng git worktree riêng per feature; **Worker services** thì không.

## Phân loại

| Loại | Submodules | Worktree | launch.json | Cách chạy |
|------|-----------|----------|-------------|-----------|
| **Core** | `ui`, `api` | ✅ riêng + branch riêng, cắt từ `origin/main` | ✅ | `dotnet run` / Claude preview |
| **Worker** | `agent`, `worker`, `geoblock`, `metabase`, `ahrefs-mcp`, `worker-lite`, `worker-lite-cpd`, `similarweb-worker`, `ncc-worker` | ❌ | ❌ | `docker compose up <svc>` |

## Lý do

- Core (`.NET`) cần dev nhiều feature song song không xung đột → worktree + branch tạm `claude_feat/<slug>` cắt từ `origin/main` mới nhất (tự động qua SessionStart hook).
- Worker chạy độc lập qua Docker, release theo image tag riêng → worktree thêm overhead không cần thiet.

## Cơ chế tự động (hook)

Trong app worktree (`.claude/worktrees/<name>`):
- `worktree-launch-config.cjs` (SessionStart) dựng `services/ui` + `services/api` thành worktree riêng trên branch tạm cắt từ `origin/main`.
- `worktree-branch-rename.cjs` (prompt đầu) suy slug → rename branch `claude_feat/<slug>`.
- Worker: hook bỏ qua (placeholder rỗng).

Dọn worktree: `.claude/scripts/worktree-cleanup.sh <name|all>` (gỡ cả submodule worktree).

## Hệ quả

- Mỗi service commit **riêng biệt** (KHÔNG cross-repo commit) — xem [[Git Flow 3 Tầng]].
- Feature chạm nhiều service → tất cả PHẢI cùng tên branch có prefix agent (`claude_*`/`codex_*`).

---
description: Tạo isolated task workspace cho BD (local) — tùy chọn
---

# Task Start — BD Workspace

> Phase local: isolation `.tasks/` là **optional**. Nhiều việc SSO làm thẳng trên `services/*`.

## Constants

```
WORKSPACE_ROOT="/Users/user/Documents/bd-workspace"
TASKS_DIR="$WORKSPACE_ROOT/.tasks"
TASKS_JSON="$TASKS_DIR/.tasks.json"
TASK_DIR="$TASKS_DIR/<TASK_SLUG>"
```

## Services hợp lệ

| Slug | Path |
|------|------|
| `directus` | `services/directus-main` |
| `abp` | `services/abp-blazor` |

**Không** dùng `ui` / `api` (Task9 — không tồn tại).

## Phase 1

```bash
mkdir -p "$WORKSPACE_ROOT/.tasks"
```

Nếu `$TASK_DIR` đã tồn tại → hỏi user.

## Phase 2 (optional clone)

Chỉ clone/copy khi user cần isolation mạnh. Mặc định: làm việc tại workspace root.

Ghi `.task-context.json`:

```json
{
  "slug": "<TASK_SLUG>",
  "workspace_root": "/Users/user/Documents/bd-workspace",
  "services": ["directus", "abp"],
  "phase": "local-sso",
  "github": false
}
```

## Next

- Cập nhật `wiki/hot.md` nếu task dài ngày  
- Handoff: `/handoff` workflow  

---
description: Hand off current work to Codex agent — tạo handoff doc (giống /handoff) rồi spawn Codex để tiếp tục, giảm token Antigravity
---

# Codex Handoff Workflow

Tạo handoff document (cùng format & location với `/handoff`) rồi spawn Codex tự động tiếp tục pending work.

> **Mục đích**: Antigravity viết handoff → thoát session → Codex tiếp quản. Giảm token.

---

## Steps

### Step 1 — Antigravity tạo Handoff Document

1. Tạo thư mục nếu chưa có:
```bash
mkdir -p /Users/user/Documents/bd-workspace/docs/handoff
```

2. Antigravity tự viết handoff doc theo template chuẩn vào:
```
docs/handoff/handoff-{YYYYMMDD-HHMM}.md
```

**Template chuẩn** (giống `/handoff`):
```markdown
# Handoff Document

**Created:** {YYYY-MM-DD HH:MM}
**Topic:** {topic}
**Status:** In Progress / Paused

## Context Summary
{Brief description của session hiện tại}

## Completed Work
- [x] ...

## Pending Work
- [ ] ...

## Key Decisions Made
| Decision | Rationale |
|----------|-----------|
| ...      | ...       |

## Current State
### Files Modified
- ...

### Files Created
- ...

### Dependencies/Blockers
- ...

## Next Steps
1. ...

## Important Notes
{Thông tin quan trọng cho session tiếp theo}
```

> ⚠️ Antigravity LUÔN lưu handoff ở `docs/handoff/` (workspace root) — không lưu trong `/tmp/`

---

### Step 2 — Spawn Codex với handoff vừa tạo

// turbo
3. Xác nhận file handoff vừa tạo:
```bash
ls -t /Users/user/Documents/bd-workspace/docs/handoff/handoff-*.md | head -3
```

4. Spawn Codex full-auto với nội dung handoff:
```bash
HANDOFF_FILE=$(ls -t /Users/user/Documents/bd-workspace/docs/handoff/handoff-*.md | head -1)
codex exec --full-auto \
  -C /Users/user/Documents/bd-workspace \
  -o /Users/user/Documents/bd-workspace/docs/handoff/codex-result-$(date +%Y%m%d-%H%M).md \
  "You are picking up work handed off from Antigravity AI agent.

$(cat "$HANDOFF_FILE")

Your job:
1. Read Pending Work and Next Steps carefully
2. Implement each pending item — minimal, careful changes
3. Follow existing code patterns in the project
4. Use conventional commits (feat:, fix:, chore:, etc.)
5. Write a brief completion summary"
```

> Codex result được lưu vào `docs/handoff/codex-result-YYYYMMDD-HHMM.md` — cùng chỗ với handoff doc, không mất sau session.

---

### Step 3 — (Optional) Xem kết quả Codex

// turbo
5. Đọc kết quả sau khi Codex xong:
```bash
cat "$(ls -t /Users/user/Documents/bd-workspace/docs/handoff/codex-result-*.md | head -1)"
```

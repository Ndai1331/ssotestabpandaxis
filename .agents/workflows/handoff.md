---
description: Create handoff document for BD SSO lab
---

# Handoff — BD Workspace

Tạo handoff document để nối tiếp hội thoại / agent khác.

## Process

1. Review conversation, todos, files đã sửa (`services/directus-main`, `services/abp-blazor`, `docs/`, `wiki/`).
2. Tạo thư mục nếu cần: `docs/handoff/`
3. Ghi `docs/handoff/handoff-{YYYYMMDD-HHMM}.md`

## Template

```markdown
# Handoff Document

**Created:** {YYYY-MM-DD HH:MM}
**Project:** BD SSO Lab (local)
**Topic:** {topic}
**Status:** {In Progress / Paused / Blocked}

## Context Summary
{Brief}

## Completed
- [x] …

## Pending
- [ ] …

## Key Decisions
| Decision | Rationale |
|----------|-----------|

## Current State
### Files Modified
### Keycloak / OIDC notes
### Blockers

## Next Steps
1. …

## Resume
- Đọc `CLAUDE.md` + `docs/workspace-architecture.md` + `wiki/hot.md`
- Services: `directus-main`, `abp-blazor`
- Keycloak local: http://localhost:5110
```

## Notes
- Workspace: `/Users/user/Documents/bd-workspace` (không còn path task9-workspace).
- Chưa GitHub → không ghi remote branch bắt buộc; ghi rõ local state.

---
description: Resume work từ handoff document mới nhất — Codex tự động pick up pending items và tiếp tục
---

# Back2Work Workflow

[SKILL] Activating: acpx-codex — Reason: Back2work via Codex with latest handoff context

## Steps

// turbo
1. Xem danh sách handoff gần nhất:
```bash
ls -t /Users/user/Documents/bd-workspace/docs/handoff/handoff-*.md | head -5
```

// turbo
2. Đọc handoff mới nhất để hiểu context:
```bash
cat "$(ls -t /Users/user/Documents/bd-workspace/docs/handoff/handoff-*.md | head -1)"
```

3. Spawn Codex ở back2work mode — nhanh, tập trung vào pending items:
```bash
HANDOFF=$(cat "$(ls -t /Users/user/Documents/bd-workspace/docs/handoff/handoff-*.md | head -1)") && codex exec --full-auto -C /Users/user/Documents/bd-workspace "Back2Work: Resume project work. LATEST HANDOFF: $HANDOFF Focus only on PENDING items (skip [x]). Work file by file. Commit each logical unit. Use conventional commits. Write completion summary at end."
```

4. Đọc kết quả Codex (nếu có output file):
```bash
ls -t /Users/user/Documents/bd-workspace/docs/handoff/codex-result-*.md 2>/dev/null | head -3 || echo "No codex-result files yet"
```

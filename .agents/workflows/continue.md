---
description: Resume work from a handoff document
---

# Continue — BD Workspace

Resume work từ handoff. **Task isolation aware** — tự detect task context.

> Override global `/continue`. File này trong `.agents/workflows/` → clone theo task.

---

## Workflow

// turbo-all

### Step 1: Find Handoff

**Nếu argument:**
```bash
cat /Users/user/Documents/bd-workspace/docs/handoff/{argument}
```

**Nếu không có argument:**
```bash
ls -lt /Users/user/Documents/bd-workspace/docs/handoff/handoff-*.md | head -5
```

> Lấy file mới nhất. Nếu không có → "Không tìm thấy handoff. Dùng `/handoff` để tạo."

---

### Step 2: Parse Handoff

Extract:
- Topic, Status, Completed/Pending work, Next steps
- **Task Isolation section** (nếu có)

---

### Step 3: Detect Task Context

Nếu handoff có section `🔀 Task Isolation`:

1. Đọc task name → check `.tasks/<task>/.task-context.json`:
```bash
TASK_NAME="<task from handoff>"
TASK_CTX="/Users/user/Documents/bd-workspace/.tasks/$TASK_NAME/.task-context.json"
if [ -f "$TASK_CTX" ]; then
  echo "✅ Task workspace found"
  cat "$TASK_CTX"
else
  echo "❌ Task workspace not found — may have been cleaned up"
fi
```

2. Verify branches:
```bash
TASK_DIR="/Users/user/Documents/bd-workspace/.tasks/$TASK_NAME"
for s in services/directus-main services/abp-blazor; do
  if [ -d "$TASK_DIR/$s" ]; then
    echo "📦 $s: $(cd "$TASK_DIR/$s" && git branch --show-current 2>/dev/null || echo local)"
    echo "   Changes: $(cd "$TASK_DIR/$s" && git status --short 2>/dev/null | wc -l | tr -d ' ') uncommitted"
  fi
done
```

---

### Step 4: Present Status

**Standard (no task):**
```markdown
## Resuming from Handoff

**Document:** {filename}
**Topic:** {topic}
**Status:** {status}

### Pending ({count})
- [ ] {task}

### Next Steps
1. {step}

**Ready to continue. What would you like to focus on?**
```

**Task workspace:**
```markdown
## Resuming Task: {task_name}

**Document:** {filename}
**Task Dir:** .tasks/{task_name}/
**Status:** {status}

### Task Branches
| Service | Branch | Uncommitted |
|---------|--------|-------------|
| {service} | {branch} | {count} |

### Pending ({count})
- [ ] {task}

### Next Steps
1. {step}

> 📌 Tất cả edits sẽ dùng absolute paths tới:
> `/Users/user/Documents/bd-workspace/.tasks/{task_name}/services/...`
> Commands sẽ chạy với Cwd = `.tasks/{task_name}/`

**Ready to continue task '{task_name}'. What's next?**
```

---

### Step 5: Set Working Context

> **CRITICAL cho task mode:**
> Sau khi present status, agent PHẢI:
> 1. Dùng ABSOLUTE PATHS tới `.tasks/<task>/services/...` cho mọi file edits
> 2. Dùng Cwd = `/Users/user/Documents/bd-workspace/.tasks/<task>/` cho commands
> 3. **KHÔNG** edit files ở `services/` trực tiếp (main workspace)

---

### Step 6: Wait for User

Chờ user confirm next step.

---

## Task Workspace Missing

Nếu handoff reference task nhưng `.tasks/<task>/` không tồn tại:
```markdown
⚠️ Task workspace '.tasks/{task_name}/' không tòn tại.
Có thể đã bị /task-finish cleanup.

Options:
1. Tạo lại: /task-start {task_name}
2. Làm trực tiếp trên main workspace
3. Bỏ qua, start fresh
```

---

## Quy tắc

1. Handoff files LUÔN ở `docs/handoff/` (main workspace)
2. Task context từ `.tasks/<task>/.task-context.json`
3. Sau khi continue task → agent dùng `.tasks/<task>/` paths
4. Nếu task workspace missing → warn user, offer recreate

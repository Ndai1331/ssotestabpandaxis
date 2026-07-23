---
description: Liệt kê task isolation BD (.tasks/)
---

# Task List — BD

```bash
TASKS_DIR="/Users/user/Documents/bd-workspace/.tasks"
ls -la "$TASKS_DIR" 2>/dev/null || echo "Chưa có .tasks/"
```

Nếu có `.tasks.json` → in tóm tắt slug + services + status.

Services hợp lệ: `directus` (`services/directus-main`), `abp` (`services/abp-blazor`).

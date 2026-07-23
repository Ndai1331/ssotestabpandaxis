---
name: worker-logs-helper
enabled: true
event: prompt
conditions:
  - field: user_prompt
    operator: regex_match
    pattern: (worker log|tail worker|check worker|worker status|/worker-logs|/tail-worker|worker state|worker health|worker active)
action: warn
---

**Worker Logs:** Gõ lệnh này để xem logs realtime:

```
bash ~/.claude/hooks/worker-logs.sh
```

**Tùy chọn:**
- `bash ~/.claude/hooks/worker-logs.sh 50 ERROR` — chỉ hiện dòng ERROR
- `bash ~/.claude/hooks/worker-logs.sh 100` — hiện 100 dòng cuối

**Cron realtime (10s):** `/loop 10s bash ~/.claude/hooks/worker-logs.sh`

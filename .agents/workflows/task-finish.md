---
description: Kết thúc / dọn isolated task BD
---

# Task Finish — BD

```
WORKSPACE_ROOT="/Users/user/Documents/bd-workspace"
TASK_DIR="$WORKSPACE_ROOT/.tasks/<TASK_SLUG>"
```

1. Xác nhận user muốn finish.  
2. Commit trong service liên quan **chỉ khi user yêu cầu**.  
3. Cập nhật `wiki/hot.md` / handoff nếu còn việc dở.  
4. Xóa `$TASK_DIR` chỉ khi user xác nhận (không tự xóa code đang cần).  

Không merge/deploy remote — phase [[Local-Only Phase]].

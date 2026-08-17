---
description: Deploy — chưa áp dụng (BD local-only)
---

# Deploy — BD

**Status: DISABLED**

Workspace BD đang phase **local-only** — chưa GitHub, chưa CI/CD, chưa remote server.

Khi user nói "deploy":
1. Nhắc: chưa có pipeline deploy.  
2. Hỏi có muốn setup GitHub/CI trước không.  
3. Không chạy flow Task9 (`[WEB]`/`[API]`, do-122, …).

Local "deploy" duy nhất hợp lệ: restart Docker/process trên máy dev.

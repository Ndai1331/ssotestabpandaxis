---
type: decision
title: "Local-Only Phase"
updated: 2026-07-23
---

# Local-Only Phase

**Quyết định (2026-07-23):** BD workspace chạy thử nghiệm local; **chưa** setup GitHub / GHA / remote deploy.

## Hệ quả cho agent
- Không bắt buộc `origin/main` preflight Task9  
- Không dùng prefix deploy `[WEB]` / `[API]`  
- Không deploy do-122 / do-187  
- Commit chỉ khi user yêu cầu; không tự push  

## Khi có GitHub
Cập nhật `CLAUDE.md` + `docs/workspace-architecture.md` § Git/CI — không copy mù flow Task9.

# HCS Mobile API

Tài liệu chi tiết đã chuyển sang bộ **page-first** (khớp call-site Web hiện tại):

**[docs/api/mobile/README.md](../api/mobile/README.md)**

| File | Nội dung |
|---|---|
| [00-auth.md](../api/mobile/00-auth.md) | PKCE `hcs-mobile`, header Bearer, lỗi, phân trang |
| [01-account.md](../api/mobile/01-account.md) | Hồ sơ, mật khẩu, avatar |
| [02-workspace.md](../api/mobile/02-workspace.md) | Workspace (không gọi `/api/dashboard`) |
| [03-documents.md](../api/mobile/03-documents.md) | Văn bản, ký số, chữ ký |
| [04-workflows.md](../api/mobile/04-workflows.md) | Quy trình |
| [05-projects-tasks.md](../api/mobile/05-projects-tasks.md) | Dự án, công việc |
| [06-calendar.md](../api/mobile/06-calendar.md) | Lịch công tác |
| [07-events.md](../api/mobile/07-events.md) | Sự kiện, QR, check-in |
| [08-surveys.md](../api/mobile/08-surveys.md) | Khảo sát |
| [09-chat.md](../api/mobile/09-chat.md) | Chat, SignalR, thông báo |
| [10-social.md](../api/mobile/10-social.md) | Mạng xã hội |
| [11-lookups.md](../api/mobile/11-lookups.md) | OU, master-data GET, contacts |

Bản runbook 2026-09-09 trong file này đã lệch code (workspace không gọi dashboard; phòng ban dùng `/api/identity/organization-unit-lookup`). Không dùng file này làm SoT API.

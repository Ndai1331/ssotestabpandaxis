---
type: concept
title: "Notification System"
created: 2026-06-29
updated: 2026-06-29
tags:
  - notification
  - api
  - n8n
  - agent
confidence: high
related:
  - "[[Codebase — task9-api]]"
  - "[[Codebase — task9-agent]]"
  - "[[N8N Workflows]]"
  - "[[Database Architecture]]"
---

# Notification System

> Hệ thống thông báo in-app shipped trên `main` (~2026-06). Cho phép N8N / cron / agent đẩy notification tới user trong task9-ui. Liên quan: [[Codebase — task9-api]], [[Codebase — task9-agent]], [[N8N Workflows]].

## Kiến trúc tổng thể

```
N8N workflow / cron  ──HTTP (secret)──►  task9-api  ──►  qcadmin (UserNotification)
agent /api/notify    ──HTTP (NOTIFY_SECRET)──►  task9-ui  ──►  hiển thị cho user
```

Có **2 đường đẩy notification**, đừng nhầm:

1. **N8N → API** (`task9-api`): webhook tạo notification lưu DB, user đọc trong UI.
2. **Agent → UI** (`task9-agent`): forward kết quả cron (vd ETL health check) sang UI qua endpoint notify.

## Đường 1 — N8N Webhook → task9-api

**Controller:** `WebApi/Controllers/N8nWebhookController.cs`
- Route: `POST /api/webhooks/n8n/notifications`
- `[AllowAnonymous]` — KHÔNG dùng JWT. Auth bằng header **`X-N8N-Webhook-Secret`** (so với config). Sai/thiếu secret → `401 {status:"unauthorized"}`.
- Body `N8nNotificationWebhookRequest`: `Title` (req, ≤255), `Message` (req, ≤2000), `SenderUserId?`, `SenderName?` (default "n8n"), `TargetType?`, `TargetValue?`, `Priority?`.

**Controller user-facing:** `WebApi/Controllers/NotificationController.cs` (list/đọc/mark-read cho user đăng nhập).

**Service:** `Application/Notifications/NotificationService.cs` → `CreateAsync(senderUserId, senderName, NotificationCreateRequest)`.

**Repo:** `SqlServ4r/Repository/Notifications/NotificationRepository.cs`.

### Quy ước domain (enum giá trị hợp lệ)
- **TargetType:** `all` | `role` | `user`
  - `all` → `TargetValue = null` (gửi toàn bộ).
  - `role` → `TargetValue` = tên role.
  - `user` → `TargetValue` = user id.
- **Priority:** `normal` | `important`.
- Giá trị không hợp lệ → `400 {status:"invalid_request"}`.
- Fix gần đây: resolve `targetValue` theo **username/email/id** (commit `7905e35` — trước đó chỉ nhận id).

### Tables (qcadmin — xem [[DB Separation Rule]])
- `Domain/Notifications/UserNotification.cs` — bản ghi notification.
- `Domain/Notifications/UserNotificationRead.cs` — trạng thái đã đọc per-user.

## Đường 2 — Agent notify → UI

**Agent** (`services/agent`, xem [[Codebase — task9-agent]]):
- Endpoint `POST /api/notify` — forward kết quả cron sang UI.
- Cron `daily-etl-health-check` thêm bước HTTP notify → khi ETL lỗi/ok thì báo về UI.
- Env cần: `UI_BASE_URL`, `NOTIFY_SECRET` (đã thêm vào `docker-compose.prod.yml`).

## Khi cần làm việc với hệ thống này
- Thêm loại notification mới từ N8N: gọi `POST /api/webhooks/n8n/notifications` với header secret, KHÔNG cần JWT.
- Đổi target/priority: nhớ chỉ chấp nhận enum ở trên, validate ở controller.
- Sửa repo/DB: notification table ở `qcadmin` (operational) — tuân [[DB Separation Rule]].

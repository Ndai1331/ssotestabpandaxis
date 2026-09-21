# 02 — Workspace

Page Web: [`Workspace.razor`](../../../src/HCS.Blazor.Client/Pages/Workspace.razor)  
Policy: `WorkManagement.Dashboard`

Workspace **không** gọi `GET /api/dashboard`. Page ghép nhiều API read-only theo khoảng ngày đang chọn.

## Pages

| Route | Permission |
|---|---|
| `/` | Public; user đã login chuyển `/workspace` |
| `/workspace` | `WorkManagement.Dashboard` |

## Màn hình → API

Web load song song từng widget; lỗi một widget không chặn widget khác.

| Widget | Method | Path | Query Web dùng |
|---|---|---|---|
| Lịch | `GET` | `/api/calendar` | `from`, `to` (UTC ISO-8601) |
| Dự án | `GET` | `/api/projects` | `skip=0`, `take=100` |
| Công việc | `GET` | `/api/project-tasks` | `skip=0`, `take=100` |
| Hàng đợi ký | `GET` | `/api/signing/queue` | — |
| Thông báo | `GET` | `/api/notifications` | `unreadOnly=false`, `skip=0`, `take=20`, `from`, `toExclusive` |
| Filter phòng ban (UI) | `GET` | `/api/identity/organization-unit-lookup` | — |

Sau khi nhận list, Web **lọc client-side** theo khoảng `fromDate`–`toDate` (local):

- Calendar: `StartTime` trong khoảng
- Projects: `EndDate >= from && StartDate <= to`
- Tasks: `DueDate >= from && StartDate <= to`
- Signing queue: `Document.CreationTime` trong khoảng
- Notifications: server đã lọc `from` / `toExclusive`

Lookup OU: [11-lookups.md](11-lookups.md). Chi tiết từng API: [06-calendar.md](06-calendar.md), [05-projects-tasks.md](05-projects-tasks.md), [03-documents.md](03-documents.md), [09-chat.md](09-chat.md).

## Endpoint workspace dùng

### GET `/api/calendar?from={utc}&to={utc}`

Response: **mảng** `CalendarEventDto` (không paged). Field chính: `id`, `title`, `startTime`, `endTime`, `allDay`, `eventType`, `location`, `visibility`, `participantUserIds`, `ownerUserId`.

### GET `/api/projects?skip=0&take=100`

```json
{
  "totalCount": 10,
  "items": [
    {
      "id": "guid",
      "code": "P01",
      "name": "Dự án",
      "startDate": "2026-09-01T00:00:00Z",
      "endDate": "2026-09-30T00:00:00Z",
      "status": "Active",
      "ownerDepartmentId": null,
      "ownerUserId": "guid",
      "memberCount": 3,
      "taskCount": 12,
      "canManage": true,
      "canDelete": false
    }
  ]
}
```

### GET `/api/project-tasks?skip=0&take=100`

Paged `ProjectTaskDto`: `id`, `projectId`, `code`, `title`, `startDate`, `dueDate`, `priority`, `status`, `progressPercent`.

### GET `/api/signing/queue`

Mảng item `{ document, task, instance, definition, canDelete }`. Web đếm `myDocumentCount` = số instance trong queue sau khi lọc ngày.

### GET `/api/notifications?unreadOnly=false&skip=0&take=20&from={utc}&toExclusive={utc}`

Mảng `NotificationDto`: `id`, `userId`, `title`, `body`, `link`, `isRead`, `createdAt`. `link` là route Web (`/document-detail/{id}`, `/chat/{id}`, …) — mobile map sang deep link.

### GET `/api/identity/organization-unit-lookup`

Mảng OU `{ id, parentId, code, displayName, concurrencyStamp }` dùng cho filter đơn vị trên workspace.

## `GET /api/dashboard` — không dùng trên page này

Client Web có `WorkManagementClient.GetDashboardAsync()` → `GET /api/dashboard` trả `{ activeProjects, openTasks, overdueTasks, activeSurveys, calculatedAt }`. **Workspace không gọi.** Mobile parity workspace không cần endpoint này. Có thể dùng sau nếu tự thiết kế màn KPI riêng.

## Ghi chú mobile

- Permission `WorkManagement.Dashboard` để vào màn; từng widget vẫn có thể `403` nếu thiếu quyền con (calendar/projects/signing/notifications) — Web nuốt lỗi widget.
- Pull-to-refresh = gọi lại 5 GET trên.
- Deep link từ notification: xem [09-chat.md](09-chat.md).

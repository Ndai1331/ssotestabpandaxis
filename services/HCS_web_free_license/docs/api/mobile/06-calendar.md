# 06 — Lịch công tác

Page: [`CalendarEvents.razor`](../../../src/HCS.Blazor.Client/Pages/CalendarEvents.razor), [`CalendarEventDetail.razor`](../../../src/HCS.Blazor.Client/Pages/CalendarEventDetail.razor)  
Policy: `WorkManagement.Calendar`  
Người tham dự: `CalendarParticipantPicker` → `GET /api/chat/contacts`.

## Pages

| Route | Việc |
|---|---|
| `/calendar-events` | Lịch theo khoảng; tạo event |
| `/calendar-event-detail/{id}` | Sửa / xóa |

## Màn hình → API

| Hành động | Method | Path |
|---|---|---|
| Loại sự kiện lịch | `GET` | `/api/organization/master-data?type=EventType&isActive=true&skipCount=0&maxResultCount=100` |
| List theo khoảng | `GET` | `/api/calendar?from={utc}&to={utc}` |
| Tạo | `POST` | `/api/calendar` |
| Detail / sửa / xóa | `GET` `PUT` `DELETE` | `/api/calendar/{id}` |
| Gắn task | `GET` | `/api/project-tasks?filter&skip&take=20` |
| Gắn dự án | `GET` | `/api/projects?filter&skip&take=20` |
| Participants | `GET` | `/api/chat/contacts?search&take=50` |

## GET `/api/calendar`

Query `from`, `to` — Web convert local → UTC `O` round-trip. Response **mảng**, không `{totalCount,items}`.

```json
[
  {
    "id": "guid",
    "title": "Họp",
    "description": null,
    "startTime": "2026-09-21T08:00:00Z",
    "endTime": "2026-09-21T09:00:00Z",
    "allDay": false,
    "eventType": "guid-hoặc-mã",
    "location": "P.201",
    "relatedType": "None",
    "relatedId": null,
    "visibility": "Private",
    "participantUserIds": ["guid"],
    "ownerUserId": "guid",
    "canManage": true
  }
]
```

`relatedType` Web: `None` hoặc loại liên kết project/task; `relatedId` string GUID khi gắn.

## POST / PUT `/api/calendar`

Cùng body:

```json
{
  "title": "Họp",
  "description": null,
  "startTime": "2026-09-21T08:00:00Z",
  "endTime": "2026-09-21T09:00:00Z",
  "allDay": false,
  "eventType": "...",
  "location": null,
  "relatedType": "None",
  "relatedId": null,
  "visibility": "Private",
  "participantUserIds": ["guid"]
}
```

`eventType` lấy từ master-data `EventType` (id hoặc code — Web gửi giá trị form, thường id GUID string).

`visibility` string (`Private` / `Public` / đơn vị — theo form Web).

DELETE → `204`.

Đây **không** phải module sự kiện quản lý `/api/events` — xem [07-events.md](07-events.md).

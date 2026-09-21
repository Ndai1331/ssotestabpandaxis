# 07 — Sự kiện

Policy: `WorkManagement.Events`. Check-in public: Razor host [`EventCheckInGatewayClient.cs`](../../../src/HCS.Blazor/EventCheckIn/EventCheckInGatewayClient.cs).

## Pages

| Route | Việc |
|---|---|
| `/events` | List/filter, CRUD, upload file lúc tạo, export CSV client-side |
| `/events/{id}` | Detail, attendees, QR, attachments |
| `/event-dashboard` | `GET /api/events/dashboard` |
| `/event-check-in/{code}` | Public lookup + confirm/decline/check-in |

## Màn hình → API

### List / CRUD

| Hành động | Method | Path |
|---|---|---|
| List | `GET` | `/api/events?filter&group&status&skip&take` |
| Detail | `GET` | `/api/events/{id}` |
| Tạo / sửa / xóa | `POST` `PUT` `DELETE` | `/api/events` |
| Upload file | `POST` | `/api/events/{id}/attachments` max 25 MB |

### Detail attendees

| Hành động | Method | Path |
|---|---|---|
| QR PNG | `GET` | `/api/events/{id}/qr` |
| Attendees paged | `GET` | `/api/events/{id}/attendees?filter&registrationStatus&checkInStatus&skip&take` |
| OU của attendee | `GET` | `/api/identity/organization-unit-lookup/users?userIds=` |
| Picker user | `GET` | `/api/identity/users?filter&skipCount&maxResultCount` và/hoặc `/api/chat/contacts` |
| Thêm | `POST` | `/api/events/{id}/attendees` |
| Đổi status | `POST` | `/api/events/attendees/{attendeeId}/status` |
| Xóa / xóa hàng loạt | `DELETE` / `POST` | `.../attendees/{id}`, `.../attendees/delete-bulk` |
| Import CSV | `POST` | `/api/events/{id}/attendees/import` max 2 MB |
| Xóa file | `DELETE` | `/api/events/attachments/{fileId}` |
| Download file | `GET` | `/api/events/attachments/{fileId}` |

`PUT /api/events/attendees/{id}` có trên client, **page không gọi**.

## Events

List item: `id`, `code`, `group`, `name`, `startTime`, `endTime`, `location`, `status`, `attendeeCount`, `ownerUserId`, `canManage`.

`group` / `status` là **string** trên query và body (Web filter text).

### POST / PUT

```json
{
  "group": "Internal",
  "name": "Hội nghị",
  "content": null,
  "description": null,
  "location": "Hội trường",
  "startTime": "2026-09-21T08:00:00Z",
  "endTime": "2026-09-21T11:00:00Z",
  "status": "Preparing"
}
```

Detail thêm: `qrToken`, `attachments[]`, `attendance: { total, confirmed, unconfirmed, declined, checkedIn, notCheckedIn }`.

### Attendees

```json
{
  "userId": "guid",
  "username": null,
  "surname": null,
  "name": null,
  "fullName": "Nguyễn Văn A",
  "cccd": null,
  "phoneNumber": null,
  "email": null,
  "address": null,
  "registrationStatus": "Pending",
  "checkInStatus": "NotCheckedIn",
  "note": null
}
```

DTO có thêm `checkedInAt`. Status body: `{ "registrationStatus": "Confirmed", "checkInStatus": null }`.

Bulk delete: body JSON **mảng GUID**; response số nguyên đã xóa.

Import: multipart `file` CSV → `{ imported, skipped }`.

## Dashboard

`GET /api/events/dashboard?from={utc}&to={utc}`

```json
{
  "totalEvents": 10,
  "completedEvents": 2,
  "ongoingEvents": 1,
  "upcomingEvents": 7,
  "totalAttendees": 100,
  "upcoming": []
}
```

## Public check-in

Gateway cho phép anonymous prefix `/api/events/public`. Confirm/decline/check-in trên Web host vẫn forward cookie nếu user đã login; guest gửi thêm query `guestPhone`, `guestEmail` và body.

```http
GET  /api/events/public/{code}?token={qrToken}
POST /api/events/public/{code}/confirm?token={qrToken}
POST /api/events/public/{code}/decline?token={qrToken}
POST /api/events/public/{code}/check-in?token={qrToken}
GET  /api/events/public/{code}/attachments/{fileId}?token={qrToken}
```

GET response: `code`, `name`, `startTime`, `endTime`, `location`, `attachments`, `status`, `content`, `description`, `registrationStatus`, `checkInStatus`, `checkedInAt`.

POST body (user đã login có thể `{}`):

```json
{ "fullName": "Nguyễn Văn A", "phoneNumber": "09...", "email": null, "note": null }
```

Confirm/decline → `{ fullName, registrationStatus }`. Check-in → `{ fullName, checkedInAt }`.

Rule nghiệp vụ Web: confirm/decline khi `Preparing`; check-in khi `Ongoing`. Không trộn API này với CRUD admin attendees.

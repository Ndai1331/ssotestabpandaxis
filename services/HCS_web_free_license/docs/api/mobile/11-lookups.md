# 11 — Lookup dùng chung

Chỉ các **GET / lookup** mà page user-facing đang gọi. Không document CRUD admin phòng ban / master-data / users.

## Organization units (phòng ban)

Web **không** dùng `/api/organization/departments` cho picker. Dùng ABP OU:

| Method | Path | Page dùng |
|---|---|---|
| `GET` | `/api/identity/organization-unit-lookup` | Workspace, dự án, signing, social, send-document, calendar-related forms |
| `GET` | `/api/identity/organization-unit-lookup/users?userIds={guid}&userIds=` | Signing, social enrich, event attendees |
| `GET` | `/api/identity/organization-unit-lookup/{organizationUnitId}/members` | Send document theo đơn vị |
| `GET` | `/api/identity/organization-units/{id}/members?filter&skipCount&maxResultCount` | Social filter theo phòng |

Lookup list: mảng

```json
[
  {
    "id": "guid",
    "parentId": null,
    "code": "00001",
    "displayName": "Phòng Kế hoạch",
    "concurrencyStamp": "..."
  }
]
```

User lookup:

```json
[
  {
    "userId": "guid",
    "organizationUnitId": "guid",
    "organizationUnitIds": ["guid"],
    "displayName": "Phòng Kế hoạch"
  }
]
```

Members paged (ABP): `{ totalCount, items: [{ id, userName, fullName, email, isActive }] }`. `id` của member = **userId**. `skipCount`/`maxResultCount` max 100.

`userIds` lookup: Web cắt 200 id/lần.

**Không** gọi `POST/PUT/DELETE /api/identity/organization-units` từ app user — đó là trang `/departments` admin.

## Master data (read)

```http
GET /api/organization/master-data?type={Type}&filter=&isActive=true&skipCount=0&maxResultCount=100
```

Response `{ totalCount, items: [{ id, type, code, name, sortOrder, isActive }] }`.

`type` Web dùng trên page user:

| Type | Page |
|---|---|
| `DocumentType` | Văn bản |
| `Sector` | Văn bản |
| `UrgencyLevel` | Văn bản |
| `ConfidentialityLevel` | Văn bản |
| `EventType` | Lịch công tác |

Đơn vị phát hành trên document-detail: `GET /api/organization/units?filter&isActive&skipCount&maxResultCount` — **read list**, không CRUD `/unit-lists`.

Item unit: `{ id, departmentId, code, name, sortOrder, isActive }`.

## User departments (chức vụ / mapping)

```http
GET /api/organization/user-departments?userIds={guid}&userIds=
GET /api/organization/user-departments?departmentId={guid}
GET /api/organization/user-departments/catalog
```

Web social dùng query `userIds` để lấy `positionName`. Send-document có thể dùng catalog sendable — chỉ khi page gọi `GetSendableDepartmentsAsync`.

Shape phía catalog client:

```json
{
  "userId": "guid",
  "departmentId": "guid",
  "departmentName": "...",
  "positionId": "guid",
  "positionName": "..."
}
```

Social `AttachOrganizationAsync` merge OU lookup (`displayName` / `organizationUnitId`) với mapping này.

## Chat contacts

```http
GET /api/chat/contacts?search=&take=50
GET /api/chat/contacts/lookup?userIds=
```

Dùng khắp: chat, gửi văn bản, signing, project members, calendar participants, event picker. Chi tiết DTO: [09-chat.md](09-chat.md). Max lookup 200 ids.

## Workflow assignees

```http
GET /api/identity/workflow-assignees/lookup?userIds=
```

Trả `[{ userId, displayName, organizationUnitId, userName }]`. Document-detail / signing / PDF preview. Batch Web 200 ids, tối đa 1000.

## Roles (chỉ picker bước quy trình)

`GET /api/identity/roles?maxResultCount=100` — WorkflowDetail chọn `assigneeType` Role. Fallback `GET /api/identity/users/assignable-roles`. Không phải màn quản trị roles.

## Users (picker sự kiện)

Event detail thử `GET /api/identity/users?filter&skipCount&maxResultCount=50` rồi fallback contacts. Mobile có thể **chỉ dùng contacts** nếu không làm admin user list.

## Avatar

```http
GET /api/identity/profile/avatar
GET /api/identity/users/{userId}/avatar
```

`404` = chưa có. Binary. Social/chat/account.

## Employee directory

```http
GET /api/identity/employee-directory?filter=&skipCount=0&maxResultCount=100
```

`{ items: [{ userId, userName, displayName, avatarUrl }], hasMore }`. Page `/employee-ratings` dùng; social **không** gọi. Ghi ở đây vì cùng họ lookup user; không mở CRUD ratings admin.

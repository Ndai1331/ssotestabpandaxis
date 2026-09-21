# 05 — Dự án và công việc

Client: [`WorkManagementClient.cs`](../../../src/HCS.Blazor.Client/Work/WorkManagementClient.cs)

## Pages

| Route | Permission |
|---|---|
| `/projects` | `WorkManagement.Projects` |
| `/project-detail`, `/project-detail/{id}` | `WorkManagement.Projects` |
| `/tasks` | `WorkManagement.ProjectTasks` |
| `/project-task-detail/{id}` | `WorkManagement.ProjectTasks` |

Tạo task: `ProjectTaskCreateModal` (từ list/detail).

## Màn hình → API

### Dự án

| Hành động | Method | Path |
|---|---|---|
| List | `GET` | `/api/projects?filter&status&skip&take` |
| OU owner | `GET` | `/api/identity/organization-unit-lookup` |
| Tạo / sửa / xóa | `POST` `PUT` `DELETE` | `/api/projects`, `/api/projects/{id}` |
| Mở chat dự án | `POST` | `/api/projects/{id}/chat-access` |
| Tìm conversation | `GET` | `/api/chat/conversations/by-project/{id}` (`404` = chưa có) |
| Tạo conversation | `POST` | `/api/chat/conversations` |

### Chi tiết dự án

| Hành động | Method | Path |
|---|---|---|
| Detail | `GET` | `/api/projects/{id}` |
| Contacts | `GET` | `/api/chat/contacts` |
| Thêm / xóa thành viên | `POST` `DELETE` | `/api/projects/{id}/members` |

### Công việc

| Hành động | Method | Path |
|---|---|---|
| List projects (filter) | `GET` | `/api/projects` |
| List tasks | `GET` | `/api/project-tasks?projectId&filter&status&skip&take` |
| Đổi status (kanban) / xóa | `PUT` `DELETE` | `/api/project-tasks/{id}` |
| Tạo (modal) | `POST` | `/api/project-tasks` |
| Gán người / văn bản lúc tạo | `POST` | `.../assignments`, `.../documents` |

### Chi tiết task

`GET /api/project-tasks/{id}`, `PUT` cập nhật, assignments, documents; `GET /api/documents` để chọn văn bản; contacts để chọn người.

## Projects

Paged `{ totalCount, items }`. Query: `skip`, `take` (max 100), `filter`, `status` (string, ví dụ `Active`).

List item: `id`, `code`, `name`, `description`, `startDate`, `endDate`, `status`, `ownerDepartmentId`, `ownerUserId`, `memberCount`, `taskCount`, `canManage`, `canDelete`.

### POST `/api/projects`

```json
{
  "code": "P01",
  "name": "Dự án",
  "description": null,
  "startDate": "2026-09-01T00:00:00Z",
  "endDate": "2026-12-31T00:00:00Z",
  "status": "Active",
  "ownerDepartmentId": null
}
```

PUT **không** gửi `code` / owner:

```json
{
  "name": "Dự án",
  "description": null,
  "startDate": "2026-09-01T00:00:00Z",
  "endDate": "2026-12-31T00:00:00Z",
  "status": "Active"
}
```

`GET /api/projects/{id}` → `{ project, members: [{ id, projectId, userId, role, isActive }], tasks: [...] }`.

`POST /api/projects/{id}/members` `{ "userId": "guid", "role": "Member" }` — Web dùng role form (thường `Member`/`Manager`).

`DELETE /api/projects/{id}/members/{memberId}` — `memberId` là id bản ghi member, không phải userId.

`POST /api/projects/{id}/chat-access` → `204`. Sau đó `GET /api/chat/conversations/by-project/{id}`.

## Tasks

`GET /api/project-tasks?projectId={guid}&filter=&status=&skip=0&take=20`

Task: `id`, `projectId`, `parentTaskId`, `code`, `title`, `description`, `startDate`, `dueDate`, `priority`, `status`, `progressPercent`, `creatorId`, `canDelete`, `canManageAssignments`, `canCreateChild`.

`priority` / `status` Web gửi **string** (không enum số).

### POST `/api/project-tasks`

```json
{
  "projectId": "guid",
  "parentTaskId": null,
  "code": "T01",
  "title": "Công việc",
  "description": null,
  "startDate": "2026-09-21T00:00:00Z",
  "dueDate": "2026-09-30T00:00:00Z",
  "priority": "Normal",
  "status": "Todo",
  "progressPercent": 0
}
```

PUT:

```json
{
  "title": "Công việc",
  "description": null,
  "startDate": "2026-09-21T00:00:00Z",
  "dueDate": "2026-09-30T00:00:00Z",
  "priority": "Normal",
  "status": "Doing",
  "progressPercent": 40
}
```

Detail: `{ task, assignments: [{ id, projectTaskId, userId, assignmentType }], documents: [{ id, projectTaskId, documentId, documentCode }] }`.

`POST .../assignments` `{ "userId", "assignmentType": "Assignee" }`  
`POST .../documents` `{ "documentId", "documentCode": "số văn bản" }`

## Lookup

OU: [11-lookups.md](11-lookups.md). Contacts: chat. Văn bản: `GET /api/documents`.

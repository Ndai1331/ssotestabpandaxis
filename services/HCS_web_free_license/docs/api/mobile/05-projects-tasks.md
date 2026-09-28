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
| Số tiếp theo (placeholder) | `GET` | `/api/projects/next-code` |
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
| Số tiếp theo (placeholder) | `GET` | `/api/project-tasks/next-code?projectId=` |
| Tạo (modal) | `POST` | `/api/project-tasks` |
| Gán người / văn bản lúc tạo | `POST` | `.../assignments`, `.../documents` |

### Chi tiết task

`GET /api/project-tasks/{id}`, `PUT` cập nhật, assignments, documents; `GET /api/documents` để chọn văn bản; contacts để chọn người.

## Projects

Paged `{ totalCount, items }`. Query: `skip`, `take` (max 100), `filter`, `status` (string, ví dụ `Active`), `from`, `to` (dự án có `[startDate, endDate]` giao với `[from, to]`), `ownerDepartmentId`.

List item: `id`, `code`, `name`, `description`, `startDate`, `endDate`, `status`, `ownerDepartmentId`, `ownerUserId`, `memberCount`, `taskCount`, `canManage`, `canDelete`, `progressPercent` (trung bình tiến độ các task **gốc**, làm tròn; không có task → 0).

### POST `/api/projects`

`GET /api/projects/next-code` → `{ "code": "PJ0012" }` (placeholder, không giữ chỗ). Để `code` null khi POST để server cấp.

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

`PUT /api/projects/{id}/members/{memberId}` `{ "role": "Manager" }` — đổi vai trò, giữ nguyên `memberId`. `role`: `Manager` | `Supervisor` | `Member` (phân biệt hoa thường). Chỉ chủ dự án hoặc admin; không đổi được role của chủ dự án (`Work:CannotChangeOwnerRole`). Trả `ProjectMemberDto`.

`DELETE /api/projects/{id}/members/{memberId}` — `memberId` là id bản ghi member, không phải userId.

`POST /api/projects/{id}/chat-access` → `204`. Sau đó `GET /api/chat/conversations/by-project/{id}`.

## Tasks

`GET /api/project-tasks?projectId={guid}&filter=&status=&skip=0&take=20`

Filter thêm: `from`, `to` (theo `dueDate`, gồm cả hai đầu), `priority`, `parentTaskId` (task con trực tiếp), `rootOnly=true` (chỉ task gốc; bỏ qua nếu có `parentTaskId`), `assigneeUserId`.

Task: `id`, `projectId`, `parentTaskId`, `code`, `title`, `description`, `startDate`, `dueDate`, `priority`, `status`, `progressPercent`, `creatorId`, `canDelete`, `canManageAssignments`, `canCreateChild`, `assigneeUserIds` (có trong list, chi tiết dự án, chi tiết task).

`priority` / `status` Web gửi **string** (không enum số).

### POST `/api/project-tasks`

`GET /api/project-tasks/next-code?projectId={guid}` → `{ "code": "T0008" }` (theo từng dự án, không giữ chỗ). Để `code` null khi POST.

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

Detail: `{ task, assignments: [{ id, projectTaskId, userId, assignmentType, note }], documents: [{ id, projectTaskId, documentId, documentCode, addedByUserId, canDelete, note, purpose }], files: [ProjectTaskFileDto] }`.

`POST .../assignments` `{ "userId", "assignmentType": "Assignee", "note": null }`  
`POST .../documents` `{ "documentId", "documentCode": "số văn bản", "note": null, "purpose": "REFERENCE" }`

`note` ≤ 1000 (tự trim; rỗng = `null`). `purpose`: `REPORT` | `REFERENCE` (không phân biệt hoa thường, mặc định `REFERENCE`; khác → `Work:InvalidTaskDocumentPurpose`).

### File đính kèm task

| Route | Ghi chú |
|---|---|
| `POST /api/project-tasks/{id}/files` | multipart field `file`, ≤ 25 MB. Thành viên task (quyền `WorkManagement.ProjectTasks`). Trả `ProjectTaskFileDto` |
| `GET /api/project-tasks/{id}/files/{fileId}` | Tải file (thành viên task) |
| `DELETE /api/project-tasks/{id}/files/{fileId}` | Người upload, người tạo task, chủ dự án hoặc admin (`Work:CannotDeleteOthersFile`) |

`ProjectTaskFileDto`: `id`, `fileName`, `contentType`, `size`, `uploadedByUserId`, `createdAt`, `canDelete`. Xoá task sẽ xoá luôn file.

## Lookup

OU: [11-lookups.md](11-lookups.md). Contacts: chat. Văn bản: `GET /api/documents`.

# Phase 6 — Dự án / công việc (4.1–4.5)

Service: `services/work-management/HCS.WorkManagementService`. Prefix gateway `/api/projects`, `/api/project-tasks` đã có.

## Route / field mới

| # | Loại | Thay đổi |
|---|---|---|
| 4.1 | Route | `POST /api/project-tasks/{id}/files` (multipart `file`, ≤ 25 MB theo `WorkAssetService.MaxFileSize`), `GET /api/project-tasks/{id}/files/{fileId}` (download), `DELETE /api/project-tasks/{id}/files/{fileId}`. Entity `ProjectTaskAttachment` lưu qua `WorkAssetStorage` (pattern `EventAttachment`). `ProjectTaskDetailDto` thêm `files: [{ id, fileName, contentType, size, uploadedByUserId, createdAt, canDelete }]` |
| 4.2 | Route | `PUT /api/projects/{id}/members/{memberId}` body `{ "role": "Manager" \| "Supervisor" \| "Member" }`. `ProjectMember.SetRole(...)`; chỉ chủ dự án; không đổi role của chủ dự án |
| 4.3 | Query | `GET /api/projects` thêm `from`, `to` (giao với `StartDate`–`EndDate`), `ownerDepartmentId`. `GET /api/project-tasks` thêm `from`, `to` (theo `DueDate`), `priority`, `parentTaskId`, `rootOnly`, `assigneeUserId`. Giữ `take` ≤ 100 |
| 4.4 | Field | `ProjectTaskDto` thêm `assigneeUserIds: guid[]` (load theo lô). `ProjectDto` thêm `progressPercent` = trung bình `ProgressPercent` các task gốc (không có task → 0) |
| 4.5 | Field | `AddTaskAssignmentDto` thêm `Note` (≤ 1000). `AddTaskDocumentReferenceDto` thêm `Note` (≤ 1000) + `Purpose` (`REPORT` / `REFERENCE`, mặc định `REFERENCE`). Entity + DTO đọc tương ứng + migration |

## Files

- `Controllers/ProjectsController.cs`, `Controllers/ProjectTasksController.cs`
- `Application/WorkAppServices.cs`, `Application/WorkAssetService.cs`
- `Domain/WorkEntities.cs`, `Data/WorkManagementDbContext.cs`, `Migrations/*`
- `HCS.WorkManagementService.Contracts/WorkContracts.cs` (field mới có default)

## Done khi

- Upload/xoá file task có kiểm quyền (người upload, người tạo task, chủ dự án, admin — cùng rule `RemoveDocumentAsync`).
- Đổi role không đổi `memberId`.
- Web hiện tại (upload qua văn bản) vẫn chạy.

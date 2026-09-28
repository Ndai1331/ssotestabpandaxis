# Phase 5 — Văn bản, ký số, quy trình (3.2–3.6, 3.8, 3.9)

3.1 không làm (quyết định 2026-09-28): bước ký giữ nguyên ghi ghi chú lên PDF qua `POST /api/signing/attempts`; bước xử lý chỉ lưu `comment` của `POST /api/workflows/tasks/{taskId}/decision`.

Service: `services/document/HCS.DocumentService`. Prefix gateway `/api/documents`, `/api/workflows`, `/api/signing` đã có.

## Route / field mới

| # | Loại | Thay đổi |
|---|---|---|
| 3.2 | Route | `GET /api/signing/history?skip&take&from&toExclusive&decision` — task mà `DecidedBy == me`, mới quyết định trước. Item: `{ document, task, instance, definition }` (tái dùng `SigningQueueItemDto`), `decision`: `Approved`/`Rejected`/`Returned` |
| 3.3 | Query + field | `GET /api/documents` thêm `organizationUnitId`, `workflowDefinitionId`, `processingMethodId`. `DocumentAggregate` thêm `ProcessingMethodId` (master-data type `ProcessingMethod` đã có ở Organization) + migration + field trong create/update/DTO |
| 3.4 | Field | `DocumentDto` thêm `isViewed` (user hiện tại đã có History `Viewed`) và `sentAt` (History `Sent` mới nhất). Tính theo lô trong list, không trả cả History |
| 3.5 | Field | `ApprovalTaskDto` thêm `isOverdue` = `Status == Pending && DueAt < now` |
| 3.6 | Route | `GET /api/signing/signatures/{id}` → `UserSignatureDto` |
| 3.8 | Route | `GET /api/signing/stats?from&toExclusive` → `{ pending, approved, rejected, returned, overdue }` của **user hiện tại** (đếm theo task), không cần quyền báo cáo |
| 3.9 | Query | `GET /api/workflows/instances` thêm `scope` (`all` mặc định / `mine` = mình được giao hoặc đã quyết định / `decidedByMe`) + `skip`, `take` (≤ 100). Giữ trả mảng để không phá Web; thêm route `GET /api/workflows/instances/page` trả `{ totalCount, items }` |
| 3.7 | Field | `WorkflowInstanceDto` thêm `currentStepCode` (StepCode của task Pending đầu tiên, null nếu xong) để mobile không phải hiểu `currentStep` |

## Files

- `Controllers/SigningController.cs`, `Controllers/DocumentsController.cs`, `Controllers/WorkflowsController.cs`
- `Signing/SigningAppService.cs`, `Documents/DocumentAppService.cs`, `Documents/DocumentAccess.cs`, `Workflows/WorkflowAppService.cs`
- `HCS.DocumentService.Contracts/**` (field mới có default)
- `Domain/DocumentAggregate.cs` + migration (3.3)

## Lưu ý

- `DocumentAccess.FilterBySource` đang nhận `mine` nhưng không dùng — sửa cùng 3.3 hoặc ghi rõ trong docs.
- Filter `status` của `queue-page` phân biệt hoa thường — cân nhắc đổi sang `OrdinalIgnoreCase` (không phá Web vì Web gửi đúng case).

## Done khi

- Test cho history/stats chỉ trả dữ liệu của user hiện tại.

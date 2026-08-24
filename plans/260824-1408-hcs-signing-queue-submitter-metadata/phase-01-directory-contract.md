# Phase 01 — Directory batch contract

Status: `pending`  
Priority: `P1`  
Depends on: none  
Owner: Platform identity + existing workflow contract files

## Context and scope

`DocumentSigning` cần tên/department của submitter nhưng `ChatContactsController` loại current user và giới hạn 50. `HttpWorkflowAssigneeResolver` đã có pattern gọi identity service và forward bearer token. Tận dụng controller/DTO hiện có, không tạo service directory mới.

## Implementation steps

1. Thêm `GET /api/identity/workflow-assignees/lookup?userId={guid}&userId={guid}` vào controller hiện có. Input là danh sách ID distinct, reject/400 khi rỗng hoặc vượt tối đa 100; không nhận wildcard/all-users.
2. Bảo vệ route bằng permission hiện có cho signing queue (`Documents.Signing.Execute`); giữ nguyên manual `WorkflowStart` check của endpoint `/{userId}`. Chỉ trả active users được yêu cầu, theo đúng `WorkflowAssigneeCandidateDto` hiện có: `UserId`, display name, username, primary organization unit.
3. Dùng repository query lọc theo ID trong database, không load toàn identity directory rồi filter in-memory. Dedupe trước query; missing/inactive ID bị omit, không trả lỗi toàn batch.
4. Xác nhận primary OU là cùng quy tắc `IsPrimary` mà Organization service đang dùng. Nếu Platform không đủ dữ liệu OU hoặc permission constant khác tên trong dirty diff, chốt lại bằng symbol hiện có trước code.

## Data flow and dependency gate

`BFF bearer -> Platform controller authorization -> identity repository (requested IDs) -> candidate DTO list -> BFF response`. Không cần đổi gateway route vì wildcard identity route đã có. Phase 02 không bắt đầu cho tới khi route trả đúng JSON và role queue được phép gọi.

## Security / failure modes

| Failure | L | I | Mitigation |
|---|---:|---:|---|
| Endpoint thành user-enumeration API | M | H | signing permission, max 100, only requested IDs, active-only, no search/all endpoint, không log PII |
| Repository query thành full-table scan | M | M | `WHERE Id IN (...)`, bounded input, execution plan/SQL log kiểm tra khi test |
| User không tồn tại/inactive | M | M | omit item, client fallback `—`, không fail toàn queue |
| Dirty resolver/controller bị ghi đè | H | H | snapshot status/diff, patch additive từng hunk, review `git diff --` đúng file |

## Success criteria

- Authenticated account có `Documents.Signing.Execute` nhận được tối đa 100 candidate theo ID.
- Account thiếu permission nhận 403; endpoint exact-user vẫn giữ behavior cũ.
- Candidate trả đúng display name, username và primary OU; ID không hợp lệ không làm 500.
- Không migration, không sửa nghĩa `DocumentAggregate.FromUserId/OrganizationUnitId`.

## Rollback

Route additive nên rollback bằng revert riêng controller/contract; không có persisted data cần phục hồi. Nếu permission hoặc identity query chưa an toàn, không nối client; giữ queue hiện tại hoạt động.

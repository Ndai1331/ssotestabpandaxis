# 04 — Quy trình

Cùng `DocumentClient` với văn bản. Permission page: `Documents.Workflow.View`; tạo/sửa/xóa cần `Documents.Workflow.Manage`; quyết định cần `Documents.Workflow.Decide`.

## Pages

| Route | Việc Web làm |
|---|---|
| `/workflow-definitions` | CRUD **loại** quy trình (`kinds`) |
| `/workflow-lists` | List definitions + templates; tạo/xóa definition |
| `/workflow-detail`, `/workflow-detail/{id}` | Sửa definition, template, upload file mẫu |
| `/workflow-instances`, `/document-workflow-instances`, `/document-workflow-instances/{id}` | List/detail instance, decision, resubmit |

Trình ký từ văn bản: `SubmitWorkflowModal` (nhúng document-detail / signing).

## Màn hình → API

### Loại quy trình (`/workflow-definitions`)

| Hành động | Method | Path |
|---|---|---|
| List | `GET` | `/api/workflows/kinds` |
| Tạo | `POST` | `/api/workflows/kinds` |
| Sửa | `PUT` | `/api/workflows/kinds/{id}` |
| Xóa | `DELETE` | `/api/workflows/kinds/{id}` |

### Danh sách quy trình (`/workflow-lists`)

`GET /api/workflows/definitions`, `GET /api/workflows/templates`, `GET /api/workflows/kinds`, `POST /api/workflows/definitions`, `DELETE /api/workflows/definitions/{id}`.

### Chi tiết quy trình

| Hành động | Method | Path |
|---|---|---|
| Detail | `GET` | `/api/workflows/definitions/{id}` |
| Tạo / sửa | `POST` / `PUT` | `/api/workflows/definitions` |
| Template | `GET/POST/PUT` | `/api/workflows/templates` |
| Active | `POST` | `/api/workflows/templates/{id}/active` body `true`/`false` |
| Upload file mẫu | `POST` | `/api/workflows/templates/{id}/files?kind=word\|pdf` |
| Download PDF mẫu | `GET` | `/api/workflows/templates/{id}/files/pdf/content` |
| Contacts bước | `GET` | `/api/chat/contacts` |
| OU bước | `GET` | `/api/identity/organization-unit-lookup` |
| Role bước | `GET` | `/api/identity/roles?maxResultCount=100` (fallback `/api/identity/users/assignable-roles`) |

### Hồ sơ / quyết định

| Hành động | Method | Path |
|---|---|---|
| Văn bản workflow | `GET` | `/api/documents?sourceType=3&skip=0&take=100` |
| Definitions | `GET` | `/api/workflows/definitions` |
| Instances | `GET` | `/api/workflows/instances` |
| Detail | `GET` | `/api/workflows/instances/{id}` |
| Quyết định | `POST` | `/api/workflows/tasks/{taskId}/decision` |
| Resubmit | `POST` | `/api/workflows/instances/{id}/resubmit` |

### SubmitWorkflowModal (từ văn bản)

| Hành động | Method | Path |
|---|---|---|
| Definitions / templates | `GET` | `/api/workflows/definitions`, `/api/workflows/templates` |
| Ứng viên bước | `GET` | `/api/workflows/definitions/{id}/assignee-candidates` |
| Upload file trước khi start | `POST` | `/api/documents/{id}/files` |
| Start | `POST` | `/api/workflows/instances` |

## Kinds

`GET /api/workflows/kinds` → mảng `{ id, code, name, description, isActive, creationTime }`.

`GET /api/workflows/next-code` → `{ "code": "QT0008" }` (peek kinds + definitions, không giữ chỗ). Để `code` null khi POST để server cấp.

POST `{ "code", "name", "description", "isActive": true }` → `guid`.  
PUT `{ "name", "description", "isActive" }` → `204`.  
DELETE → `204`.

## Definitions

`GET /api/workflows/definitions` — mảng. Detail `GET .../{id}`.

DTO: `id`, `code`, `name`, `steps[]`, `creationTime`, `kindId`, `description`, `isActive`, `signMode` (`SEQUENTIAL` mặc định).

Step: `id`, `code`, `name`, `order`, `requiredPermission`, `type` (`PROCESS`), `assigneeUserId`, `assigneeType` (`SpecificUser` / role / department…), `roleId`, `userIds`, `departmentIds`, `slaDays`, `allowReturn`.

POST create:

```json
{
  "code": "APPROVAL_01",
  "name": "Quy trình duyệt",
  "kindId": "guid",
  "description": null,
  "isActive": true,
  "signMode": "SEQUENTIAL",
  "steps": [
    {
      "code": "REVIEW",
      "name": "Kiểm tra",
      "order": 1,
      "requiredPermission": "Documents.Workflow.Decide",
      "type": "PROCESS",
      "assigneeType": "SpecificUser",
      "assigneeUserId": "guid",
      "slaDays": 2,
      "allowReturn": true
    }
  ]
}
```

POST trả `guid`. PUT không trả body (`204`).

`GET /api/workflows/definitions/{definitionId}/assignee-candidates` → nhóm theo bước: `{ stepCode, stepName, assigneeType, roleId, candidates: [{ userId, displayName, organizationUnitId, userName }] }`.

## Templates

`GET /api/workflows/templates` — mảng.

DTO: `id`, `code`, `name`, `definitionId`, `version`, `isActive`, `wordFileId`, `wordFileName`, `pdfFileId`, `pdfFileName`, `templateJson`, `outputFormat` (`PDF` mặc định), `creationTime`.

POST `{ "code", "name", "definitionId", "version", "templateJson", "outputFormat": "PDF" }`.

PUT `{ "name", "templateJson", "outputFormat": "PDF" }`.

`POST /api/workflows/templates/{id}/active` body JSON boolean `true`/`false`.

Upload: multipart `file`, query `kind=word` hoặc `kind=pdf`, max 50 MB.

## Instances

`GET /api/workflows/instances?documentId={guid}&status={name}` — mảng (Web list không filter).

DTO: `id`, `documentId`, `definitionId`, `status`, `currentStep`, `tasks[]`, `creationTime`.

`WorkflowInstanceStatus`: `Running=0`, `Completed=1`, `Rejected=2`, `Cancelled=3`, `Returned=4`.  
`ApprovalTaskStatus`: `Pending=0`, `Approved=1`, `Rejected=2`, `Cancelled=3`, `Returned=4`.

Task: `id`, `instanceId`, `stepCode`, `status`, `decidedBy`, `decidedAt`, `assigneeUserId`, `dueAt`, `comment`.

### POST `/api/workflows/instances` — start

`idempotencyKey` bắt buộc. Phải chọn đúng nguồn file: `useWorkflowTemplateFile` **hoặc** `documentId` (rule hiện tại).

```json
{
  "documentId": "guid",
  "definitionId": "guid",
  "idempotencyKey": "uuid-without-dashes-or-with",
  "signers": [{ "stepCode": "REVIEW", "userId": "guid" }],
  "viewScopes": [{ "stepCode": "REVIEW", "departmentIds": [], "userIds": [] }],
  "useTemplateFile": false,
  "useWorkflowTemplateFile": false,
  "signingContent": null
}
```

Web dùng `Guid.NewGuid().ToString("N")` (32 hex, không gạch).

### POST `/api/workflows/tasks/{taskId}/decision`

```json
{
  "approve": true,
  "comment": "Đồng ý",
  "idempotencyKey": "uuid",
  "return": false,
  "signingAttemptId": null,
  "signingFileId": null
}
```

Ký rồi duyệt: điền `signingAttemptId` / `signingFileId` từ `POST /api/signing/attempts`.

### POST `/api/workflows/tasks/{taskId}/extend`

```json
{ "additionalDays": 3, "reason": "Gia hạn từ hàng đợi ký" }
```

Web signing queue gửi `additionalDays: 3`.

### POST `/api/workflows/instances/{id}/resubmit`

Body JSON **string** idempotency key: `"uuid"`. Response instance mới.

## Lookup phụ

- Contacts: [11-lookups.md](11-lookups.md)
- `GET /api/identity/workflow-assignees/lookup?userIds=`
- `GET /api/identity/roles` — chỉ list để chọn `assigneeType` Role, không phải màn admin roles

# 03 — Văn bản và ký số

Clients: [`DocumentClient.cs`](../../../src/HCS.Blazor.Client/Documents/DocumentClient.cs), [`OrganizationCatalogClient`](../../../src/HCS.Blazor.Client/Pages/Organization/OrganizationCatalogClient.cs), chat contacts, OU lookup.

## Pages

| Route | Permission |
|---|---|
| `/manage-documents`, `/my-documents`, `/document-assignments`, `/document-files`, `/document-histories` | `Documents.View` |
| `/document-detail`, `/document-detail/{id}`, `/view-document-detail/{id}` | `Documents.View` |
| `/document-signing`, `/document-signing/{relatedId}` | Authenticated; action theo signing/workflow |
| `/signature-settings` | `Documents.Signing.Configure` |
| `/user-signatures` | Redirect `/account?tab=signatures` |
| `/account` tab chữ ký | Authenticated |

`POST /api/documents/{id}/submit` có trên client nhưng **không** page nào gọi. Web dùng send / assign / start workflow.

## Màn hình → API

### Danh sách văn bản

| Hành động | Method | Path |
|---|---|---|
| Filter loại/lĩnh vực/khẩn/mật | `GET` | `/api/organization/master-data?type=DocumentType\|Sector\|UrgencyLevel\|ConfidentialityLevel` |
| List | `GET` | `/api/documents` |
| Preview dòng | `GET` | `/api/documents/{id}` |
| Thu hồi | `POST` | `/api/documents/{id}/revoke` |
| Xóa | `DELETE` | `/api/documents/{id}` |

### Chi tiết / tạo

| Hành động | Method | Path |
|---|---|---|
| Master-data + search | `GET` | `/api/organization/master-data` |
| Đơn vị | `GET` | `/api/organization/units` |
| Contacts người nhận | `GET` | `/api/chat/contacts` |
| Tạo | `POST` | `/api/documents` |
| Sửa | `PUT` | `/api/documents/{id}` |
| Upload file | `POST` | `/api/documents/{id}/files` |
| Xóa file | `DELETE` | `/api/documents/{id}/files/{fileId}` |
| Preview PDF | `GET` | `/api/documents/{id}/files/{fileId}/watermarked-content` |
| File không PDF | `GET` | `/api/documents/{id}/files/{fileId}/content` |
| Tên người liên quan | `GET` | `/api/identity/workflow-assignees/lookup?userIds=` |
| Ghi activity (preview modal) | `POST` | `/api/documents/{id}/activity` |

Gửi văn bản (`SendDocumentModal`):

| Hành động | Method | Path |
|---|---|---|
| Gửi user | `POST` | `/api/documents/{id}/send` `{ receiverUserId }` |
| Gửi đơn vị | `POST` | `/api/documents/{id}/send` `{ organizationUnitId }` |
| Assign xem | `POST` | `/api/documents/{id}/assignments` |
| Thành viên OU | `GET` | `/api/identity/organization-unit-lookup/{ouId}/members` |

### Ký duyệt

| Hành động | Method | Path |
|---|---|---|
| Queue | `GET` | `/api/signing/queue` |
| Credential / chữ ký | `GET` | `/api/signing/credentials/current`, `/api/signing/signatures` |
| Report | `GET` | `/api/signing/reports/documents/{documentId}` |
| Ký | `POST` | `/api/signing/attempts` |
| Quyết định | `POST` | `/api/workflows/tasks/{taskId}/decision` |
| Gia hạn | `POST` | `/api/workflows/tasks/{taskId}/extend` |
| Xóa trình ký | `DELETE` | `/api/documents/{id}/submission` |
| OU của người trình | `GET` | `/api/identity/organization-unit-lookup/users?userIds=` |

### Cấu hình chữ ký / chữ ký cá nhân

| Hành động | Method | Path |
|---|---|---|
| Providers | `GET` | `/api/signing/provider-definitions` |
| Lưu credential | `PUT` | `/api/signing/credentials/current` JSON; có ảnh: `PUT .../upload` multipart |
| List / upload / sửa / default / xóa chữ ký | xem mục User signatures | |

Trình ký từ văn bản: `SubmitWorkflowModal` → [04-workflows.md](04-workflows.md).

## Documents API

### GET `/api/documents`

Query (Web): `skip`, `take` (max 100), `mine` (`true`/`false`), `filter`, `status`, `sourceType` (số), `documentTypeId`, `sectorId`, `urgencyId`, `confidentialityId`, `from`, `to` (ISO-8601).

`sourceType`: `Archive=0`, `Personal=1`, `SentToMe=2`, `Workflow=3`. Workflow instances page gọi `sourceType=3`.

Response `{ totalCount, items: DocumentDto[] }`.

`DocumentStatus`: `Draft=0`, `Submitted=1`, `InReview=2`, `Approved=3`, `Rejected=4`, `Archived=5`.

DocumentDto chính: `id`, `number`, `title`, `description`, `status`, `documentTypeId`, `sectorId`, `urgencyId`, `confidentialityId`, `files[]`, `assignments[]`, `history[]`, `creationTime`, `sourceType`, `parentDocumentId`, `fromUserId`, `organizationUnitId`, `fileCount`, `isSent`, `documentCode`.

File: `id`, `fileName`, `contentType`, `size`, `sha256`, `creationTime`, `pairedFileId`, `isWorkflowFile`.

### POST `/api/documents`

```json
{
  "number": null,
  "title": "Tên văn bản",
  "description": null,
  "documentTypeId": "guid",
  "sectorId": "guid",
  "urgencyId": "guid",
  "confidentialityId": "guid",
  "sourceType": 0,
  "documentCode": null,
  "organizationUnitId": null
}
```

Quyền tạo/sửa: `Documents.Update`. File max **50 MB**, field multipart `file`.

### PUT `/api/documents/{id}`

```json
{
  "title": "...",
  "description": null,
  "documentTypeId": null,
  "sectorId": null,
  "urgencyId": null,
  "confidentialityId": null,
  "documentCode": null,
  "organizationUnitId": null
}
```

### POST `/api/documents/{id}/send`

```json
{ "receiverUserId": "guid", "organizationUnitId": null }
```

Quyền `Documents.Assign`. Gửi đơn vị: `receiverUserId` null, `organizationUnitId` = OU id.

### POST `/api/documents/{id}/assignments`

```json
{ "assigneeUserId": "guid", "responsibility": "VIEW" }
```

### POST `/api/documents/{id}/revoke` — body `{}`. Quyền `Documents.Assign`.

### POST `/api/documents/{id}/activity`

```json
{ "action": "Viewed" }
```

Web ghi khi preview PDF.

### DELETE `/api/documents/{id}` — `204`.

### DELETE `/api/documents/{id}/submission` — xóa trình ký (signing queue).

Download: lấy tên file từ `Content-Disposition`. Không dùng URL storage nội bộ.

## Signing API

### GET `/api/signing/queue`

Mảng `{ document, task, instance, definition, canDelete }`.

### POST `/api/signing/attempts`

`idempotencyKey` bắt buộc.

```json
{
  "documentId": "guid",
  "fileId": "guid",
  "kind": 0,
  "idempotencyKey": "uuid",
  "signatureId": null,
  "placeholder": null,
  "signerName": "Nguyễn Văn A",
  "note": null
}
```

`SigningKind`: `Electronic=0`, `RemoteCa=1`, `Hsm=2`, `UsbToken=3`.  
`SigningStatus`: `Pending=0`, `Completed=1`, `Failed=2`.

Response: `id`, `documentId`, `fileId`, `kind`, `status`, `inputSha256`, `outputSha256`, `error`, `creationTime`, `completedAt`.

Decision/extend: [04-workflows.md](04-workflows.md).

### Credentials

```http
GET /api/signing/credentials/current?userId={optional}
GET /api/signing/provider-definitions
PUT /api/signing/credentials/current?userId={optional}
PUT /api/signing/credentials/current/upload?userId={optional}
```

JSON body:

```json
{
  "kind": 0,
  "endpoint": "https://...",
  "secret": "...",
  "providerCode": "HCS",
  "layoutImageBase64": null,
  "apiTimeoutSeconds": 30,
  "signWidth": 150,
  "signHeight": 70,
  "allowElectronicSign": true,
  "allowDigitalSign": true,
  "requireOtp": false,
  "credentialId": null
}
```

Multipart upload thêm `layoutImage` (max 3 MB) và các field string tương ứng. **Không log secret.** Response credential có `maskedSecret`, không trả secret thật.

### GET `/api/signing/reports/documents/{documentId}`

`{ documentId, completed, failed, attempts[] }`.

## User signatures

Query `userId` optional (admin sửa hộ). Web account chỉ gọi không `userId`.

```http
GET    /api/signing/signatures
POST   /api/signing/signatures          multipart
PUT    /api/signing/signatures/{id}     multipart
PUT    /api/signing/signatures/{id}/default
DELETE /api/signing/signatures/{id}
GET    /api/signing/signatures/{id}/content
```

POST multipart: `file` (bắt buộc, max 2 MB, jpeg/png/webp/gif), `signatureType` (`Electronic`/`Digital`), `providerCode`, `tokenRef`, `secret`, `sealImage` (max 2 MB), `validFrom`, `validTo`, `isActive`.

`UserSignatureType`: `Electronic=0`, `Digital=1`.

DTO: `id`, `fileName`, `contentType`, `size`, `isDefault`, `creationTime`, `type`, `providerCode`, `tokenRef`, `validFrom`, `validTo`, `isActive`, `hasSealImage`.

## Lookup

Xem [11-lookups.md](11-lookups.md): master-data GET, units GET, contacts, OU lookup, workflow-assignees.

# 12 — Trạng thái đề xuất API mobile (mục 1–4)

Trả lời file đề xuất `new-api-request.md` (mục 1 Chat, 2 Tài khoản/push, 3 Văn bản/ký số/quy trình, 4 Dự án/công việc). Mục 0 (hạ tầng) và 5 (module ngoài contract) không nằm trong file này.

Đối chiếu theo **source code** ngày 2026-09-28 (sau plan `260927-1910-hcs-mobile-api-gaps`). Base URL, header, lỗi: xem [00-auth.md](00-auth.md).

> Các route mới chỉ chạy sau khi server deploy bản này **và** chạy migration: Collaboration → Document → WorkManagement. Trước đó gateway trả `404` / thiếu field.

Quyết định sản phẩm ngày 2026-09-28:

- Không cho tự đăng ký (2.5).
- Thu hồi tin không giới hạn thời gian (1.3).
- Không trả `userCode` / `dob` / `gender` (2.3).
- Tắt thông báo hội thoại chỉ tắt thông báo realtime SignalR; push vẫn gửi (1.8).
- Duyệt: bước ký ghi chú lên PDF như hiện tại, bước xử lý chỉ lưu ghi chú (3.1).

Enum trả về dạng **số** (ví dụ `ConversationType`: `User=0`, `Group=1`, `Project=2`, `Task=3`). Thời gian là UTC ISO-8601. Field mới đều **thêm cuối** DTO, có giá trị mặc định — app cũ bỏ qua được.

Route trả `Task` không có body: thành công là `2xx` body rỗng.

---

## A. Thay đổi bắt buộc phía mobile

Mobile **phải** dùng cột "Bắt buộc dùng". Endpoint cột "Cũ" không còn trong source hoặc gateway.

| Chức năng | Cũ / app đang làm | Bắt buộc dùng |
|---|---|---|
| Đăng ký push token | `POST /api/app/user-push-device-token/register` (**đã bỏ**) | `POST /api/notifications/devices` `{ "token", "platform" }` |
| Huỷ push token khi logout | – | `POST /api/notifications/devices/unregister` `{ "token" }` — gọi **trước** khi xoá access token |
| Thêm thành viên chat | Đoán `{ userId }` | `POST /api/chat/conversations/{id}/members` `{ "userIds": ["guid"] }` |
| Ghim / bỏ ghim tin | Đoán body | `PUT /api/chat/messages/{id}/pin` `{ "pinned": true }` |
| Tải tin cũ hơn | `take=1` lấy `totalCount` rồi tính `skip` | `GET /api/chat/conversations/{id}/messages?beforeMessageId={oldestId}&take=50` |
| Thời gian tin / thành viên | Suy từ `id` | Dùng thẳng `createdAt` / `joinedAt` (đã sửa + backfill) |
| Thu hồi tin | `DELETE /api/chat/messages/{id}` | `POST /api/chat/messages/{id}/recall`; `DELETE` giữ cho admin kiểm duyệt |
| Toast khi hội thoại tắt thông báo | – | Bỏ toast khi `ReceiveMessage.isConversationMuted == true` |
| Picker người dùng > 50 | `/api/chat/contacts` tối đa 50 | `GET /api/chat/contacts/page?search&skip&take` |
| Hồ sơ đầy đủ | Gọi 3–4 API | `GET /api/identity/my-profile-summary` |
| Duyệt kèm ghi chú | `POST /api/app/documents/approve-with-note` (**đã bỏ**) | Bước ký: `POST /api/signing/attempts` có `note` rồi `decision`. Bước xử lý: chỉ `decision` với `comment` |
| Văn bản tôi đã ký/xử lý | `queue-page?inbox=toMe` | `GET /api/signing/history` |
| Kiểm tra quá hạn | `check-and-handle-overdue` (**đã bỏ**) | `task.isOverdue`, hoặc `queue-page?status=Overdue` |
| Bước hiện tại của hồ sơ | `instance.currentStep` làm index | `instance.currentStepCode` |
| Đổi vai trò thành viên dự án | Xoá rồi thêm lại | `PUT /api/projects/{id}/members/{memberId}` `{ "role" }` |
| Đính kèm file vào task | 3 bước qua văn bản | `POST /api/project-tasks/{id}/files` (multipart) |
| Tự đăng ký tài khoản | Mock | **Bỏ chức năng** |

---

## 1. Chat

Prefix `/api/chat`, quyền `Collaboration.Chat`. Tài liệu chat đầy đủ: [09-chat.md](09-chat.md).

| # | Chức năng | Trạng thái |
|---|---|---|
| 1.1 | Xoá hội thoại / nhóm | Đã có |
| 1.2 | Xoá lịch sử phía mình | Đã có |
| 1.3 | Thu hồi tin | Đã có |
| 1.4 | Tìm tin server-side | Đã có |
| 1.5 | Media / file / link trong hội thoại | Đã có |
| 1.6 | Reaction | Đã có |
| 1.7 | Lưu tin | Đã có |
| 1.8 | Tắt thông báo hội thoại | Đã có |
| 1.9 | Sửa mô tả / avatar nhóm | Đã có |
| 1.10 | Body `members`, `pin` | Đã có |
| 1.11 | `createdAt` / `joinedAt` = `0001-01-01` | Đã sửa (có backfill) |
| 1.12 | Phân trang từ tin mới nhất | Đã có |
| 1.13 | `totalCount` của conversations / notifications | Đã có (không có `totalCount`, theo thiết kế) |

### 1.1 Xoá hội thoại

```http
DELETE /api/chat/conversations/{id}
```

| Loại | Hành vi |
|---|---|
| `User` (1-1) | Ẩn **phía mình** + xoá lịch sử phía mình. Hội thoại hiện lại khi có tin mới, chỉ thấy tin sau lúc xoá |
| `Group` | Chỉ admin nhóm hoặc admin hệ thống. Xoá cho mọi người. Event `ConversationDeleted { conversationId }` gửi tới mọi thành viên |
| `Project`, `Task` | Lỗi `Collaboration:WorkConversationCannotBeDeleted` (hội thoại gắn công việc) |

Nhận `ConversationDeleted`: bỏ hội thoại khỏi danh sách, đóng màn chat nếu đang mở.

Rời nhóm / xoá thành viên giữ nguyên: `POST .../leave`, `DELETE .../members/{userId}`.

### 1.2 Xoá lịch sử phía mình

```http
POST /api/chat/conversations/{id}/clear-history
```

- Chỉ ảnh hưởng user gọi; người khác vẫn thấy.
- Sau đó `messages`, `context`, `search`, `attachments` chỉ trả tin **sau** thời điểm xoá; `lastMessage` ẩn nếu thuộc phần đã xoá; hội thoại được đánh dấu đã đọc.

### 1.3 Thu hồi tin

```http
POST /api/chat/messages/{id}/recall
```

- Chỉ **người gửi**, không giới hạn thời gian. Gọi lại là idempotent.
- Sau thu hồi: `isRecalled: true`, `text: ""`, `attachments: []`, bỏ ghim (`isDeleted` vẫn `false` — app hiển thị theo `isRecalled`). File đính kèm không tải được nữa.
- Nếu là tin mới nhất: `lastMessage` của hội thoại = `"Tin nhắn đã được thu hồi"`.
- Realtime: `MessageRecalled { conversationId, messageId }`.
- Tin đã thu hồi không forward / ghim / reaction / lưu được.

`DELETE /api/chat/messages/{id}` vẫn còn (xoá bởi người gửi, admin hội thoại, admin hệ thống; event `MessageDeleted`).

### 1.4 Tìm tin

Toàn cục (mọi hội thoại mình tham gia):

```text
GET /api/chat/messages/search?text={q}&conversationId={optional}&skip=0&take=20
```

- `text` ≥ 2 ký tự (không thì `Collaboration:SearchTextTooShort`), không phân biệt hoa thường; `take` ≤ 50.
- Bỏ tin đã xoá / thu hồi, bỏ phần lịch sử đã xoá, bỏ hội thoại đã ẩn/xoá.
- Response `{ "totalCount": n, "items": [ChatMessageDto] }`, mới nhất trước. Dùng `conversationId` + `id` để mở bằng `.../messages/{id}/context`.

Trong một hội thoại vẫn dùng được: `GET /api/chat/conversations/{id}/messages?keyword={text}&skip&take` (`take` ≤ 100).

### 1.5 Media / file / link

```text
GET /api/chat/conversations/{id}/attachments?kind=media|file|link&skip=0&take=30
```

- `media` = ảnh + video; `file` = file và audio; `link` = URL `http(s)://` tìm trong nội dung tin. `take` ≤ 100.
- Response `{ "totalCount": n, "items": [ConversationAttachmentItemDto] }`, mới nhất trước:

```json
{
  "messageId": "guid", "senderUserId": "guid", "createdAt": "2026-09-28T03:00:00Z",
  "url": "/api/chat/attachments/{attachmentId}",
  "attachmentId": "guid", "fileName": "a.png", "contentType": "image/png", "size": 1024, "kind": 1
}
```

Với `kind=link`: `url` là link, các field file là `null`.

### 1.6 Reaction

```http
PUT /api/chat/messages/{id}/reactions
{ "emoji": "👍" }

DELETE /api/chat/messages/{id}/reactions
```

- Mỗi user một reaction / tin; `PUT` lại là đổi emoji. `emoji` bắt buộc, ≤ 32 ký tự.
- Cả hai trả mảng reaction mới nhất: `[{ "emoji": "👍", "count": 2, "reactedByMe": true, "userIds": ["guid"] }]`, sắp theo `count` giảm dần.
- `ChatMessageDto.reactions` có sẵn trong mọi API trả tin.
- Realtime: `MessageReactionsChanged { conversationId, messageId, reactions }` (`reactedByMe` đã tính riêng cho từng người nhận).

### 1.7 Lưu tin

```http
PUT /api/chat/messages/{id}/save
{ "saved": true }

GET /api/chat/messages/saved?skip=0&take=20
```

- Lưu riêng từng user. `GET` trả `{ totalCount, items: [ChatMessageDto] }`, mới lưu trước, `take` ≤ 50.
- `ChatMessageDto.isSaved` cho biết tin đã lưu chưa.

### 1.8 Tắt thông báo hội thoại

```http
PUT /api/chat/conversations/{id}/mute
{ "muted": true }
```

- `ConversationDto.isMuted` trả trạng thái của user hiện tại.
- Event `ConversationMuteChanged { conversationId, muted }` gửi tới các thiết bị của chính user.
- Phạm vi — **chỉ tắt thông báo realtime SignalR**:
  - `ReceiveMessage` vẫn gửi (để màn chat cập nhật) nhưng có `isConversationMuted: true` → app **không** hiện toast / âm thanh / banner.
  - Giữ nguyên: **push vẫn gửi**, `unreadCount` và `/api/chat/unread-count` vẫn tăng.
  - Tin chat không tạo `NotificationReceived`, nên không có gì để chặn thêm.

### 1.9 Sửa thông tin / avatar nhóm

```http
PUT /api/chat/conversations/{id}
{ "name": "Tên nhóm", "description": "Mô tả" }
```

- Admin hội thoại. `name` bắt buộc ≤ 256, `description` ≤ 1024. Hội thoại 1-1: lỗi `Collaboration:DirectConversationInfoIsFixed`.
- Trả `ConversationDto`. Event `ConversationUpdated { conversationId }` gửi mọi thành viên → load lại `GET /api/chat/conversations/{id}`.

Avatar:

| Route | Ghi chú |
|---|---|
| `POST /api/chat/conversations/{id}/avatar` | multipart field `file`, ≤ 2 MB, `image/jpeg` / `image/png` / `image/webp`. Admin hội thoại hoặc admin hệ thống. Trả `ConversationDto` |
| `DELETE /api/chat/conversations/{id}/avatar` | Gỡ avatar |
| `GET /api/chat/conversations/{id}/avatar` | Ảnh; `404` nếu chưa có |

`ConversationDto.avatarUrl` = `/api/chat/conversations/{id}/avatar?v=...` (đổi `v` khi ảnh đổi) hoặc `null`. Cần gửi header `Authorization` khi tải.

Đổi tên riêng vẫn dùng được: `PUT .../name` `{ "name" }`.

### 1.10 Body request

| Route | Body |
|---|---|
| `POST /api/chat/conversations/{id}/members` | `{ "userIds": ["guid", "..."] }` — tối đa 100 |
| `PUT /api/chat/messages/{id}/pin` | `{ "pinned": true }` |
| `PUT /api/chat/conversations/{id}/pin` | `{ "pinned": true }` |
| `PUT /api/chat/conversations/{id}/members/{userId}/role` | `{ "role": 0 }` (`Member=0`, `Admin=1`) |
| `POST /api/chat/conversations/{id}/leave` | `{ "transferAdminTo": "guid" }` hoặc `null` |

### 1.11 `createdAt` / `joinedAt` — Đã sửa

Server gán thời gian khi tạo tin, thành viên, hội thoại, đính kèm. Migration `BackfillChatCreationTimes` sửa dữ liệu cũ (lấy thời gian từ id; không suy được thì lấy thời gian tạo hội thoại). App dùng thẳng `createdAt` / `joinedAt`.

### 1.12 Phân trang từ tin mới nhất

```text
GET /api/chat/conversations/{id}/messages?take=50                              # trang đầu
GET /api/chat/conversations/{id}/messages?beforeMessageId={oldestId}&take=50   # trang cũ hơn
```

- `items` mới nhất trước (`items[0]` mới nhất). `take` ≤ 100.
- Khi có `beforeMessageId`, server bỏ qua `skip`; `totalCount` = số tin **cũ hơn** `beforeMessageId`. Dừng khi trả ít hơn `take`.
- Ổn định cả khi nhiều tin cùng thời điểm (so thêm theo `id`).

`.../messages/{id}/context?before&after` vẫn dùng để nhảy tới một tin.

### 1.13 Không có `totalCount`

| Route | Kiểu trả | Tổng |
|---|---|---|
| `GET /api/chat/conversations?type&pinnedOnly&skip&take` | Mảng `ConversationDto[]`, `take` ≤ 100, sắp theo `lastMessageAt` giảm dần | Chưa đọc: `GET /api/chat/unread-count` → `int` |
| `GET /api/notifications?unreadOnly&skip&take&from&toExclusive&filter&isRead` | Mảng `NotificationDto[]`, `take` ≤ 100 | `GET /api/notifications/count?...` → `int` |

Phân trang: tăng `skip` cho đến khi số phần tử trả về < `take`.

---

## 2. Tài khoản / push

| # | Chức năng | Trạng thái |
|---|---|---|
| 2.1 | Đăng ký push token | Đã có — **bắt buộc đổi endpoint** |
| 2.2 | Huỷ push token khi logout | Đã có |
| 2.3 | Hồ sơ đầy đủ | Đã có (`userCode`, `dob`, `gender`: không làm) |
| 2.4 | Picker user > 50 | Đã có |
| 2.5 | Tự đăng ký tài khoản | Không làm — đã tắt |

### 2.1 Đăng ký push token

```http
POST /api/notifications/devices
{ "token": "fcm-or-apns-token", "platform": "android" }
```

- `token` bắt buộc ≤ 2048; `platform` ≤ 32.
- Response `PushDeviceDto { id, platform, isActive }`.
- Idempotent theo `token`: gọi lại khi token đổi hoặc user khác đăng nhập cùng máy → token chuyển sang user hiện tại.
- Quyền: `Collaboration.Realtime`.

### 2.2 Huỷ push token

```http
POST /api/notifications/devices/unregister
{ "token": "fcm-or-apns-token" }
```

- Chỉ huỷ token thuộc user hiện tại; token lạ / đã huỷ vẫn trả thành công (idempotent). Body rỗng.
- Gọi lúc logout, **trước** khi xoá access token.

### 2.3 Hồ sơ

```http
GET /api/identity/my-profile-summary
```

```json
{
  "id": "guid", "userName": "nv01", "email": "", "name": "An", "surname": "Nguyễn",
  "phoneNumber": "", "displayName": "Nguyễn An",
  "avatarUrl": "/api/identity/users/{id}/avatar",
  "departments": [ { "id": "guid", "name": "Khoa Nội", "isPrimary": true } ],
  "positionId": "guid", "positionName": "Trưởng khoa"
}
```

- `avatarUrl` `null` nếu chưa có ảnh. `positionId` / `positionName` `null` nếu chưa gán.
- `departments` rỗng nếu chưa gán hoặc Organization service lỗi (không làm hỏng cả response).
- Chỉ cần đăng nhập (không cần role đọc danh mục).
- `userCode`, `dob`, `gender`: **không trả về**.

Chỉ cần phòng ban: `GET /api/organization/user-departments/mine` → `[{ userId, departmentId, departmentName, positionId, positionName }]`, phòng chính đứng đầu. Chỉ cần đăng nhập. Xem [11-lookups.md](11-lookups.md).

Sửa hồ sơ vẫn qua `GET|PUT /api/account/my-profile` (ABP chuẩn), xem [01-account.md](01-account.md).

### 2.4 Picker người dùng

```text
GET /api/chat/contacts/page?search={text}&skip=0&take=50
```

- Response `{ totalCount, items: [{ id, userName, displayName, isActive, surname, name, phoneNumber, avatarUrl }] }`.
- `take` ≤ 50/trang. Chỉ user active, không gồm chính mình. Tham số `skip`/`take` (không phải `skipCount`/`maxResultCount`).
- Tra tên theo id: `GET /api/chat/contacts/lookup?userIds=a&userIds=b` (≤ 200 id).

### 2.5 Tự đăng ký — Không làm

Setting `Abp.Account.IsSelfRegistrationEnabled` mặc định `false` trên AuthServer và Platform (nơi gateway route `/api/account/**`), nên `POST /api/account/register` bị từ chối. App **bỏ** màn/nút đăng ký.

---

## 3. Văn bản / ký số / quy trình

Prefix `/api/documents`, `/api/workflows`, `/api/signing`. Chi tiết: [03-documents.md](03-documents.md), [04-workflows.md](04-workflows.md).

| # | Chức năng | Trạng thái |
|---|---|---|
| 3.1 | Duyệt kèm ghi chú | Đã có (giữ như hiện tại) |
| 3.2 | Văn bản tôi đã ký/xử lý | Đã có |
| 3.3 | Lọc theo đơn vị, quy trình, hình thức xử lý | Đã có |
| 3.4 | `isViewed`, `sentAt` | Đã có |
| 3.5 | Quá hạn | Đã có |
| 3.6 | Chữ ký theo id | Đã có |
| 3.7 | queue / credentials / bước hiện tại | Đã có |
| 3.8 | Thống kê ký cho dashboard | Đã có |
| 3.9 | Lọc hồ sơ theo user | Đã có |

### 3.1 Duyệt kèm ghi chú

Bước **ký**:

1. `POST /api/signing/attempts`

   ```json
   { "documentId": "guid", "fileId": "guid", "kind": 0, "idempotencyKey": "uuid",
     "signatureId": "guid|null", "placeholder": null, "signerName": null, "note": "Đồng ý" }
   ```

   `kind`: `Electronic=0`, `RemoteCa=1`, `Hsm=2`, `UsbToken=3`. Ghi chú điền vào placeholder `<<NoteContentNN>>` / `<<NoteContent>>`.

2. `POST /api/workflows/tasks/{taskId}/decision`

   ```json
   { "approve": true, "comment": "Đồng ý", "idempotencyKey": "uuid", "return": false,
     "signingAttemptId": "guid", "signingFileId": "guid" }
   ```

Bước **xử lý**: chỉ gọi bước 2, ghi chú trong `comment`, không gửi `signingAttemptId` / `signingFileId`.

### 3.2 Văn bản tôi đã ký/xử lý

```text
GET /api/signing/history?skip=0&take=20&from&toExclusive&decision=Approved
```

- Chỉ task **do tôi quyết định** (`Approved` / `Rejected` / `Returned`), mới quyết định trước. `from` / `toExclusive` lọc theo thời điểm quyết định.
- `decision`: tên hoặc số (`Approved=1`, `Rejected=2`, `Returned=4`); giá trị khác → `400`.
- `take` ≤ 100 (mặc định 20).
- Response `{ "totalCount": n, "items": [{ document, task, instance, definition, canDelete: false }] }` — cùng item với `queue-page`; `task` là **task của tôi**.

### 3.3 Lọc văn bản

`GET /api/documents` thêm:

| Param | Ý nghĩa |
|---|---|
| `organizationUnitId` | Đơn vị ban hành (`DocumentDto.organizationUnitId`) |
| `workflowDefinitionId` | Văn bản có hồ sơ chạy quy trình này (kể cả hồ sơ con của văn bản lưu trữ) |
| `processingMethodId` | Hình thức xử lý — danh mục `GET /api/organization/master-data?type=ProcessingMethod` |

Văn bản có field mới `processingMethodId`:

- Tạo: `POST /api/documents` thêm `"processingMethodId": "guid|null"`.
- Sửa: `PUT /api/documents/{id}` — không gửi / `null` = **giữ nguyên**; `"00000000-0000-0000-0000-000000000000"` = xoá.

`mine` vẫn **không có tác dụng** — lọc theo nguồn bằng `sourceType`.

### 3.4 `isViewed`, `sentAt`

`DocumentDto` (list và chi tiết) thêm:

- `isViewed`: user hiện tại đã ghi nhận xem (`POST /api/documents/{id}/activity` `{ "action": "VIEWED" }`).
- `sentAt`: lần gửi gần nhất (`null` nếu chưa gửi). Dùng cùng `isSent`.

### 3.5 Quá hạn

`ApprovalTaskDto.isOverdue` = `status == Pending && dueAt < now` (server tính lúc trả response). Có trong mọi `task` / `instance.tasks`.

Danh sách quá hạn: `GET /api/signing/queue-page?status=Overdue`.

### 3.6 Chữ ký theo id

```text
GET /api/signing/signatures/{id}
```

→ `UserSignatureDto`; `404` nếu không phải chữ ký của mình. Ảnh: `GET /api/signing/signatures/{id}/content`.

### 3.7 queue-page, credentials, bước hiện tại

**`GET /api/signing/queue-page`**

| Param | Giá trị |
|---|---|
| `inbox` | `toMe`, `byMe`, khác = tất cả. Không phân biệt hoa thường |
| `status` | `Pending`, `InProgress`, `Overdue`, `Completed`, `Approved`, `Rejected`, `Returned`, `Cancelled`. **Không** phân biệt hoa thường; giá trị lạ = không lọc |
| `search` | text |
| `from`, `toExclusive` | datetime |
| `dateField` | `WorkflowDeadline` = theo hạn task; mặc định theo ngày tạo |
| `submitterId`, `fromUserIds` | guid / guid[] |
| `skip`, `take` | mặc định `take=20` |

Response `{ totalCount, countAll, countToMe, countByMe, items: [{ document, task, instance, definition, canDelete }] }`.

**`GET /api/signing/credentials/current`** trả **mảng** `SigningCredentialDto[]`.

**Bước hiện tại**: dùng `instance.currentStepCode` (mã bước của task `Pending` đầu tiên; `null` khi hồ sơ đã xong), tra `definition.steps[].code`. **Không** dùng `currentStep` làm index (hồ sơ import có giá trị kiểu `1002`).

`ApprovalTaskStatus`: `Pending=0`, `Approved=1`, `Rejected=2`, `Cancelled=3`, `Returned=4`. `WorkflowInstanceStatus`: `Running=0`, `Completed=1`, `Rejected=2`, `Cancelled=3`, `Returned=4`.

### 3.8 Thống kê ký

```text
GET /api/signing/stats?from&toExclusive
```

```json
{ "pending": 5, "approved": 12, "rejected": 1, "returned": 2, "overdue": 1 }
```

- Của **user hiện tại**, đếm theo task. Không cần quyền báo cáo.
- `pending` / `overdue`: task đang chờ giao cho tôi (bỏ bước xem, chỉ hồ sơ đang chạy) — **không** lọc theo ngày.
- `approved` / `rejected` / `returned`: task tôi quyết định trong `[from, toExclusive)` (không truyền = toàn bộ).

### 3.9 Lọc hồ sơ theo user

```text
GET /api/workflows/instances/page?scope=mine&status&documentId&skip=0&take=20
```

| `scope` | Ý nghĩa |
|---|---|
| `all` (mặc định) | Như cũ: hồ sơ mình được xem (role `admin` / `lanhdao` thấy tất cả) |
| `mine` | Có task giao cho tôi hoặc tôi đã quyết định |
| `decidedByMe` | Có task tôi đã quyết định |

- Không phân biệt hoa thường; giá trị khác → `400`.
- `take` ≤ 100 (mặc định 20). Response `{ "totalCount": n, "items": [WorkflowInstanceDto] }`, mới tạo trước.
- `GET /api/workflows/instances` (mảng, cho Web) cũng nhận `scope`, `skip`, `take` (≤ 200).

---

## 4. Dự án / công việc

Prefix `/api/projects`, `/api/project-tasks`. Chi tiết: [05-projects-tasks.md](05-projects-tasks.md).

| # | Chức năng | Trạng thái |
|---|---|---|
| 4.1 | File đính kèm vào task | Đã có |
| 4.2 | Đổi vai trò thành viên | Đã có |
| 4.3 | Lọc server-side | Đã có |
| 4.4 | Assignees + tiến độ trong list | Đã có |
| 4.5 | `note`, `purpose` | Đã có |

### 4.1 File đính kèm task

| Route | Ghi chú |
|---|---|
| `POST /api/project-tasks/{id}/files` | multipart field `file`, ≤ 25 MB. Thành viên task (quyền `WorkManagement.ProjectTasks`). Trả `ProjectTaskFileDto` |
| `GET /api/project-tasks/{id}/files/{fileId}` | Tải file (thành viên task) |
| `DELETE /api/project-tasks/{id}/files/{fileId}` | Người upload, người tạo task, chủ dự án hoặc admin (`Work:CannotDeleteOthersFile`) |

`GET /api/project-tasks/{id}` thêm `files`:

```json
[ { "id": "guid", "fileName": "bao-cao.pdf", "contentType": "application/pdf", "size": 1024,
    "uploadedByUserId": "guid", "createdAt": "2026-09-28T03:00:00Z", "canDelete": true } ]
```

Gắn văn bản có sẵn (`POST .../documents`) vẫn giữ nguyên — Web vẫn dùng.

### 4.2 Đổi vai trò thành viên

```http
PUT /api/projects/{id}/members/{memberId}
{ "role": "Manager" }
```

- `role`: `Manager` | `Supervisor` | `Member` (phân biệt hoa thường). Chỉ chủ dự án (hoặc admin). Không đổi được role của chủ dự án (`Work:CannotChangeOwnerRole`).
- Giữ nguyên `memberId`. Trả `ProjectMemberDto`.

### 4.3 Lọc server-side

| Route | Param mới |
|---|---|
| `GET /api/projects` | `from`, `to` — dự án có `[startDate, endDate]` giao với `[from, to]`; `ownerDepartmentId` |
| `GET /api/project-tasks` | `from`, `to` — theo `dueDate` (gồm cả hai đầu); `priority`; `parentTaskId` (task con trực tiếp); `rootOnly=true` (chỉ task gốc, bị bỏ qua nếu có `parentTaskId`); `assigneeUserId` |

Param cũ giữ nguyên (`filter`, `status`, `projectId`, `skip`, `take` ≤ 100). Response `{ totalCount, items }`.

### 4.4 Assignees + tiến độ

- `ProjectTaskDto.assigneeUserIds: guid[]` — có trong list, chi tiết dự án, chi tiết task.
- `ProjectDto.progressPercent` = trung bình `progressPercent` các task **gốc**, làm tròn (không có task → 0). Có trong list và `GET /api/projects/{id}`.

### 4.5 `note`, `purpose`

- Giao việc: `POST /api/project-tasks/{id}/assignments` `{ "userId", "assignmentType", "note": "..." }`.
- Gắn văn bản: `POST /api/project-tasks/{id}/documents` `{ "documentId", "documentCode", "note": "...", "purpose": "REPORT" | "REFERENCE" }`.
- `note` ≤ 1000 (tự trim; rỗng = `null`). `purpose` không phân biệt hoa thường, mặc định `REFERENCE`; giá trị khác → `Work:InvalidTaskDocumentPurpose`.
- DTO đọc tương ứng: `TaskAssignmentDto.note`, `TaskDocumentReferenceDto.note` / `purpose`.

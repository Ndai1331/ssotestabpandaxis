# 13 — Phản hồi file đề xuất `new-api-request.md`

Trả lời từng mục của file "Đề xuất API mới cho mobile" (mục 0–5). Đối chiếu theo source ngày 2026-09-28, commit `9bffb94` (`[all] build 199`).

- Contract đầy đủ (body, response, lỗi) của mục 1–4: [12-api-request-status.md](12-api-request-status.md) và file module được dẫn trong từng dòng.
- Route mới chỉ chạy sau khi server deploy build 199 **và** chạy migration (Collaboration → Document → WorkManagement). Trước đó gateway trả `404` hoặc thiếu field.

Trạng thái dùng trong file:

| Trạng thái | Ý nghĩa với mobile |
|---|---|
| **Như đề xuất** | Làm đúng route/field app đề xuất |
| **Đổi** | Có làm nhưng route, param hoặc hành vi khác đề xuất → app **phải** làm theo cột "Dùng" |
| **Bỏ** | Không làm; xem ghi chú lý do và cách xử lý phía app |
| **Docs** | Không đổi server; chỉ chốt contract trong docs |

---

## Tóm tắt

**Bỏ (không làm):**

| # | Mục | Lý do / app làm gì |
|---|---|---|
| 1.13 | `{ totalCount, items }` cho conversations / notifications | Giữ mảng để không phá Web. Phân trang bằng `skip`/`take` tới khi trả ít hơn `take`; tổng dùng `/api/chat/unread-count`, `/api/notifications/count` |
| 2.3 (một phần) | `userCode`, `dob`, `gender` | Sản phẩm quyết định không trả. App bỏ 3 field này khỏi UI |
| 2.5 | Tự đăng ký tài khoản | Sản phẩm quyết định không cho tự đăng ký; server đã tắt. App **bỏ** màn/nút đăng ký (bỏ mock) |
| 3.1 (một phần) | Endpoint `approve-with-note` riêng | Không làm endpoint mới; dùng luồng ký + quyết định có sẵn (xem 3.1) |
| 3.5 (một phần) | Endpoint `check-and-handle-overdue` | Không làm; server trả sẵn `isOverdue` |
| 5 | Các module ngoài contract | Server mới không có các endpoint cũ; ngoài phạm vi đợt này (xem mục 5) |

**Đổi so với đề xuất (mobile bắt buộc cập nhật):**

| # | Đề xuất | Dùng |
|---|---|---|
| 1.8 | Mute tắt thông báo | Chỉ tắt thông báo realtime trong app (SignalR); **push vẫn gửi**, unread vẫn tăng |
| 1.12 | `order=desc` hoặc `before={messageId}` | `beforeMessageId={oldestId}`; `items` mới nhất trước |
| 2.2 | `DELETE /api/notifications/devices/{deviceId}` | `POST /api/notifications/devices/unregister` `{ "token" }` |
| 2.3 | Thêm field vào `GET /api/account/my-profile` | Route mới `GET /api/identity/my-profile-summary` |
| 2.4 | Nâng `take` của `/api/chat/contacts` | Route `GET /api/chat/contacts/page?search&skip&take` |
| 3.1 | `approve-with-note` | `POST /api/signing/attempts` (có `note`) → `POST /api/workflows/tasks/{taskId}/decision` |
| 3.3 | Master-data `HandlingType` | Master-data `type=ProcessingMethod`; query `organizationUnitId`, `workflowDefinitionId`, `processingMethodId` |
| 3.5 | `check-and-handle-overdue` | Field `task.isOverdue`; list: `queue-page?status=Overdue` |
| 3.8 | `stats?from&to` | `stats?from&toExclusive` |
| 3.9 | Filter `mine` / `decidedByMe` trên `/instances` | `GET /api/workflows/instances/page?scope=mine\|decidedByMe` |
| 4.4 | `assignees`, `progress` | `assigneeUserIds` (chỉ id) trên task; `progressPercent` trên project |

---

## 0. Hạ tầng

| # | Trạng thái | Ghi chú |
|---|---|---|
| 0.1 | **Việc vận hành, không phải code** | Nginx production trước Gateway (`api-hcs.benhvien199.vn`) phải forward `Upgrade`/`Connection` cho `/hubs/`. Mẫu nginx trong repo (`deploy/ubuntu/nginx/hcs-proxy.inc`) đã có `proxy_http_version 1.1`, `Upgrade $http_upgrade`, `Connection $connection_upgrade` — cần áp cùng cấu hình lên nginx của server bệnh viện. Trong lúc chờ: app giữ fallback LongPolling |
| 0.2 | **Đã sửa** | Gateway nhận Bearer token của `hcs-mobile` (xác nhận 2026-09-27). Không cần làm gì thêm |

---

## 1. Chat

Prefix `/api/chat`. Chi tiết: [09-chat.md](09-chat.md), [12-api-request-status.md](12-api-request-status.md) mục 1.

| # | Trạng thái | Dùng | Ghi chú cho mobile |
|---|---|---|---|
| 1.1 | Như đề xuất | `DELETE /api/chat/conversations/{id}` | 1-1: ẩn + xoá lịch sử **phía mình**, hiện lại khi có tin mới. Nhóm: chỉ admin nhóm/admin hệ thống, xoá cho mọi người, event `ConversationDeleted`. Hội thoại dự án/task: lỗi `Collaboration:WorkConversationCannotBeDeleted` → app ẩn nút xoá với `type` 2, 3 |
| 1.2 | Như đề xuất | `POST /api/chat/conversations/{id}/clear-history` | Chỉ ảnh hưởng user gọi |
| 1.3 | Như đề xuất | `POST /api/chat/messages/{id}/recall` | Chỉ người gửi, **không giới hạn thời gian**. Tin thu hồi: `isRecalled: true`, `text: ""`, `attachments: []`, `isDeleted` vẫn `false`. Event `MessageRecalled`. `DELETE /api/chat/messages/{id}` vẫn là xoá (kiểm duyệt) |
| 1.4 | Như đề xuất | `GET /api/chat/messages/search?text&conversationId&skip&take` | `text` ≥ 2 ký tự, `take` ≤ 50. Bỏ `conversationId` = tìm toàn cục. Response `{ totalCount, items }` |
| 1.5 | Như đề xuất | `GET /api/chat/conversations/{id}/attachments?kind&skip&take` | `kind`: `media` (ảnh+video), `file` (file+audio), `link`. `take` ≤ 100 |
| 1.6 | Như đề xuất | `PUT` / `DELETE /api/chat/messages/{id}/reactions` | PUT body `{ "emoji": "👍" }`. Mỗi user 1 reaction/tin. `ChatMessageDto.reactions`; event `MessageReactionsChanged` |
| 1.7 | Như đề xuất | `PUT /api/chat/messages/{id}/save` `{ saved }`, `GET /api/chat/messages/saved?skip&take` | `ChatMessageDto.isSaved` |
| 1.8 | **Đổi** (phạm vi) | `PUT /api/chat/conversations/{id}/mute` `{ muted }` | Route như đề xuất, nhưng **chỉ tắt thông báo realtime trong app**: `ReceiveMessage` vẫn tới, kèm `isConversationMuted: true` → app không hiện toast/âm thanh/banner. **Push vẫn gửi**, `unreadCount` vẫn tăng. `ConversationDto.isMuted`; event `ConversationMuteChanged` |
| 1.9 | Như đề xuất | `PUT /api/chat/conversations/{id}` `{ name, description }`; `POST`/`DELETE`/`GET .../avatar` | Avatar multipart `file` ≤ 2 MB (jpeg/png/webp). `ConversationDto.avatarUrl` (tải kèm `Authorization`). Hội thoại 1-1 không sửa được. Event `ConversationUpdated` |
| 1.10 | Docs | `POST .../members` `{ "userIds": [...] }` (≤ 100); `PUT /api/chat/messages/{id}/pin` `{ "pinned": true }` | App bỏ đoán `{ userId }` |
| 1.11 | Đã sửa | – | `createdAt` / `joinedAt` đúng; dữ liệu cũ đã backfill bằng migration. App **bỏ** suy thời gian từ message id |
| 1.12 | **Đổi** | `GET .../messages?take=50` rồi `GET .../messages?beforeMessageId={oldestId}&take=50` | `items` mới nhất trước (`items[0]` mới nhất). Có `beforeMessageId` thì server bỏ qua `skip`. Dừng khi trả ít hơn `take`. App **bỏ** request `take=1` để tính ngược `skip` |
| 1.13 | **Bỏ** | Mảng như hiện tại | Xem tóm tắt |

Event SignalR mới cần xử lý: `MessageRecalled`, `MessageReactionsChanged`, `ConversationUpdated`, `ConversationDeleted`, `ConversationMuteChanged`.

---

## 2. Tài khoản / push

| # | Trạng thái | Dùng | Ghi chú cho mobile |
|---|---|---|---|
| 2.1 | Docs — **bắt buộc đổi endpoint** | `POST /api/notifications/devices` `{ "token", "platform" }` | Endpoint cũ `/api/app/user-push-device-token/register` **không còn**. Trả `{ id, platform, isActive }`. Idempotent theo token |
| 2.2 | **Đổi** | `POST /api/notifications/devices/unregister` `{ "token": "..." }` | Huỷ theo **token** (không theo `deviceId`). Gọi lúc logout, **trước** khi xoá access token. Token lạ/đã huỷ vẫn trả thành công |
| 2.3 | **Đổi** + một phần **Bỏ** | `GET /api/identity/my-profile-summary` | `my-profile` giữ nguyên (dùng để sửa hồ sơ). Route mới trả `id, userName, email, name, surname, phoneNumber, displayName, avatarUrl, departments[{ id, name, isPrimary }], positionId, positionName`. **Không có** `userCode`, `dob`, `gender`. Chỉ cần phòng ban: `GET /api/organization/user-departments/mine`. Chi tiết: [01-account.md](01-account.md) |
| 2.4 | **Đổi** | `GET /api/chat/contacts/page?search&skip&take` | Chỉ cần quyền chat, không cần quyền quản trị user. Chỉ trả user active, không gồm chính mình. `take` ≤ 50/trang, có `totalCount` → tải thêm bằng `skip` |
| 2.5 | **Bỏ** | – | Server tắt tự đăng ký (`POST /api/account/register` bị từ chối). App bỏ màn/nút đăng ký |

---

## 3. Văn bản / ký số / quy trình

Chi tiết: [03-documents.md](03-documents.md), [04-workflows.md](04-workflows.md), [12-api-request-status.md](12-api-request-status.md) mục 3.

| # | Trạng thái | Dùng | Ghi chú cho mobile |
|---|---|---|---|
| 3.1 | **Đổi** | Bước ký: `POST /api/signing/attempts` (có `note`) → `POST /api/workflows/tasks/{taskId}/decision` (kèm `signingAttemptId`, `signingFileId`). Bước xử lý: chỉ `decision` với `comment` | Không có endpoint `approve-with-note`. Ghi chú bước ký được in vào placeholder `<<NoteContent>>` trên PDF; bước xử lý chỉ lưu ghi chú, không ghi lên PDF. App bỏ thông báo "không hỗ trợ" |
| 3.2 | Như đề xuất | `GET /api/signing/history?skip&take&from&toExclusive&decision` | Chỉ task tôi đã quyết định, mới nhất trước. Item giống `queue-page` (`document, task, instance, definition`) → không cần fetch document từng cái |
| 3.3 | **Đổi** (tên) | `GET /api/documents?organizationUnitId&workflowDefinitionId&processingMethodId` | Danh mục hình thức xử lý: `GET /api/organization/master-data?type=ProcessingMethod` (**không** phải `HandlingType`). Văn bản có thêm `processingMethodId` (tạo/sửa được) |
| 3.4 | Như đề xuất | `DocumentDto.isViewed`, `DocumentDto.sentAt` | `isViewed` = user hiện tại đã ghi `POST /api/documents/{id}/activity` `{ "action": "Viewed" }` → app phải gọi activity khi mở xem. `sentAt` = lần gửi gần nhất |
| 3.5 | **Đổi** | `ApprovalTaskDto.isOverdue`; list: `GET /api/signing/queue-page?status=Overdue` | Không có `check-and-handle-overdue`. App bỏ tự tính từ `dueAt` |
| 3.6 | Như đề xuất | `GET /api/signing/signatures/{id}` | `404` nếu không phải chữ ký của mình |
| 3.7 | Docs | – | `queue-page`: `inbox` = `toMe`/`byMe`; `status` = `Pending`, `InProgress`, `Overdue`, `Completed`, `Approved`, `Rejected`, `Returned`, `Cancelled` (không phân biệt hoa thường). `credentials/current` trả **mảng**. Bước hiện tại: dùng field mới `instance.currentStepCode`, **không** dùng `currentStep` làm index |
| 3.8 | **Đổi** (param) | `GET /api/signing/stats?from&toExclusive` | Response `{ pending, approved, rejected, returned, overdue }` của user hiện tại. `pending`/`overdue` không lọc theo ngày |
| 3.9 | **Đổi** | `GET /api/workflows/instances/page?scope=mine&status&documentId&skip&take` | `scope`: `all` (mặc định), `mine` (có task giao cho tôi hoặc tôi đã quyết định), `decidedByMe`. Response `{ totalCount, items }`. App bỏ lọc trên máy |

---

## 4. Dự án / công việc

Chi tiết: [05-projects-tasks.md](05-projects-tasks.md), [12-api-request-status.md](12-api-request-status.md) mục 4.

| # | Trạng thái | Dùng | Ghi chú cho mobile |
|---|---|---|---|
| 4.1 | Như đề xuất (+ tải file) | `POST /api/project-tasks/{id}/files` (multipart `file` ≤ 25 MB), `GET`/`DELETE .../files/{fileId}` | Danh sách file nằm trong `GET /api/project-tasks/{id}` → `files[]` (`canDelete` cho biết được xoá không) |
| 4.2 | Như đề xuất | `PUT /api/projects/{id}/members/{memberId}` `{ "role": "Manager" }` | `Manager`/`Supervisor`/`Member` (phân biệt hoa thường). Giữ nguyên `memberId`. Chỉ chủ dự án/admin; không đổi role chủ dự án |
| 4.3 | Như đề xuất | `/api/projects?from&to&ownerDepartmentId`; `/api/project-tasks?from&to&priority&parentTaskId&rootOnly&assigneeUserId` | Dự án: lọc theo khoảng `[startDate, endDate]` giao `[from, to]`. Task: `from`/`to` theo `dueDate`. `take` ≤ 100 |
| 4.4 | **Đổi** (tên, dạng) | Task: `assigneeUserIds: guid[]`. Project: `progressPercent` | Chỉ trả **id** người được giao. Avatar: `GET /api/identity/users/{userId}/avatar`; tên: `GET /api/chat/contacts/lookup?userIds=...`. `progressPercent` dự án = trung bình task gốc (làm tròn) |
| 4.5 | Như đề xuất | Assignment `{ userId, assignmentType, note }`; gắn văn bản `{ documentId, documentCode, note, purpose }` | `purpose`: `REPORT` / `REFERENCE` (mặc định `REFERENCE`). `note` ≤ 1000 |

---

## 5. Module chưa có trong contract

Không làm trong đợt này. Server mới **không có** các endpoint cũ dưới đây → app gọi sẽ nhận `404`. Cần sản phẩm quyết định giữ hay bỏ module trước khi làm API thay thế.

| Module | Endpoint cũ | Trên server mới | App nên làm |
|---|---|---|---|
| Mẫu dự án | `/api/project-templates/list` | Không có | Ẩn màn |
| Lịch hẹn | `/api/appointments/*` | Không có | Ẩn màn |
| Nội dung cuộc họp | `/api/meeting-content` | Không có | Ẩn màn |
| Báo cáo điều hành | `/api/app/reports` | Không có route cũ. Có `/api/reports/*` (WorkManagement, cần quyền báo cáo) phục vụ trang báo cáo Web, chưa document cho mobile | Ẩn màn; nếu cần, đề xuất riêng để chốt contract từ `/api/reports/*` |

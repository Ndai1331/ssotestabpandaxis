# Phase 2 — Quản lý hội thoại (1.1, 1.2, 1.3, 1.8, 1.9)

Service: `services/collaboration/HCS.CollaborationService`. Prefix gateway `/api/chat` đã có.

## Route mới

| # | Method | Route | Body | Quyền |
|---|---|---|---|---|
| 1.1 | `DELETE` | `/api/chat/conversations/{id}` | – | 1-1: thành viên (ẩn phía mình, xem ghi chú); Nhóm: admin hội thoại hoặc admin hệ thống; Project/Task: không cho |
| 1.2 | `POST` | `/api/chat/conversations/{id}/clear-history` | – | Thành viên |
| 1.3 | `POST` | `/api/chat/messages/{id}/recall` | – | Người gửi |
| 1.8 | `PUT` | `/api/chat/conversations/{id}/mute` | `{ "muted": bool }` | Thành viên |
| 1.9 | `PUT` | `/api/chat/conversations/{id}` | `{ "name", "description" }` | Người có `canRename` |
| 1.9 | `POST` / `DELETE` | `/api/chat/conversations/{id}/avatar` | multipart `file` | Người có `canRename` |
| 1.9 | `GET` | `/api/chat/conversations/{id}/avatar` | – | Thành viên |

## Thiết kế

- **1.1 Xoá**
  - Nhóm: soft-delete `Conversation` (đã là `FullAuditedAggregateRoot`); phát `ConversationDeleted { conversationId }` cho mọi thành viên.
  - 1-1: không xoá cho người kia; tương đương 1.2 + ẩn khỏi danh sách của mình đến khi có tin mới (`ConversationMember.HiddenAt`).
- **1.2 Xoá lịch sử phía mình**: thêm `ConversationMember.HistoryClearedAt`. `SearchMessagesAsync`, `GetMessageContextAsync`, search toàn cục (phase 3) lọc `CreationTime > HistoryClearedAt` của user hiện tại. Reset `UnreadCount = 0`.
- **1.3 Thu hồi**: thêm `ChatMessage.RecalledAt`. Chỉ người gửi, **không giới hạn thời gian** (quyết định 2026-09-28); xoá nội dung + gỡ attachment khỏi DTO; phát `MessageRecalled { conversationId, messageId }`. `DELETE /messages/{id}` giữ nguyên cho kiểm duyệt (admin).
  - DTO `ChatMessageDto` thêm `isRecalled` (default `false`).
- **1.8 Mute** — chỉ tắt **thông báo realtime SignalR** (quyết định 2026-09-28): thêm `ConversationMember.IsMuted`. `ConversationDto` thêm `isMuted` (theo user hiện tại).
  - Hiện trạng: tin chat **không** phát `NotificationReceived`; toast chat trên Web dựa vào `ReceiveMessage` (`src/HCS.Blazor.Client/Components/NotificationToast.razor` dòng 149). `ReceiveMessage` cũng là nguồn cập nhật màn chat → **không** chặn `ReceiveMessage` ở server.
  - Server: `ReceiveMessage`, `MessageDeleted` gửi như cũ. `SignalRChatRealtimeNotifier.NotificationSentAsync` bỏ qua member muted nếu notification thuộc hội thoại đó (chặn trước cho trường hợp chat phát `NotificationReceived` sau này).
  - Thêm event `ConversationMuteChanged { conversationId, muted }` gửi tới các kết nối của chính user để đồng bộ giữa Web và mobile.
  - Client (Web `NotificationToast` + mobile): không hiện toast / âm thanh / banner khi `ReceiveMessage` thuộc hội thoại `isMuted`.
  - Giữ nguyên: bản ghi `/api/notifications`, push (`PushDelivery`), `UnreadCount` và `/api/chat/unread-count`.
- **1.9 Sửa nhóm**: `Conversation.UpdateInfo(name, description)`; avatar lưu blob như chat attachment (ảnh ≤ 2 MB, jpeg/png/webp). `ConversationDto` thêm `avatarUrl` (null nếu không có). Phát `ConversationUpdated { conversationId }`.

## Migration

`ConversationMember`: `HistoryClearedAt`, `HiddenAt`, `IsMuted`. `ChatMessage`: `RecalledAt`. `Conversation`: `AvatarBlobName`, `AvatarContentType`.

## Files

- `Domain/CollaborationEntities.cs`, `Data/CollaborationDbContext.cs`, `Migrations/*`
- `Application/CollaborationAppService.cs`, `Api/ChatController.cs`
- `Contracts/CollaborationContracts.cs` (DTO field mới có default)
- `Hubs/ChatHub.cs` + notifier (`ConversationDeleted`, `ConversationUpdated`, `MessageRecalled`)
- `Hubs/ChatHub.cs` (`SignalRChatRealtimeNotifier`): event `ConversationMuteChanged`; bỏ `NotificationReceived` cho member muted
- Web: `NotificationToast.razor` bỏ toast cho hội thoại muted; `CollaborationClient.cs` gọi route mute
- Web: `CollaborationClient.cs` chỉ cần xử lý event mới nếu muốn parity (không bắt buộc phase này)

## Done khi

- Mỗi route có test quyền (thành viên/không thành viên/admin) + happy path.
- Web chat cũ vẫn chạy (route cũ không đổi).

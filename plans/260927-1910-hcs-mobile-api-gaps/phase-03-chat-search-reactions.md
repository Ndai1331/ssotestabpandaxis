# Phase 3 — Tìm kiếm, đính kèm, reaction, lưu tin (1.4, 1.5, 1.6, 1.7)

Service: `services/collaboration/HCS.CollaborationService`. Phụ thuộc phase 1 (thứ tự theo `CreationTime`) và phase 2 (`HistoryClearedAt`).

## Route mới

| # | Method | Route | Ghi chú |
|---|---|---|---|
| 1.4 | `GET` | `/api/chat/messages/search?text&conversationId&skip&take` | `conversationId` bỏ trống = mọi hội thoại mình là thành viên. `text` bắt buộc ≥ 2 ký tự. `take` ≤ 50. Trả `{ totalCount, items: ChatMessageDto[] }`, mới nhất trước |
| 1.5 | `GET` | `/api/chat/conversations/{id}/attachments?kind&skip&take` | `kind`: `media` (Image+Video), `file` (File+Audio), `link`. Trả `{ totalCount, items }` |
| 1.6 | `PUT` | `/api/chat/messages/{id}/reactions` | `{ "emoji": "👍" }` — mỗi user 1 reaction/tin (đặt lại = thay) |
| 1.6 | `DELETE` | `/api/chat/messages/{id}/reactions` | Gỡ reaction của mình |
| 1.7 | `PUT` | `/api/chat/messages/{id}/save` | `{ "saved": bool }` |
| 1.7 | `GET` | `/api/chat/messages/saved?skip&take` | Tin mình đã lưu, mới lưu trước |

## Thiết kế

- **1.4**: tái dùng `ILike` + index trigram trên `Text` đã có (`CollaborationDbContext.cs` dòng 65). Join `ConversationMembers` của user, lọc `HistoryClearedAt`, bỏ tin deleted/recalled. Kết quả kèm `conversationId` (đã có trong `ChatMessageDto`).
- **1.5**:
  - `media`/`file`: query `MessageAttachments` theo `ConversationId` + `Kind` (index `(ConversationId, CreationTime)` đã có). Item: `{ id, messageId, fileName, contentType, size, kind, createdAt, uploadedByUserId }`.
  - `link`: không có bảng link. Làm đơn giản: tin có `Text ILIKE '%http%'`, trích URL phía server bằng regex, item `{ messageId, url, createdAt, senderUserId }`.
- **1.6**: entity `ChatMessageReaction (MessageId, UserId, Emoji ≤ 16)`, unique `(MessageId, UserId)`. `ChatMessageDto` thêm `reactions: [{ emoji, count, reactedByMe }]` (default rỗng). Phát `MessageReactionsChanged { conversationId, messageId, reactions }`.
- **1.7**: entity `ChatSavedMessage (UserId, MessageId, SavedAt)`, unique `(UserId, MessageId)`. `ChatMessageDto` thêm `isSaved` (theo user). Chỉ lưu được tin trong hội thoại mình còn là thành viên.

## Migration

Bảng `CollaborationChatMessageReactions`, `CollaborationChatSavedMessages`.

## Files

- `Domain/CollaborationEntities.cs`, `Data/CollaborationDbContext.cs`, `Migrations/*`
- `Application/CollaborationAppService.cs` (hoặc tách `ChatSearchAppService` nếu file quá dài)
- `Api/ChatController.cs`, `Contracts/CollaborationContracts.cs`, `Hubs/ChatHub.cs`

## Done khi

- Search không lộ tin của hội thoại mình không tham gia (test).
- Reaction/save idempotent; DTO trả đúng `reactedByMe`/`isSaved`.

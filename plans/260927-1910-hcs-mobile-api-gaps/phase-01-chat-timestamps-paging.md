# Phase 1 — Sửa thời gian chat + phân trang tin (1.11, 1.12)

Service: `services/collaboration/HCS.CollaborationService`

## Nguyên nhân

`CollaborationAppService` inject thẳng `CollaborationDbContext` (dòng 17), không qua unit of work của ABP → `AbpDbContext.Initialize()` không chạy → ABP không gán `CreationTime`/`CreatorId`. Entity `Notification` đã tự gán `CreationTime` để né lỗi này (`CollaborationEntities.cs` dòng 156–157); chat chưa.

Ảnh hưởng: `ChatMessage`, `ConversationMember`, `Conversation`, `MessageAttachment` lưu `CreationTime = 0001-01-01`.

## Việc làm

1. **Gán thời gian tường minh** (theo pattern `Notification`), không đổi cách inject DbContext trong phase này:
   - Constructor `ChatMessage`, `ConversationMember`, `Conversation`, `MessageAttachment` nhận `DateTime creationTimeUtc` và gán `CreationTime`; gán `CreatorId` nơi có user.
   - Mọi chỗ `new ...` trong `CollaborationAppService`, `CollaborationAttachmentStore`, handler tạo hội thoại dự án truyền `clock.Now.ToUniversalTime()`.
2. **Backfill dữ liệu cũ** (migration SQL, idempotent, chỉ đụng dòng `CreationTime = '0001-01-01'`):
   - Message: suy từ timestamp trong sequential GUID (`Id`). Trước khi viết SQL, **xác minh format** bằng dữ liệu mẫu (so vài message mới tạo sau bước 1: `CreationTime` thật vs phần hex đầu của `Id`). Nếu không suy được chắc chắn → fallback `Conversation.LastMessageAt` cho message cuối và giữ thứ tự theo `Id`.
   - Member: `joinedAt` = min(`CreationTime` message đầu tiên của hội thoại, `Conversation.CreationTime`) nếu không có nguồn tốt hơn.
   - Conversation, attachment: suy từ `Id` như message; attachment có `MessageId` thì lấy theo message.
3. **Thứ tự ổn định**: `SearchMessagesAsync` thêm `.ThenByDescending(x => x.Id)` sau `OrderByDescending(CreationTime)`.
4. **Cursor theo tin** cho 1.12: thêm query `beforeMessageId` (Guid?) vào `GET /api/chat/conversations/{id}/messages`:
   - Có `beforeMessageId` → trả các tin cũ hơn tin đó (so `(CreationTime, Id)` như `GetMessageContextAsync`), mới nhất trước, `take` tối đa 100; bỏ qua `skip`.
   - Response giữ `PagedMessagesDto { totalCount, items }`.
   - Không đổi mặc định: không truyền cursor = hành vi cũ (mới nhất trước).

## Files

- `Domain/CollaborationEntities.cs`
- `Application/CollaborationAppService.cs`
- `Storage/CollaborationAttachmentStore.cs`
- `Api/ChatController.cs`
- `Migrations/*_BackfillChatCreationTime.cs`
- Test: `services/collaboration/**Tests` (tạo message → `CreatedAt` ≠ default; cursor trả đúng trang)

## Done khi

- Message/member mới trả `createdAt`/`joinedAt` đúng giờ.
- Không còn dòng `0001-01-01` sau migration (query kiểm tra trong DB lab).
- `GET .../messages?take=20` mới nhất trước; `beforeMessageId` phân trang không trùng/không sót.

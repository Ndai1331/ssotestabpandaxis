# 09 — Chat và thông báo

Clients: [`CollaborationClient.cs`](../../../src/HCS.Blazor.Client/Collaboration/CollaborationClient.cs), [`ChatRealtimeConnection.cs`](../../../src/HCS.Blazor.Client/Collaboration/ChatRealtimeConnection.cs)  
Prefix REST: `/api/chat`, `/api/notifications` (Web client đôi khi ghi `api/chat` không slash đầu — tương đương `{gateway}/api/chat`).

## Pages

| Route | Permission |
|---|---|
| `/chat`, `/chat/{id}`, `/chat1`, `/chat1/{id}` | `Collaboration.Chat` |
| `/notifications` | `Collaboration.Notifications` |

Toast global (`NotificationToast`) cũng gọi unread-count + mark read trên mọi page đã login.

## Màn hình → API (chat)

| Hành động Web | Method | Path |
|---|---|---|
| List hội thoại | `GET` | `/api/chat/conversations?skip&take` |
| Contacts tạo mới / thêm thành viên | `GET` | `/api/chat/contacts?search&take` (take max 50) |
| Lookup tên theo id | `GET` | `/api/chat/contacts/lookup?userIds=` |
| Mở hội thoại | `GET` | `/api/chat/conversations/{id}` |
| Quyền action | `GET` | `/api/chat/conversations/{id}/permissions` |
| Đánh dấu đọc | `POST` | `/api/chat/conversations/{id}/read` |
| Messages | `GET` | `/api/chat/conversations/{id}/messages?skip&take` |
| Pinned | `GET` | cùng path `pinnedOnly=true` |
| Context (scroll / nhảy tin) | `GET` | `/api/chat/conversations/{id}/messages/{messageId}/context?before&after` |
| Upload | `POST` | `/api/chat/conversations/{id}/attachments` max 25 MB |
| Gửi | `POST` | `/api/chat/messages` |
| Pin hội thoại | `PUT` | `/api/chat/conversations/{id}/pin` `{ pinned }` |
| Đổi tên | `PUT` | `/api/chat/conversations/{id}/name` `{ name }` |
| Thêm / xóa thành viên | `POST` `DELETE` | `.../members`, `.../members/{userId}` |
| Rời nhóm | `POST` | `.../leave` `{ transferAdminTo }` |
| Tạo hội thoại | `POST` | `/api/chat/conversations` |
| Pin / xóa / forward tin | `PUT` `DELETE` `POST` | `/api/chat/messages/{id}/pin`, `.../{id}`, `.../{id}/forward` |
| Download file | `GET` | `/api/chat/attachments/{id}` |
| Unread chat (toast) | `GET` | `/api/chat/unread-count` |

`GET /api/chat/contacts/page` có trên client, **ChatWorkspace không gọi**.

## REST chat

`ConversationType`: `User=0`, `Group=1`, `Project=2`, `Task=3`.  
`ConversationMemberRole`: `Member=0`, `Admin=1`.

Conversation: `id`, `type`, `name`, `description`, `projectId`, `taskId`, `lastMessage`, `lastMessageAt`, `unreadCount`, `isPinned`, `members: [{ userId, role, joinedAt }]`.

Permissions: `{ canSend, canManageMembers, canRename, canLeave, canModerateMessages }`.

### POST `/api/chat/conversations`

```json
{
  "type": 0,
  "name": null,
  "description": null,
  "targetUserId": "guid",
  "projectId": null,
  "taskId": null,
  "memberUserIds": []
}
```

1-1: `type=0` + `targetUserId`. Nhóm: `type=1` + `name` + `memberUserIds`. Dự án: `type=2` + `projectId` (xem [05-projects-tasks.md](05-projects-tasks.md)).

### POST `/api/chat/messages`

Text max 4000.

```json
{
  "conversationId": "guid",
  "text": "Nội dung",
  "clientMessageId": "uuid",
  "replyToMessageId": null,
  "attachmentIds": []
}
```

Message: `id`, `conversationId`, `senderUserId`, `text`, `createdAt`, `replyToMessageId`, `forwardedFromMessageId`, `isPinned`, `isDeleted`, `attachments[]`, `replyTo`, `forwardedFrom`.

Upload trả `{ id, fileName, contentType, size, kind }` rồi đưa `id` vào `attachmentIds`. `AttachmentKind`: `File=0`, `Image=1`, `Video=2`, `Audio=3`.

Forward: `{ "targetConversationId": "guid", "comment": null }`.

Context: `{ target, before, after, hasMoreBefore, hasMoreAfter }`; `before`/`after` max 50.

Leave: `{ "transferAdminTo": "guid-or-null" }`.

Contacts: `{ id, userName, displayName, isActive, surname, name, phoneNumber, avatarUrl }`. Lookup max 200 ids/lần.

## SignalR

```text
{gateway}/hubs/chat?access_token={access_token}
```

Permission hub: `Collaboration.Realtime`.

Server events:

| Event | Payload |
|---|---|
| `ReceiveMessage` | `ChatMessageDto` |
| `MessageDeleted` | `{ conversationId, messageId }` |
| `NotificationReceived` | `NotificationDto` |
| `PresenceChanged` | `{ userId, isOnline }` |

Client invoke `GetOnlineUserIds` → `guid[]` (Web gọi sau connect / reconnect).

REST là nguồn đồng bộ: sau reconnect load lại conversations + messages. Không dựa một mình event.

## Thông báo

| Hành động | Method | Path |
|---|---|---|
| List (page + workspace + toast) | `GET` | `/api/notifications?unreadOnly&skip&take&from&toExclusive` |
| Unread count (toast) | `GET` | `/api/notifications/unread-count` |
| Count (client có) | `GET` | `/api/notifications/count?unreadOnly` |
| Đọc một / tất cả | `POST` | `/api/notifications/{id}/read`, `/api/notifications/read-all` |

`take` max 100. DTO: `id`, `userId`, `title`, `body`, `link`, `isRead`, `createdAt`.

`title`/`body` có thể là key `Notification:...` (kèm separator `\u001f` + args). Mobile localize giống Web hoặc hiển thị raw.

`link` là route Web — map deep link, allow-list, không mở URL lạ.

`POST /api/notifications/devices` (push) **không** được page Web gọi. Chốt contract push trước khi native dùng.

Admin `POST /api/notifications` broadcast không thuộc scope user-facing này.

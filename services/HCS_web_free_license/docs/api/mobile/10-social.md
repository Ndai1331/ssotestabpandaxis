# 10 — Mạng xã hội

Page: [`SocialWorkspace.razor`](../../../src/HCS.Blazor.Client/Pages/SocialWorkspace.razor)  
Policy: `Collaboration.Social`  
Client: [`SocialClient.cs`](../../../src/HCS.Blazor.Client/Collaboration/SocialClient.cs)

## Pages

| Route | Việc |
|---|---|
| `/social` | Feed public |
| `/social/profile` | Trang cá nhân của tôi |
| `/social/profile/{userId}` | Profile người khác |

## Màn hình → API

| Hành động | Method | Path |
|---|---|---|
| Feed | `GET` | `/api/social/feed?skip&take&keyword&from&to&hashtag&postId` |
| Profile posts | `GET` | `/api/social/profile/posts?skip&take&visibility&authorUserId&...` |
| Profile tôi / người | `GET` | `/api/identity/social-profile`, `/api/identity/social-profile/{userId}` |
| Avatar tôi | `GET` | `/api/identity/profile/avatar` |
| Top tags / authors | `GET` | `/api/social/tags?take`, `/api/social/top-authors?take` |
| Reaction post/comment | `POST` | `/api/social/posts/{id}/reactions`, `/api/social/comments/{id}/reactions` |
| Share | `POST` | `/api/social/posts/{id}/shares` |
| Sửa / xóa post | `PUT` `DELETE` | `/api/social/posts/{id}` |
| Comments | `GET` `POST` | `/api/social/posts/{id}/comments` |
| Xóa comment | `DELETE` | `/api/social/comments/{id}` |
| Upload media / comment file | `POST` | `/api/social/uploads`, `/api/social/comment-uploads` max 25 MB |
| Hủy media chưa gắn | `DELETE` | `/api/social/media/{id}`, `/api/social/comment-media/{id}` |
| Tạo post | `POST` | `/api/social/posts` |
| Tìm người | `GET` | `/api/identity/social-people?search&take` |
| Filter phòng ban | `GET` | `/api/identity/organization-unit-lookup` |
| Thành viên phòng | `GET` | `/api/identity/organization-units/{id}/members?filter&skipCount&maxResultCount` |
| Enrich OU/chức vụ | `GET` | `/api/identity/organization-unit-lookup/users`, `/api/organization/user-departments` |
| Chat 1-1 từ profile | `POST` | `/api/chat/conversations` |
| Điểm đánh giá trên profile | `GET` | `/api/employee-ratings/summary?skip=0&take=1&userId=` |
| Chấm điểm | `POST` | `/api/employee-ratings` |

Download media: `GET /api/social/media/{id}`, `GET /api/social/comment-media/{id}` (URL Gateway + Bearer).

## Feed / posts

`take` max 50. `from`/`to` là `yyyy-MM-dd`. Profile `visibility` query: `public` / `internal` (lowercase string).

Post: `id`, `authorUserId`, `authorName`, `avatarUrl`, `text`, `visibility`, `createdAt`, `media[]`, `commentCount`, `linkPreview`, `reactions`, `shareCount`.

`SocialPostVisibility`: `Public=0`, `Internal=1`. Internal: Web chỉ hiện cho tác giả trên trang cá nhân.

### POST `/api/social/posts`

Text max 4000; tối đa 10 media; phải có text **hoặc** media.

```json
{
  "text": "Nội dung #tag",
  "visibility": 0,
  "mediaIds": ["guid"]
}
```

PUT sửa: `{ "text", "visibility" }` (không đổi media list trên Web).

Lỗi nghiệp vụ: `Collaboration:EmptySocialPost`, `Collaboration:TooManySocialMediaItems`, `Collaboration:InvalidSocialVisibility`.

## Comments

GET paged `{ totalCount, items }`. POST:

```json
{
  "text": "Bình luận",
  "parentCommentId": null,
  "attachmentIds": []
}
```

Text max 2000; tối đa 10 attachment; text hoặc file. Reply: `parentCommentId`.

## Reactions / share

```json
{ "reactionType": 0, "remove": false }
```

`Like=0`, `Love=1`, `Haha=2`, `Wow=3`, `Sad=4`, `Angry=5`. `remove: true` gỡ reaction.

Response `{ reactions: { totalCount, counts: [{ type, count }], currentUserReaction } }`.

Share: body `{}` → `{ postId, shareUrl, shareCount, alreadyShared }`. `shareUrl` là URL Web; mobile đổi thành deep link nếu cần.

## People / OU

`GET /api/identity/social-people?search=&take=` (take max 30). Search rỗng có thể trả `[]`.

Person: `userId`, `userName`, `displayName`, `email`, `phoneNumber`, `avatarUrl`, `positionName`, `departmentName`, `departmentId`, `positionId`.

Web enrich thêm OU + `GET /api/organization/user-departments` (chức vụ). Xem [11-lookups.md](11-lookups.md).

Members phòng: paged ABP `{ totalCount, items: [{ id, userName, fullName, email, isActive }] }` rồi `AttachOrganizationAsync`.

## Đánh giá nhân viên (lookup của social)

Không phải màn admin ratings. Web trên profile:

```http
GET /api/employee-ratings/summary?skip=0&take=1&userId={profileUserId}&from&to
POST /api/employee-ratings
```

POST `{ "targetUserId": "guid", "score": 1-5 }`. Trùng trong ngày: `409` code `Work:EmployeeRatingAlreadySubmitted`.

Cần permission `WorkManagement.EmployeeRatings`. Directory `/api/identity/employee-directory` **không** được social page gọi (dùng ở `/employee-ratings` — ngoài scope trừ khi tái sử dụng picker).

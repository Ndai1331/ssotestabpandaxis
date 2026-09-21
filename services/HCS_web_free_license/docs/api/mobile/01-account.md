# 01 — Quản lý tài khoản

Page Web: [`AccountManagement.razor`](../../../src/HCS.Blazor.Client/Pages/AccountManagement.razor)  
Client: [`AccountProfileClient.cs`](../../../src/HCS.Blazor.Client/Account/AccountProfileClient.cs)

## Pages

| Route | Permission | Hành vi Web |
|---|---|---|
| `/account` | Authenticated | Tab hồ sơ + đổi mật khẩu + tab chữ ký |
| `/user-signatures` | Authenticated | Redirect `/account?tab=signatures` |

Tab chữ ký nhúng `UserSignaturesPanel` → API signing trong [03-documents.md](03-documents.md).

## Màn hình → API

| Hành động Web | Method | Path |
|---|---|---|
| Mở trang | `GET` | `/api/account/my-profile` |
| Load avatar | `GET` | `/api/identity/profile/avatar` (`404` nếu chưa có) |
| Lưu hồ sơ | `PUT` | `/api/account/my-profile` |
| Đổi mật khẩu | `POST` | `/api/account/my-profile/change-password` |
| Upload avatar | `POST` | `/api/identity/profile/avatar` multipart `file` |
| Xóa avatar | `DELETE` | `/api/identity/profile/avatar` |
| Avatar user khác (social/chat) | `GET` | `/api/identity/users/{userId}/avatar` |

## Endpoints

### GET `/api/account/my-profile`

Response:

```json
{
  "userName": "admin",
  "email": "admin@abp.io",
  "name": "Admin",
  "surname": null,
  "phoneNumber": null,
  "isExternal": false,
  "hasPassword": true,
  "concurrencyStamp": "abc..."
}
```

`isExternal: true` nghĩa là user SSO — ẩn form đổi mật khẩu local nếu Web đang làm vậy.

### PUT `/api/account/my-profile`

Phải gửi `concurrencyStamp` mới nhất.

```json
{
  "userName": "admin",
  "email": "admin@abp.io",
  "name": "Nguyễn",
  "surname": "Văn A",
  "phoneNumber": "0900000000",
  "concurrencyStamp": "abc..."
}
```

Response: profile mới (kèm `concurrencyStamp` mới).

### POST `/api/account/my-profile/change-password`

```json
{
  "currentPassword": "old",
  "newPassword": "new"
}
```

Response: `204`.

### Avatar

```http
POST /api/identity/profile/avatar
Content-Type: multipart/form-data

file: <jpeg|png|webp|gif>, tối đa 2 MB
```

Response: `204`.

```http
GET /api/identity/profile/avatar
GET /api/identity/users/{userId}/avatar
DELETE /api/identity/profile/avatar
```

GET trả binary; `404` = chưa có avatar. Render từ bytes hoặc URL Gateway kèm Bearer — không lưu URL MinIO nội bộ.

## Ghi chú mobile

- Sau PUT profile, dùng `concurrencyStamp` mới cho lần sửa tiếp theo.
- Avatar Web reload sau upload; phát sự kiện `AvatarChanged` trên client.
- Chữ ký: xem [03-documents.md](03-documents.md) mục User signatures.

# Phase 4 — Push token, hồ sơ, tắt tự đăng ký (2.2, 2.3, 2.5)

## 2.2 Huỷ push token khi đăng xuất

Service: Collaboration. Entity `PushDeviceToken` đã có `Deactivate()` nhưng không ai gọi. `PushDeliveryWorker` đã chỉ lấy token `IsActive`. Schema không có `deviceId`, và token dài tới 2048 ký tự → không đặt token lên path.

| Method | Route | Body |
|---|---|---|
| `POST` | `/api/notifications/devices/unregister` | `{ "token": "..." }` |

- Chỉ deactivate token thuộc user hiện tại; token không tồn tại → `204` (idempotent).
- `POST /api/notifications/devices` trả thêm `{ id, platform, isActive }` thay vì body rỗng (không phá client cũ vì trước đó không có body).

Files: `Api/NotificationController.cs`, `Application/NotificationAppService.cs`, `Contracts/CollaborationContracts.cs`.

## 2.3 Hồ sơ cho mobile

Không sửa `GET /api/account/my-profile` (ABP chuẩn, Web dùng). Thêm endpoint tổng hợp ở Platform:

| Method | Route | Quyền |
|---|---|---|
| `GET` | `/api/identity/my-profile-summary` | Đã đăng nhập |

Response dự kiến:

```json
{
  "id": "guid", "userName": "", "email": "", "name": "", "surname": "", "phoneNumber": "",
  "displayName": "",
  "avatarUrl": "/api/identity/users/{id}/avatar | null",
  "departments": [{ "id": "guid", "name": "" }],
  "positionId": "guid | null", "positionName": "string | null"
}
```

- `departments`, `positionId`: đọc từ Organization (`user-mappings` / `user-departments`) qua HTTP nội bộ hoặc integration service — **không** yêu cầu quyền admin của caller vì chỉ đọc bản ghi của chính mình.
- `userCode`, `dob`, `gender`: **không trả về** (quyết định 2026-09-28) → không thêm extra property, không migration Platform.
- Gateway: `/api/identity/**` đã route sang Platform.

Files: `services/platform/HCS.PlatformService/Controllers/MyProfileSummaryController.cs` (mới).

## 2.5 Không cho tự đăng ký

Quyết định 2026-09-28: tài khoản do quản trị cấp. Hiện `Abp.Account.IsSelfRegistrationEnabled` không bị override (mặc định ABP = `true`), trang `/Account/Register` của auth-server vẫn truy cập được.

- Thêm `SettingDefinitionProvider` đặt default `AccountSettingNames.IsSelfRegistrationEnabled = false` (theo pattern `src/HCS.Domain/Identity/ChangeIdentityPasswordPolicySettingDefinitionProvider.cs`), đặt ở project có tham chiếu `Volo.Abp.Account.Application` (vd `src/HCS.Application`) và đảm bảo cả **Platform** lẫn **auth-server** nạp module chứa provider này.
- Kiểm tra bảng `AbpSettings` (DB lab + prod) không có giá trị `Abp.Account.IsSelfRegistrationEnabled = true` ghi đè; nếu có thì xoá/đặt `false`.
- Kết quả mong đợi: `/Account/Register` báo tắt đăng ký; `POST /api/account/register` trả lỗi `Volo.Account:SelfRegistrationDisabled` (và vẫn `401` ở gateway khi không có token).

## Done khi

- Unregister rồi gửi push → user không nhận (test service: token `IsActive=false`).
- `my-profile-summary` trả phòng ban/chức vụ cho user thường (không có quyền `UserMappings`).
- `/Account/Register` trên auth-server local không tạo được tài khoản.

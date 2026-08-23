# Phase 2 — Profile + password

## Context

ABP Account đã expose trên Platform: `GET/PUT /api/account/my-profile`, `ChangePasswordAsync`. Blazor Client chưa gọi. AuthServer Manage hiện là UI duy nhất.

## Overview

- Priority: P1
- Status: pending
- Estimate: 1 day
- Goal: section thông tin cá nhân + đổi mật khẩu trên `/account` qua BFF.

## Requirements

1. Client typed hoặc thin wrapper (`AccountProfileClient`) dùng HttpClient `"HCS.Bff"`:
   - Get/Update profile (`ProfileDto` / `UpdateProfileDto`)
   - Change password (`ChangePasswordInput`)
2. Form hiển thị/editable: họ (`Surname`), tên (`Name`), tên đăng nhập (`UserName` — read-only nếu ABP mặc định khóa), SĐT, email.
3. Username: ưu tiên read-only (tránh xung đột Identity uniqueness / SSO subject); nếu product cần sửa thì enable + validation server-side.
4. Đổi mật khẩu: current + new + confirm; disable + thông báo khi `!HasPassword` hoặc `IsExternal` (SSO).
5. ConcurrencyStamp gửi kèm update.
6. UI theo pattern form Blazorise hiện có (`HCSComponentBase`, notify success/error).

## Related files

- `src/HCS.Blazor.Client/Pages/AccountManagement.razor`
- `src/HCS.Blazor.Client/Account/AccountProfileClient.cs` (new) hoặc tương đương
- Packages/contracts Account đã có trong solution (HttpApi trên Platform)
- `test/HCS.HttpApi.Client.ConsoleTestApp/ClientDemoService.cs` — reference gọi `IProfileAppService`

## Implementation steps

1. Thêm client BFF gọi `/api/account/my-profile` (+ change-password endpoint ABP).
2. Bind form load on init; save profile; change password section riêng.
3. L10n labels/errors.
4. Test: local user update phone/email; external user thấy warning password.

## Success criteria

- User local sửa được Name/Surname/Phone/Email và đổi mật khẩu thành công qua BFF cookies.
- External user không bị lỗi opaque khi cố đổi password — có message rõ.

## Risks / security

- Không log password.
- Antiforgery: BFF handler hiện có phải áp dụng cho PUT/POST (giống DocumentClient).
- Không expose admin Identity endpoints cho self-service.

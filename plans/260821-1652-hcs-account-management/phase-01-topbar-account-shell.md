# Phase 1 — Topbar + account shell

## Context

Dropdown profile hiện hardcode trong [`HCSMainLayout.razor`](../../services/HCS_web_free_license/src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor): Workspace → `/workspace`, Manage → `https://auth.hcs.localhost/Account/Manage` (`target=_blank`).

## Overview

- Priority: P1
- Status: pending
- Estimate: 0.5 day
- Goal: bỏ Workspace khỏi dropdown; trỏ Manage vào page Blazor mới `@attribute [Authorize]` (chỉ cần login).

## Requirements

1. Xóa link `Auth:Workspace` trong user menu panel (giữ nav chính “Không gian làm việc” nếu vẫn cần).
2. Đổi Manage thành `<NavLink href="/account">` (hoặc `<a href="/account">`) same-tab, đóng menu sau click.
3. Tạo `Pages/AccountManagement.razor` route `/account`, `[Authorize]`, layout mặc định `HCSMainLayout`, skeleton sections (profile / password / avatar / signature) có thể trống tạm.
4. Cập nhật [`HCSMenuContributor.ConfigureUserMenu`](../../services/HCS_web_free_license/src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs): `Account.Manage` → `/account`, bỏ `target:_blank` và bỏ phụ thuộc `Bff:AccountUrl` cho item này (hoặc trỏ relative `/account`).
5. Thêm L10n keys page title nếu thiếu (`Account:*`).

## Related files

- `src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor`
- `src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs`
- `src/HCS.Blazor.Client/Pages/AccountManagement.razor` (new)
- `src/HCS.Domain.Shared/Localization/HCS/vi.json`, `en.json`
- `wwwroot/appsettings.json` — `Bff:AccountUrl` có thể giữ cho deep-link khác hoặc deprecate trong comment; layout không dùng nữa

## Implementation steps

1. Sửa dropdown markup.
2. Scaffold page + `PageTitle`.
3. Align menu contributor.
4. Smoke: login → mở menu → không thấy Workspace → Manage vào `/account` không rời host `hcs.localhost`.

## Success criteria

- Dropdown chỉ còn identity summary + Quản lý tài khoản + Đăng xuất.
- `/account` yêu cầu authenticated; anonymous redirect login.

## Risks

- ABP default user menu widgets khác layout: chỉ cần contributor + custom layout đồng bộ URL.

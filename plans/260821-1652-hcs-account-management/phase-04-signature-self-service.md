# Phase 4 — Signature self-service

## Context

`SigningController` gắn `[Authorize(Policy = Documents.Signing.Execute)]` cho list/upload/delete signatures. `SigningAppService.ResolveTargetUser` cũng `RequirePermission(SigningExecute)` kể cả self. `/user-signatures` page cùng policy. User thường không vào được dù chỉ quản lý chữ ký cá nhân.

## Overview

- Priority: P1
- Status: pending
- Estimate: 1 day
- Goal: self CRUD chữ ký chỉ cần authenticated; elevated vẫn quản lý hộ; embed panel trên `/account`.

## Requirements

1. Đổi auth chữ ký **metadata self**:
   - Controller: `[Authorize]` (authenticated) cho `GET/POST/DELETE …/signatures*`.
   - `ResolveTargetUser`: nếu target == current → chỉ `RequireUser`; nếu target khác → `RequirePermission(SigningExecute)` + `IsElevated` (giữ như hiện tại).
2. **Không** nới `POST /attempts`, reports, credentials — vẫn SigningExecute / Configure.
3. Thêm `GET /api/signing/signatures/{id}/content` (authenticated + ownership/elevated) để preview ảnh trong panel — mirror document `/content`.
4. `UserSignaturesPanel`: hiện thumbnail khi có content endpoint; vẫn reuse trên Administration (`UserId=`).
5. Embed panel trong `/account` (section chữ ký). Giữ `/user-signatures` hoặc nới `[Authorize]` cho đồng bộ; ưu tiên redirect `/user-signatures` → `/account#signatures` nếu muốn một cửa (optional, không bắt buộc).
6. DocumentClient: `GetSignatureContentAsync`.

## Related files

- `services/document/.../Controllers/SigningController.cs`
- `services/document/.../Signing/SigningAppService.cs`
- `services/document/.../Signing/SigningContracts.cs`
- `src/HCS.Blazor.Client/Documents/DocumentClient.cs`
- `src/HCS.Blazor.Client/Components/UserSignaturesPanel.razor`
- `src/HCS.Blazor.Client/Pages/AccountManagement.razor`
- `src/HCS.Blazor.Client/Pages/UserSignatures.razor`

## Implementation steps

1. Nới `ResolveTargetUser` + attribute controller.
2. Content download endpoint + client.
3. Panel preview + account embed.
4. Test: user không có SigningExecute vẫn upload/delete chữ ký own; không upload được cho user khác; ký document vẫn cần Execute.

## Success criteria

- Login-only user quản lý được chữ ký trên `/account`.
- Admin vẫn gán chữ ký user khác từ Administration.
- Ký văn bản không bị mở permission nhầm.

## Risks / security

- Không để anonymous đọc content chữ ký.
- Ownership check bắt buộc trên content GET.
- Giữ limit 2MB image.

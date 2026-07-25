# Phase 07 — Permission seed `Elsa.*` cho admin

**Goal:** Cấp permission Elsa (`Elsa.*`) cho role `admin` để truy cập Studio/Workflow API, dùng ABP Permission Management (giống pattern `260723-1530-abp-role-permission-seed`).

**Depends on:** phase-02 (Elsa module define permission group `Elsa.*`), phase-03 (Permission Management DB mapped).
**Owns files:** `services/abp-blazor/services/administration/hanhchinhso.AdministrationService/Data/AdministrationServiceDataSeeder.cs` (nơi seed role/permission hiện tại).

## Background
- Permission grants lưu ở DB `hanhchinhso_Administration` (Permission Management). AuthServer/host đọc qua mapped connection.
- Elsa Pro tự định nghĩa permission group (tên chính xác cần verify khi module chạy — thường prefix `Elsa.` / `ElsaPro.`). Verify bằng UI Permission Management hoặc bảng `AbpPermissionGrants` sau khi module load.

## Tasks
- [ ] Xác định tên permission Elsa thực tế: mở Blazor Admin → Roles → admin → Permissions → nhóm Elsa; hoặc grep constant trong package Elsa. Ghi lại danh sách (vd `Elsa.Workflows`, `Elsa.WorkflowInstances`, `Elsa.Activities`...).
- [ ] Trong `AdministrationServiceDataSeeder.SeedAsync()` (sau seed admin hiện có), cấp cho role `admin` các permission `Elsa.*` bằng `IPermissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, "admin", elsaPermissionNames, tenantId)`. Idempotent → an toàn chạy lại.
  - Nếu admin đã được cấp "tất cả" qua cơ chế `alwaysGranted`/super-admin thì có thể KHÔNG cần seed thủ công — verify trước (YAGNI: nếu admin auto-full thì phase này chỉ còn bước verify).
- [ ] (Optional) role khác (`lanhdao`/`bacsi`) — BỎ cho lab trừ khi user yêu cầu.

## Verify
- [ ] Restart AdministrationService → `AbpPermissionGrants` có ProviderKey `admin` + các permission `Elsa.*`.
- [ ] Login admin vào Studio (phase-05) → truy cập được workflow list/designer (không 403).
- [ ] Menu Blazor gate bằng permission Elsa (phase-06) hiển thị cho admin.

## Rollback
- Xóa block seed Elsa permission trong `AdministrationServiceDataSeeder`; xóa grants thừa trong `AbpPermissionGrants` nếu muốn.

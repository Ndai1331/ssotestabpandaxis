# Phase 06 — Blazor menu link → Elsa Studio

**Goal:** Thêm 1 menu item trong ABP Blazor chính mở Elsa Studio (`http://localhost:44396`) ở **tab mới** (`_blank`). Không nhúng — chỉ điều hướng (Option A).

**Depends on:** phase-05 (Studio URL tồn tại).
**Owns files:** `apps/blazor/hanhchinhso.Blazor.Client/Navigation/hanhchinhsoMenuContributor.cs` (+ localization json nếu thêm text).

## Tasks
- [ ] Trong `ConfigureMainMenuAsync(...)`, thêm menu item (đặt trong nhóm Administration hoặc top-level tùy UX; mặc định top-level sau Dashboard):
```csharp
context.Menu.AddItem(
    new ApplicationMenuItem(
        "WorkflowService.Studio",
        l["Menu:Workflow"],
        url: "http://localhost:44396",
        icon: "fa fa-project-diagram",
        target: "_blank",
        order: 3
    ).RequirePermissions(/* Elsa permission name, ví dụ "Elsa.Workflows" */)
);
```
- [ ] URL nên đọc từ config thay vì hardcode: thêm key `ElsaStudio:Url` vào `apps/blazor/hanhchinhso.Blazor/appsettings.json` (`http://localhost:44396`) và inject `IConfiguration` (contributor đã có `_configuration`) → `_configuration["ElsaStudio:Url"]`.
- [ ] Localization: thêm `"Menu:Workflow": "Workflow (Elsa Studio)"` vào `Localization/hanhchinhso/en.json` + `vi.json` ("Quy trình (Elsa Studio)"). *(Hoặc reuse `WorkflowServiceResource` — nhưng menu chính đang dùng `LanguageServiceResource`; đơn giản nhất thêm key vào resource hiện dùng.)*
- [ ] Permission gate: dùng đúng permission name Elsa từ phase-07 (`RequirePermissions`). Nếu chưa chắc tên permission → tạm `RequireAuthenticated()` và siết lại sau.

## Verify
- [ ] Rebuild + hard refresh Blazor `:44306`.
- [ ] User admin thấy menu "Workflow (Elsa Studio)"; click → mở tab mới `:44396`, Studio load.
- [ ] User không có quyền Elsa → không thấy menu (nếu đã gate permission).

## Rollback
- Xóa menu item + key localization + key `ElsaStudio:Url`.

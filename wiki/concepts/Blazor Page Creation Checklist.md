---
type: concept
title: "Blazor Page Creation Checklist"
created: 2026-06-27
updated: 2026-06-27
tags:
  - blazor
  - ui
  - checklist
status: evergreen
complexity: basic
related:
  - "[[Task9 Platform Overview]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Blazor Page Creation Checklist

**3 bước BẮT BUỘC** khi tạo page `.razor` mới. Thiếu bước nào = layout vỡ hoặc 403 Unauthorized.

## Bước 1 — Khai báo trong file `.razor`

```razor
@page "/your-route"
@layout PageLayout
@inherits BootstrapComponentBase
```

- `@layout PageLayout` → đúng cấu trúc Sidebar/Navigation
- `@inherits BootstrapComponentBase` → tránh thiếu Master Layout

## Bước 2 — Thêm vào `PageLayout.razor.cs`

Thêm `MenuItem` vào hàm tạo menu:

```csharp
new MenuItem
{
    Text = "Tên Menu",
    Url = "/your-route",
    Icon = "fa fa-icon"
}
```

## Bước 3 — Khai báo trong `UrlAuthorizationService.cs`

**Copy y chang** đoạn menu ở bước 2 và chèn vào hàm `BuildMenusForRole`:

> 🔴 **Nếu bỏ qua bước này:** URL không nằm trong whitelist → user bấm link bị chặn với màn hình **Không có quyền truy cập (Unauthorized)**.

## Vị trí Files

```
services/ui/src/BootstrapBlazor.Server/
├── Components/Task9/<TênPage>.razor        ← Bước 1
├── Components/Layout/PageLayout.razor.cs   ← Bước 2
└── Services/UrlAuthorizationService.cs     ← Bước 3 (BẮT BUỘC)
```

## Pattern Tham khảo

- Page mẫu: `DomainByPic.razor` + `DomainByPic.razor.cs`
- Service mẫu: `SeoCostOverviewService` (MySqlConnector raw SQL, đọc từ seo_data)

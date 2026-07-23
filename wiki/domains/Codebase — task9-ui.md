---
type: domain
title: "Codebase — task9-ui"
created: 2026-06-28
updated: 2026-06-28
tags:
  - task9/ui
  - codebase
  - blazor
status: mature
related:
  - "[[Task9 Platform Overview]]"
  - "[[Codebase — task9-api]]"
  - "[[Blazor Page Creation Checklist]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Codebase — task9-ui

Frontend .NET 9 **Blazor Server** (BootstrapBlazor v9), port 8080, prod `task9.pro`. Repo `services/ui`, project chính `src/BootstrapBlazor.Server/`.

## Cây thư mục chính (`src/BootstrapBlazor.Server/`)
| Folder | Nội dung |
|--------|----------|
| `Components/Task9/` | **~135 file `.razor`** — toàn bộ page nghiệp vụ. Mỗi page có bộ `.razor` + `.razor.cs` (code-behind) + `.razor.css` + đôi khi `.razor.js`. |
| `Services/` | **~86 service** (UI-side). Gọi API qua `HttpClientService`. Có cả file lẻ (`BrandCodeService.cs`) và folder (`CPD/`, `DomainPriceEvals/`, `AgentChat/`). |
| `Identity/` | SSO + JWT, LocalStorage token. |
| `Http/` | `HttpClientService` — Bearer JWT tới API. |
| `Controllers/` | MVC controller phụ (vd SSO callback). |
| `Locales/` + `localization/` | i18n (giao diện tiếng Việt). |
| `Program.cs` | Bootstrap DI. |

Ngoài project chính: `src/Task9.ETL/` (ETL report → MySQL trực tiếp), `src/SerpThai.Tests/`, `src/BootstrapBlazor/` (lib fork).

## Pattern 1 page Blazor (3 file đồng hành)
```
XxxPage.razor       → markup, @page, @layout PageLayout, @inherits BootstrapComponentBase
XxxPage.razor.cs    → logic (code-behind partial class)
XxxPage.razor.css   → scoped CSS
XxxPage.razor.js    → JS interop (optional)
```

## ⚠️ Tạo page mới — 3 bước BẮT BUỘC (xem [[Blazor Page Creation Checklist]])
1. Đầu file: `@layout PageLayout` + `@inherits BootstrapComponentBase` (tránh vỡ layout / thiếu Master Layout).
2. Khai báo `MenuItem` (có `Url`) trong `PageLayout.razor.cs`.
3. **Copy y hệt** menu đó vào `BuildMenusForRole` ở `UrlAuthorizationService.cs` — nếu quên, URL không nằm whitelist → user bị **Unauthorized**.

## ETL (`src/Task9.ETL/`)
`Configuration/`, `Models/`, `Repositories/`, `Services/`, `Transformers/`. Đọc/ghi MySQL trực tiếp qua `Etl__ConnectionString` (không qua API). Sinh report SEO.

## Giao tiếp ra ngoài
- → **API**: HTTP + JWT (`HttpClientService`).
- → **Agent**: HTTP + SSE streaming (`Services/AgentChat`, `AiSidecar`) cho AI Chat.
- → **N8N**: webhook.
- Embed **Metabase** dashboard.

## Deploy
Commit prefix `[WEB]`. `test`→tag `test`, `staging`→`staging`, `main`→`net9`. Sau sửa UI phải restart dev server / rebuild container + báo user reload (Ctrl+Shift+R) để không test nhầm bản cache.

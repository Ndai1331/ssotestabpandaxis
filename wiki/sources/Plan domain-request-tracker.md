---
type: source
title: "Plan domain-request-tracker"
created: 2026-06-27
updated: 2026-06-29
tags:
  - plan
  - ui
  - api
status: shipped
source_type: plan
date_published: 2026-06-25
url: "plans/260625-1224-domain-request-tracker/"
confidence: high
key_claims:
  - "Trang /domain-request-tracker đọc từ seo_data.seo_domain_request_canonical (read-only, ETL từ Google Sheets)"
  - "Layout: 4 summary cards + flat table server-side pagination"
  - "Roles: ALL (ADMIN, HEAD, ASSISTANT, IC, QC, SEO*)"
  - "Backend: .NET API endpoint mới, pattern SeoCostOverviewService + MySqlConnector raw SQL"
  - "Status: SHIPPED 2026-06-29 (UI commit 5c80521/2d1461b) — page domain request tracker + redesigned UI đã lên main"
related:
  - "[[Blazor Page Creation Checklist]]"
  - "[[DB Separation Rule]]"
---

# Plan domain-request-tracker

**Status: ✅ SHIPPED** (2026-06-29) — page `/domain-request-tracker` + UI redesign đã merge lên `main` (task9-ui commit `5c80521`, `2d1461b`). Plan gốc draft 2026-06-25.

## Mục tiêu

Trang `/domain-request-tracker` — hiển thị danh sách domain đã request mua và trạng thái (Done/Pending/No/Cancel...) từ bảng `seo_data.seo_domain_request_canonical`.

## Phases

| Phase | Nội dung | Status |
|-------|---------|--------|
| 01 | API Backend — Contract + Service + Controller | ✅ Done |
| 02 | UI Blazor — Page + Code-behind | ✅ Done |
| 03 | Auth + Menu registration | ✅ Done |

## Design Decisions

- DB: **Read-only** từ `seo_data.seo_domain_request_canonical` qua `ConnectionString "SeoData"` (đã có trong appsettings)
- Pattern tham khảo: `SeoCostOverviewService` (MySqlConnector raw SQL), `DomainByPic.razor` (UI pattern)
- Export: để phase sau

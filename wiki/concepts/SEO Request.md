---
type: concept
title: "SEO Request"
created: 2026-07-17
updated: 2026-07-20
tags:
  - seo
  - feature
  - ui
  - api
  - apps-script
status: active
related:
  - "[[Codebase — task9-ui]]"
  - "[[Codebase — task9-api]]"
  - "[[Authz & Permission Model]]"
---

# SEO Request

Tính năng quản lý yêu cầu SEO (seo-request) — sheet-export cho Apps Script mirror sync, domain migrate CSV, 4-step approval, post-purchase statuses. Major feature ~3200 lines API code (2026-07-15 → 2026-07-16).

## Commits chính

| Commit | Ngày | Mô tả |
|--------|------|-------|
| `6b1d10d` | 2026-07-15 | feat: sheet-export endpoint for Apps Script mirror sync |
| `c61deeb` | 2026-07-15 | feat: add `tools-seo.seo-request` permission + backfill |
| `7b7b49e` | 2026-07-16 | fix: domain migrate CSV approvals, brand code K, post-purchase |
| `fde8d3d` | 2026-07-16 | fix: match FINAL CHECK(ASST)/DUYỆT MUA(HEAD); No→Không triển khai |
| `ebdfaea` | 2026-07-17 | fix: domain migrate Done→Đã thanh toán |
| `07ebc73` | 2026-07-17 | feat: approval filters, pending-step lock, pic-keyword summary, pipeline statuses |
| `ba9bd69` | 2026-07-17 | feat: group pic-keyword summary by RequestTime |
| `8b0950f` | 2026-07-17 | feat: legacy sheet-export column map v2 (Domain/Resource headers match team sheets; PIC alias_name_ggsheet; TEAM=Code; brand K→Khác) |
| `7997363` | 2026-07-17 | fix: remove duplicate SeoCostOverviewController (AmbiguousMatchException) |
| `1974d75` | 2026-07-17 | feat: seo_domain_inventories + Done hook → tạo Domain Inventory entry. Xem [[Domain Inventory]] |
| `8c08be6` | 2026-07-17 | feat: domain-inventory sheet-export for Google Sheet mirror tab 1 |
| `ab6cd58` | 2026-07-18 | feat: migrate skip invalid + inventory import (SeoDomainInventoryMigrateColumnMap 37 lines, validator 65 lines, statuses 33 lines, service +263; 556 lines 14 files) |
| `982258a` | 2026-07-18 | fix: migrate IC DA_CHECK, domain cost_type 9, request_time |
| `80fae32` | 2026-07-18 | fix: migrate BRAND→brand_codes and VN VND parse |
| `2aa2830` | 2026-07-18 | fix: dashboard TotalVnd uses `vnd ?? amount` |
| `ffac241` | 2026-07-18 | fix: THANH TOÁN VND/USDT aliases + GIẢI TRÌNH→pic_note |
| `7e08665` | 2026-07-18 | fix: migrate dates, CK% as 10, ticket_number from ID Phiếu |
| `e8635b5` | 2026-07-18 | feat: ticket_number from ID Phiếu, THÁNH TOÁN USDT, assistant_note (DDL `20260718_seo_requests_assistant_note.sql`) |
| `62704cf` | 2026-07-18 | fix: batch list load to eliminate N+1 |
| `e68aa72` | 2026-07-19 | feat: pending pipeline, money rules, migrate PIC/alignment (SeoRequestMigrateAlignmentValidator 282 lines, SeoRequestMoneyHelper 52 lines, service +250 lines; 652 lines, 7 files) |
| `da7fcd0` | 2026-07-19 | fix: resolve migrate PIC for inactive sheet users (SeoRequestPicLookupHelper 140 lines, include non-deleted inactive users in PIC lookup; 216 lines, 4 files) |

## Kiến trúc

### API

- **Controller:** `SeoRequestController.cs`
- **Service:** `SeoRequestService.cs` (~1111 lines), `SeoRequestService.Helpers.cs` (151 lines), `SeoRequestService.Persistence.cs` (143 lines)
- **DTOs:** `CreateSeoRequestDto`, `UpdateSeoRequestDto`, `SeoRequestDto`, `SeoRequestFilterDto`, `SeoRequestDashboardDto`, `SeoRequestApprovalDto`, `SeoRequestMigrateDtos`, `SeoRequestSheetExportDto`, `SeoRequestSheetBridgeExportDto`
- **Migrate:** `SeoRequestMigrateColumnMap.cs` (134 lines), `SeoRequestMigrateStatusParser.cs` (161 lines) + tests
- **Sheet export:** `SeoRequestSheetExportMapper.cs` (172 lines) — xuất data ra format Apps Script mirror sync
- **Post-purchase:** `SeoPostPurchaseStatuses.cs` (142 lines) — statuses sau mua domain
- **Approval:** `SeoRequestApprovalStatuses.cs` (40 lines) — 4-step approval flow
- **Role helper:** `SeoRequestRoleHelper.cs` (23 lines) — role-based logic
- **Payment:** `SeoPaymentStatusConstants.cs` (32 lines)

### Apps Script Mirror Sync

Sheet-export endpoint cho phép Apps Script đồng bộ dữ liệu SEO Request sang Google Sheets (mirror). `SeoRequestSheetExportMapper` map data ra format sheet.

### Domain Migrate CSV

- Import CSV approvals cho domain migrate
- Brand code K support
- Post-purchase status tracking
- Status parser: `FINAL CHECK(ASST)` / `DUYỆT MUA(HEAD)` matching, `No` → `Không triển khai`

### RBAC

- Permission: `tools-seo.seo-request` (thêm vào `PermissionSeeder.cs`, `Permissions.cs`)
- Backfill: gán permission cho các role hiện có

### Tests

- `SeoRequestMigrateStatusParserTests.cs` — test status parser
- `SeoRequestMigrateRowValidatorTests.cs` — test migrate row validator (`ab6cd58`)
- `PermissionSeederLegacyMatrixTests.cs` — test permission seeder compatibility

### UI (2026-07-18 → 2026-07-19, UI `9e8e2a0`→`82d19bf`, 10 commits)

- **List** (`SeoRequestList.razor/.cs/.css`): show DB Id thay request_code (`8703431`/`e4f06ab`); VND fallback (`vnd ?? amount`); pie merge by label; en-US format; tab pills RESOURCE vs Domain (`?type=` query); filter chung; ẩn cột loại; pending tabs + inventory styling + money UX + annotated charts (`bd3dd1e`, +2883 lines — SeoRequestDetailModalContent 557+593+382 lines mới, SeoRequestList +414/+209/+136 lines, SeoRequestDetail +152 lines, SeoRequestCreateOrUpdate +79 lines, SeoDomainInventorySheetStyle 74 lines, dashboard charts +123 lines).
- **Detail** (`SeoRequestDetail.razor/.cs/.css`, ~673 insertions 2026-07-18): info layout, Head VND fallback, Assistant comment, equal-height cards, PIC×Keyword modal (`b864b0e`).
- **Dashboard**: KPI Chi phí (VND) full money display without B/K; donut charts en-US format.
- **Site**: `site.css` z-index cleanup — remove global modal/dropdown overrides (`14cb0b2`).
- **Charts**: `seo-dashboard-charts.js` donut center/tooltip en-US.
- **ETL page repurposed** (`52b3a86`): `/etl` page → worker monitor (read-only: canonical freshness from seo_data, run log/registry/header-resolver mappings/quarantine from qcadmin). `EtlWorkerMonitorService.cs` 203 lines + `EtlWorkerMonitorModels.cs` 72 lines. Old job services retained (render disabled only).
- **Dead ETL pages removed** (`82d19bf`, -877 lines): Report Runner + Table Registry pages (queried non-existent `report_definitions`/`etl_table_registry` tables), EtlExecutionLogs component, sidebar menu + authorization entries removed. ETL Dashboard (worker monitor) + Connection Manager remain.

## DB

- Migration: `20260713_create_domain_purchase_request.sql` (43 lines) — tạo bảng domain purchase request
- Bảng: `seo_request` + related tables trong `qcadmin`

## Domain Purchase Request (pilot, `979b7e9`, 2026-07-13)

Pilot feature riêng nhưng liên quan:
- CRUD + 4-step approval workflow
- Read-only export endpoint
- `DomainPurchaseRequestService.cs` (121 lines) + `.Helpers.cs` (151) + `.Persistence.cs` (143)
- `DomainPurchaseRequestRules.cs` (96 lines) — business rules
- `DomainPurchaseRequestController.cs` (84 lines)
- Tests: `DomainPurchaseRequestRulesTests.cs` (56 lines)
- Permission: thêm vào `PermissionSeeder.cs`

## Lưu ý

- Feature đang active trên branch `feat/seo-request`
- Đã merge vào test branch (`8a3ac21`)
- UI đã ship: list (DB Id, tabs RESOURCE/Domain, PIC×Keyword modal, VND fallback, pie merge, en-US), detail (info layout, Assistant comment, equal-height cards). Xem section UI ở trên.
- Liên quan đến [[SpiderCave]] qua brand code K + BOKT
- **2026-07-17 update:** approval filters + pending-step lock + pic-keyword summary (group by RequestTime) + pipeline statuses + legacy sheet-export column map v2 + Done→Đã thanh toán fix. ~3200+ lines API.
- **2026-07-17 fix:** SeoCostOverviewController duplicate gây AmbiguousMatchException → đã xóa (`7997363`, -752 lines).
- **Done hook (2026-07-17):** Khi SEO Request Done, tự động tạo [[Domain Inventory]] entry (API `1974d75`).
- **2026-07-18 afternoon batch (10 API + 7 UI commits):** Migrate stabilization — BRAND→brand_codes, VN VND parse, IC DA_CHECK, domain cost_type 9, request_time, dates, CK% as 10, skip invalid + inventory import, VND fallback `vnd ?? amount`, THANH TOÁN VND/USDT aliases, GIẢI TRÌNH→pic_note, ticket_number from ID Phiếu, THÁNH TOÁN USDT, assistant_note column (DDL), batch list load N+1 fix. UI: list tabs, detail layout, z-index cleanup. Re-import runbook trong `tools/seo-request-sheet-bridge/MIGRATE_PILOT.md`.
- **N+1 fix (`62704cf`):** Batch list load — SeoRequestService.cs +46/-17 lines.
- **2026-07-19 batch (2 API + 3 UI commits):** API `e68aa72` — pending pipeline, money rules (SeoRequestMoneyHelper 52 lines), migrate PIC/alignment (SeoRequestMigrateAlignmentValidator 282 lines, service +250 lines; 652 lines 7 files). API `da7fcd0` — fix migrate PIC for inactive sheet users (SeoRequestPicLookupHelper 140 lines, include non-deleted inactive users so alias_name_ggsheet labels map to pic_id; 216 lines 4 files). UI `bd3dd1e` — pending tabs, inventory styling, money UX, annotated charts (+2883 lines, 26 files; SeoRequestDetailModalContent 1532 lines mới). UI `52b3a86` — `/etl` page → worker monitor (EtlWorkerMonitorService 203 lines). UI `82d19bf` — remove dead Report Runner + Table Registry pages (-877 lines).
- **Migrate là insert-only:** Re-import = delete + re-import (không SQL backfill). Xem `MIGRATE_PILOT.md` § Re-import.
- **assistant_note DDL:** `Scripts/20260718_seo_requests_assistant_note.sql` — `ALTER TABLE seo_requests ADD COLUMN assistant_note TEXT NULL AFTER head_note;` nếu thiếu.
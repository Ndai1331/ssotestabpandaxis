---
type: concept
title: "Domain Inventory"
created: 2026-07-18
updated: 2026-07-18
tags:
  - seo
  - domain
  - feature
  - api
status: active
related:
  - "[[SEO Request]]"
  - "[[Codebase — task9-api]]"
  - "[[Authz & Permission Model]]"
---

# Domain Inventory

Quản lý kho domain (seo_domain_inventories) — khi SEO Request đánh dấu Done, tự động tạo/cập nhật domain inventory entry. Feature mới 2026-07-17 (API `1974d75`, ~902 lines).

## Commit chính

| Commit | Ngày | Mô tả |
|--------|------|-------|
| `1974d75` | 2026-07-17 | feat: seo_domain_inventories + Done hook |

## Kiến trúc

### API

- **Controller:** `SeoDomainInventoryController.cs` (53 lines) — `[Authorize]`, route `api/seo-domain-inventory`
  - `POST /list` — get list với viewer scope (filter by role/team)
  - `PATCH /` — patch single inventory entry
- **Service:** `SeoDomainInventoryService.cs` (389 lines) — list + patch + viewer scope filtering
- **Interface:** `ISeoDomainInventoryService.cs` (15 lines)
- **Role helper:** `SeoDomainInventoryRoleHelper.cs` (12 lines)
- **DTOs:** `SeoDomainInventoryDtos.cs` (56 lines) — `SeoDomainInventoryDto`, `SeoDomainInventoryFilterDto`, `PatchSeoDomainInventoryDto`
- **Statuses:** `SeoDomainInventoryStatuses.cs` (46 lines) — domain statuses (Active/Inactive/Bàn giao/No data/Lỗi domain/KGH/Khác) + key statuses (SEO/DUY_TRI_TOP/NGUNG_SEO/KHAC)
- **Repository:** `SeoDomainInventoryRepository.cs` (11 lines)
- **Entity:** `SeoDomainInventory.cs` (115 lines) — FK tới `SeoRequest`, `User` (PIC), `Team`, `Brand`, `Keyword`

### Done Hook (từ SEO Request)

Khi SEO Request được đánh dấu Done (PostPurchaseStatus), `SeoRequestService` tạo/cập nhật entry trong `seo_domain_inventories`. Liên quan: [[SEO Request]] post-purchase statuses.

### RBAC

- Permission: `tools-seo.domain-inventory` (thêm vào `Permissions.cs` + `PermissionSeeder.cs`)
- Controller dùng `[Authorize]` + viewer scope (ViewerUserId/ViewerRole từ JWT claims)
- Backfill: gán permission cho các role hiện có (qua `PermissionSeeder`)

## DB

- Migration: `Scripts/20260717_create_seo_domain_inventories.sql` (32 lines)
- Bảng: `seo_domain_inventories` trong `qcadmin` (prod) / `qcadmin_test` (test)
- Unique key: `uk_seo_domain_inventories_request` (seo_request_id) — 1:1 với SEO Request
- Indexes: team_id, status_domain, status_key, domain

### Schema chính

| Column | Type | Mô tả |
|--------|------|-------|
| `id` | BIGINT PK | Auto increment |
| `seo_request_id` | BIGINT | FK → seo_request (unique) |
| `pic_id` | INT | PIC user |
| `team_id` | INT | Team |
| `domain` | VARCHAR(255) | Domain name |
| `keyword_id` | BIGINT | Keyword |
| `keyword_text` | VARCHAR(255) | Keyword text |
| `price` | DECIMAL(18,4) | Price |
| `domain_classification` | VARCHAR(50) | Classification |
| `brand_id` | INT | Brand |
| `status_domain` | VARCHAR(50) | Active/Inactive/Bàn giao/... |
| `status_key` | VARCHAR(50) | SEO/DUY_TRI_TOP/NGUNG_SEO/KHAC |
| `note` | TEXT | Note |
| `qc_check` | VARCHAR(255) | QC check |

## Tests

- `SeoDomainInventoryServiceTests.cs` (121 lines)
- `PermissionSeederLegacyMatrixTests.cs` — updated cho permission mới

## Files chính

| File | Vai trò |
|------|---------|
| `SeoDomainInventoryController.cs` | API controller — list + patch |
| `SeoDomainInventoryService.cs` | Business logic — list, patch, viewer scope |
| `SeoDomainInventory.cs` | Domain entity — FK tới SeoRequest/User/Team/Brand/Keyword |
| `SeoDomainInventoryStatuses.cs` | Status constants — domain + key statuses |
| `SeoDomainInventoryRoleHelper.cs` | Role-based scope logic |
| `20260717_create_seo_domain_inventories.sql` | Migration script |

## Lưu ý

- Feature closely related to [[SEO Request]] — Done hook tạo inventory entry
- Permission `tools-seo.domain-inventory` riêng (không phải `tools-seo.seo-request`)
- Viewer scope: PIC thấy row của mình, HEAD/ADMIN thấy tất cả
- Status domain/key dùng Vietnamese labels (Bàn giao, Đang SEO, Duy trì TOP, Ngừng SEO)
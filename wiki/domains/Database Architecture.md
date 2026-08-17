---
type: domain
title: "Database Architecture"
created: 2026-06-27
updated: 2026-06-27
tags:
  - database
  - mysql
  - architecture
status: mature
related:
  - "[[Task9 Platform Overview]]"
  - "[[DB Separation Rule]]"
  - "[[Infrastructure & Servers]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Database Architecture

Task9 dùng **MySQL 8.0** làm primary store. Nhiều logical database tách theo mục đích, mapping qua Metabase IDs.

## Databases

| Database | Metabase ID | Host | Mục đích | Quyền từ API |
|----------|-------------|------|----------|--------------|
| `qcadmin` | 2 | [[do-187]] (prod) | **Operational data của task9-api** — mọi CRUD table mới vào đây | Read/Write |
| `seo_data` | 3 | — | SEO analytics (ETL, Ahrefs, GSC) | **CHỈ ĐỌC** từ API |
| `task9_ai` | 5 | — | AI / Agent data | — |
| `warehousedata` | 4 | — | Data warehouse | — |

> 🔴 Xem [[DB Separation Rule]]: KHÔNG tạo operational/CRUD table trong `seo_data`. Khi thiết kế API mới với `DreamContext` → PHẢI hỏi & xác nhận database trước khi chạy DDL. (Incident 2026-04-22: tạo nhầm `domain_price_eval_session` vào `seo_data` thay vì `qcadmin`.)

## Clone topology (test/staging)

N8N workflow `TxrqpeGZi9OERH5K` mỗi **03:00** clone `qcadmin@do-187` (prod) → 2 bản trên [[do-122]]:

| Env | DB clone |
|-----|----------|
| Test | `qcadmin_test` |
| Staging | `qcadmin_stag` (clone đầy đủ prod) |

## Connection patterns

- **task9-api:** `DreamContext` (EF-style) cho `qcadmin`; raw `MySqlConnector` cho read `seo_data` (vd `SeoCostOverviewService`) qua ConnectionString `"SeoData"`.
- **N8N:** direct SQL, credential `QCADMIN PRO` (`HAodCNFUpt4MrPhb`), `seo_data` (`AJOxfO4I7aFS17G3`).
- **ETL:** direct SQL qua `Etl__ConnectionString`.

## Bảng đáng nhớ

| Bảng | DB | Ghi chú |
|------|-----|---------|
| `cpd_checker_tool_rows` | qcadmin | Brand whitelist = brand thủ công với `is_auto=0/NULL`. Xem [[CPD Auto-Detect System]] |
| `ahrefs_domain_metrics_weekly` | seo_data | 136 rows/snapshot, UNIQUE (domain, snapshot_date). Xem [[Handoff Phase3 Ahrefs Site Explorer]] |
| `seo_domain_request_canonical` | seo_data | Nguồn cho [[Plan domain-request-tracker]] (read-only, ETL từ Google Sheets) |

## MCP access

Dùng `mcp__metabase__execute_sql_query` để query, `update_metabase_card` để sửa SQL card. **KHÔNG** viết Python/curl gọi Metabase API trực tiếp.

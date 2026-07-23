---
type: decision
title: "DB Separation Rule"
created: 2026-06-27
updated: 2026-06-27
tags:
  - database
  - architecture
  - rule
status: evergreen
related:
  - "[[Infrastructure & Servers]]"
  - "[[Task9 Platform Overview]]"
sources:
  - "[[workspace-architecture.md]]"
---

# DB Separation Rule

## Quy tắc

| Database | Metabase ID | Dùng cho | Ghi chú |
|----------|-------------|---------|---------|
| **qcadmin** | 2 | Task9-api **operational data** — MỌI CRUD table mới | Default khi có doubt |
| **seo_data** | 3 | SEO analytics — **CHỈ ĐỌC** từ API | KHÔNG tạo operational table ở đây |
| **task9_ai** | 5 | AI/Agent data | |
| **warehousedata** | 4 | Data warehouse | |

## Quy trình Bắt buộc

Khi thiết kế API mới dùng database:
1. **HỎI** và được user **xác nhận** database TRƯỚC KHI code
2. **KHÔNG** tự quyết định
3. **KHÔNG** chạy DDL chưa được xác nhận đúng database

## Tại sao có Rule này

> **Incident 2026-04-22:** Tạo nhầm `domain_price_eval_session` trong `seo_data` thay vì `qcadmin`. Phải xóa và tạo lại — mất thời gian và tiềm ẩn rủi ro data.

`seo_data` là SEO analytics database, không phải operational database. Lẫn lộn 2 loại dữ liệu này gây khó maintain và khó backup/restore riêng lẻ.

---
type: concept
title: "CPD Auto-Detect System"
created: 2026-06-27
updated: 2026-06-27
tags:
  - cpd
  - n8n
  - worker
status: mature
complexity: intermediate
related:
  - "[[N8N Workflows]]"
  - "[[Task9 Platform Overview]]"
sources:
  - "[[workspace-architecture.md]]"
---

# CPD Auto-Detect System

Hệ thống kiểm tra banner/link tự động trên pub sites.

## ⚠️ Logic Quan Trọng — Đừng Sửa Nhầm

### `is_auto` flag
- `is_auto = 1` → **TẮT** quét tự động (bot bỏ qua site này)
- `is_auto = 0` hoặc `NULL` → **CHO PHÉP** quét tự động

### Brand Whitelist
**Không có** bảng `cpd_brand_whitelist` riêng. Whitelist chính là brand thủ công trong `cpd_checker_tool_rows` với `is_auto = 0/NULL`.

## Flow Hoạt động

```
N8N Schedule 7AM
  → MySQL SELECT pub sites WHERE (is_auto IS NULL OR is_auto = 0) AND deleted IS NULL/0
  → Loop từng site
  → Worker (scan_lite endpoint) → ScrapingBee → crawl banner
  → So sánh brand của banner với Brand Whitelist
     ├─ Brand NẰM TRONG whitelist → INSERT/UPDATE vào DB
     └─ Brand KHÔNG có trong whitelist → BỎ QUA
  → Telegram notify kết quả
```

## N8N Workflow

- **ID:** `ZcSMgWpbZ27k8xgV` (CPD Batch Lite)
- **Schedule:** 7:00 AM hàng ngày
- **Instance:** `subsytem.task9.pro`

## Worker Endpoint

- **URL:** `cpd.task9.pro` (do-122)
- **Port:** 5000 (Flask)
- **Key endpoint:** `scan_lite`
- **Tech:** Python + SeleniumBase + Cloudflare bypass
- **Storage:** R2 (screenshots)

## Database

Bảng chính: `cpd_checker_tool_rows` trong `qcadmin`

```sql
-- Lấy sites cần quét
SELECT * FROM pub_sites
WHERE (is_auto IS NULL OR is_auto = 0)
AND (deleted IS NULL OR deleted = 0)
```

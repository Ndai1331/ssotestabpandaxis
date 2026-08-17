---
type: source
title: "Handoff Phase3 Ahrefs Site Explorer"
created: 2026-06-27
updated: 2026-06-27
tags:
  - source
  - handoff
  - ahrefs
  - n8n
status: mature
source_type: handoff
source_path: "docs/handoff/handoff-20260522-1842-phase3-ahrefs-site-explorer-done.md"
date_published: 2026-05-22
confidence: high
key_claims:
  - "Workflow 5GVPcmzoJJsTkpg3 pull Ahrefs Site Explorer (DR, traffic, backlinks, refdomains) → upsert seo_data mỗi thứ Hai 6AM"
  - "ahrefs_domain_metrics_weekly: 136 rows/snapshot, UNIQUE (domain, snapshot_date)"
  - "organic-keywords endpoint 401 — ngoài gói subscription, đã xóa 3 nodes"
related:
  - "[[Ahrefs Integration]]"
  - "[[N8N Workflows]]"
  - "[[Database Architecture]]"
---

# Source: Handoff Phase 3 Ahrefs Site Explorer (Done)

**2026-05-22** — Phase 3 COMPLETE. Build & deploy N8N workflow pull Ahrefs Site Explorer data cho toàn bộ mainsites, upsert vào `seo_data` mỗi tuần.

## Workflow

| Field | Value |
|-------|-------|
| ID | `5GVPcmzoJJsTkpg3` |
| Tên | Ahrefs Site Explorer Weekly (Mainsites v2) |
| Status | Active (published, schedule ON) |
| Schedule | Every Monday 6:00 AM |
| Nodes | 9 (sau khi remove organic keywords) |

Flow: `Get Mainsites → Loop domains (batch=1) → Ahrefs [Site Metrics / Domain Rating / Backlinks Stats] → Build SQL (Code node) → MySQL upsert → nextBatch`

### Credentials
- Get Mainsites: `QCADMIN PRO` (`HAodCNFUpt4MrPhb`)
- Ahrefs HTTP: `Ahref` httpBearerAuth (`xHrdmsbyjaWQEYKb`)
- MySQL upsert: `seo_data` (`AJOxfO4I7aFS17G3`)

## Unit conversions (Build SQL)

| Ahrefs field | Công thức | DB column |
|---|---|---|
| `org_traffic` | ÷ 10 | `organic_traffic` |
| `org_cost` | ÷ 10 ÷ 100 | `traffic_value_usd` |
| `live_refdomains` | ÷ 5 | `referring_domains` |
| `domain_rating` | `dr.domain_rating.domain_rating` | `domain_rating` |
| `live` (backlinks) | không chia | `backlinks_total` |

## Pitfalls đã giải quyết

- SQL `invalid syntax` (escaped quotes) → patch via REST API.
- `access to env vars denied` (`N8N_BLOCK_ENV_ACCESS_IN_NODE=true`) → thay Code node bằng 4 HTTP Request nodes + credential.
- `Unknown column 'deleted'` ở `mainsites` → query `WHERE domain IS NOT NULL AND domain != ''`.
- `organic-keywords` 401 (ngoài gói) → xóa 3 nodes; bảng `ahrefs_organic_keywords_weekly` tạo nhưng TRỐNG.

## Files

`plans/260511-1042-seo-gsc-ahrefs-integration/` — plan tổng (Phase 1-6), phase-03 detail, DDL đã chạy PROD.

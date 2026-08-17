---
type: concept
title: "Ahrefs Integration"
created: 2026-06-27
updated: 2026-07-20
tags:
  - ahrefs
  - seo
  - n8n
  - data
status: mature
complexity: intermediate
related:
  - "[[N8N Workflows]]"
  - "[[Database Architecture]]"
sources:
  - "[[workspace-architecture.md]]"
  - "[[Handoff Phase3 Ahrefs Site Explorer]]"
---

# Ahrefs Integration

## Gói Subscription Hiện Tại

| Endpoint | Có trong gói |
|----------|--------------|
| `site-metrics` (org_traffic, org_cost) | ✅ |
| `domain-rating` | ✅ |
| `backlinks-stats` | ✅ |
| `organic-keywords` | ✅ (tài khoản mới, từ 2026-07-20 — trước đó 401 ngoài gói) |

> Bảng `ahrefs_organic_keywords_weekly` trong `seo_data` còn **rỗng** do gói cũ chặn endpoint.
> Credential n8n đã cập nhật 2026-07-20 → bảng sẽ đầy dần từ các lần chạy sau.

## N8N Workflow — Site Explorer Weekly

- **Workflow ID:** `5GVPcmzoJJsTkpg3`
- **Tên:** Ahrefs Site Explorer Weekly (Mainsites v2)
- **URL:** https://subsytem.task9.pro/workflow/5GVPcmzoJJsTkpg3
- **Schedule:** Mỗi thứ Hai 6:00 AM
- **Status:** Active ✅ (lần chạy cuối #40, 2026-05-22, ~3 phút)

**Flow:**
```
Mon 6AM / Manual → Get Mainsites → Loop domains (batch=1)
  → Ahrefs: Site Metrics → Ahrefs: Domain Rating → Ahrefs: Backlinks Stats
  → Build SQL (Code node) → MySQL upsert → next batch
```

**Credentials:**
| Node | Credential ID |
|------|--------------|
| Get Mainsites | `HAodCNFUpt4MrPhb` (QCADMIN PRO MySQL) |
| Ahrefs HTTP | `xHrdmsbyjaWQEYKb` (httpBearerAuth) |
| MySQL upsert | `AJOxfO4I7aFS17G3` (seo_data MySQL) |

## Unit Conversions (BẮT BUỘC trong Build SQL)

| Ahrefs field | Công thức | DB column | DB table |
|---|---|---|---|
| `org_traffic` | ÷ 10 | `organic_traffic` | `ahrefs_domain_metrics_weekly` |
| `org_cost` | ÷ 10 ÷ 100 | `traffic_value_usd` (USD) | |
| `live_refdomains` | ÷ 5 | `referring_domains` | |
| `domain_rating` | từ `dr.domain_rating.domain_rating` | `domain_rating` | |
| `live` (backlinks) | không chia | `backlinks_total` | |

## Database

- **DB:** `seo_data` (Metabase ID=3)
- **Table:** `ahrefs_domain_metrics_weekly`
- **Rows:** 136 (snapshot_date = 2026-05-19)
- **UNIQUE KEY:** `(domain, snapshot_date)` → `ON DUPLICATE KEY UPDATE`

## Credentials — MỘT API key duy nhất (2026-07-20)

Một API key từ app.ahrefs.com/account/api-keys xác thực **cả** REST v3 (`/v3/*`) **và**
MCP endpoint (`/mcp/mcp`). Verify bằng curl: tools/list + tools/call đều 200.

### Lịch sử: workaround OAuth token đã gỡ

Hồi `ahrefs-mcp` được viết, endpoint MCP chưa hỗ trợ API key công khai, nên service
mượn OAuth token từ file `.credentials.json` **của Claude Code** (format `mcpOAuth`),
mount `:ro` vào container. Kéo theo một đống phụ trợ: `CredentialsService` dò 4 đường
dẫn, entrypoint validate file, startup check, health report token expiry — và một bug:
REST v3 dùng nhầm OAuth token nên **luôn 401**.

Ahrefs đã mở API key cho MCP → 2026-07-20 gỡ sạch: xoá `credentials.service.ts`, bỏ
mount, bỏ validate entrypoint. Còn đúng `AHREFS_API_KEY`.

| Nơi giữ key | Ghi chú |
|---|---|
| do-122 `/home/tobi/ahrefs-mcp/.env` | compose inject vào container |
| workspace `.mcp.json` (gitignored) | Claude Code MCP client |
| n8n credential `xHrdmsbyjaWQEYKb` (httpBearerAuth) | workflow `5GVPcmzoJJsTkpg3` gọi thẳng v3, **không** qua service |

> ⚠️ do-122 dùng `docker-compose.yml` **maintain bằng tay**, KHÔNG sync từ repo
> (`docker-compose.run.yml`). Thêm/bớt env var, volume phải sửa cả hai.

> ⚠️ Đổi API key phải update credential n8n `xHrdmsbyjaWQEYKb` TRƯỚC lần chạy Thứ Hai 10:00 UTC.

### Chốt chặn chống ghi 0 (thêm 2026-07-20)

Mọi node Ahrefs trong workflow weekly để `continueOnFail: true`, còn Build SQL default
`?? 0` → credential 401 thì workflow vẫn báo "success" nhưng **ghi 0 cho toàn bộ 134 domain**.
Đã xảy ra thật: snapshot `2026-07-03` và `2026-07-10` toàn 0 (không backfill, coi như mất).

Build SQL nay chặn ngay đầu node, trước mọi phép `?? 0`:

| Tình huống | Hành vi |
|---|---|
| Lỗi 401/403/unauthorized/invalid key ở site-metrics, DR hoặc backlinks | `throw` → execution **đỏ**, không ghi gì |
| Lỗi lẻ 1 domain (timeout, rate limit) | emit `SELECT 1 AS skipped` → bỏ qua domain đó, giữ snapshot cũ |
| `m.metrics` thiếu / không phải object | như trên, `ahrefs_errors: ['empty metrics response']` |
| Bình thường | chạy nguyên logic cũ, không đổi |

`organic-keywords` cố tình KHÔNG nằm trong nhóm fatal — mất keywords không được phép
giết cả run metrics.

Backup workflow trước khi vá: `plans/260720-0836-ahrefs-credentials-update/workflow-backup-before-guard.json`

## Ahrefs MCP Service

- **Path:** `services/ahrefs-mcp/` (port 3000)
- **Tech:** Node.js/TypeScript + Fastify
- **Swagger:** `/docs`
- **Subdomains:** `ahref-api.task9.pro`, `ahref-mcp.task9.pro` (do-122)

## Gotchas Đã Gặp

- `select` cho `keywords-explorer/*` phải là **chuỗi comma-separated**, KHÔNG phải mảng.
  Truyền mảng -> Ahrefs trả `column '["keyword"' not found`, rồi `catch` trong
  `analyze.service.ts` nuốt lỗi và return `{keywords: []}` -> UI thấy rỗng, không thấy lỗi.
  Đã sửa `matching-terms` + `related-terms` (2026-07-20). Các call site khác vốn đã `.join(',')`.

- `$env` bị block trong N8N Code node (`N8N_BLOCK_ENV_ACCESS_IN_NODE=true`) → dùng HTTP Request nodes + credential thay vì Code node chứa env var
- `mainsites` không có cột `deleted` → query: `WHERE domain IS NOT NULL AND domain != ''`
- Escaped quotes `\\'0\\'` trong expression → patch via REST API

## Roadmap Còn Lại

- **Phase 4:** API endpoints expose `ahrefs_domain_metrics_weekly` qua REST
- **Phase 5:** UI Dashboard — DR, traffic, backlinks trend per domain
- **Phase 6:** Alert system khi DR/traffic drop > threshold

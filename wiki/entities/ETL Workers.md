---
type: entity
title: "ETL Workers"
created: 2026-07-13
updated: 2026-07-17
tags:
  - etl
  - submodule
  - python
status: active
related:
  - "[[Task9 Platform Overview]]"
  - "[[Infrastructure & Servers]]"
sources:
  - "[[workspace-architecture.md]]"
---

# ETL Workers

3 submodule ETL mới được đăng ký vào workspace 2026-07-05.

## Submodules

| Path | Repo | Mô tả |
|---|---|---|
| `services/etl` | `task9org/etl-worker` | ETL worker chính — Python, có `app.py`, `cost_row_parser.py`, `domain_inventory_parser.py`, `header_matcher.py`, `lookup_service.py`, `coerce.py`, `alias_map.py`, `cost_keyword_reconciler.py` |
| `services/gas-etl` | `task9org/etl-seo-data` | GAS ETL — đẩy dữ liệu SEO qua Google Apps Script |
| `services/gas-domain-request` | `task9org/etl-seo-domain-request` | Domain request tracker ETL qua GAS |

## Đặc điểm

- **KHÔNG dùng worktree** — chạy qua `docker compose up <svc>` như các worker khác
- **Python** — Flask/FastAPI pattern tương tự `worker-lite`
- Có `Dockerfile` + `docker-compose.prod.yml` (ít nhất `services/etl`)
- Thư mục `raw/` chứa dữ liệu thô ETL

## ETL Audit Reports (2026-07-10)

3 báo cáo salvage từ worktree đã gỡ:
- `plans/reports/audit-260703-1139-seo-cost-sheet-gws-audit.md` — audit SEO cost sheet GWS
- `plans/reports/audit-260705-0919-tong-hop-file-team-source-eval.md` — tổng hợp file team source eval
- `plans/reports/validation-260703-1243-seo-cost-etl-test-fixes.md` — validation + test fixes

## ETL Worker — Lookup Endpoint (2026-07-15)

Commit `fc50d57` (etl-worker): thêm lookup endpoint resolve PIC/team và domain/keyword. Cho phép truy vấn PIC/team cho 1 domain/keyword cụ thể, phục vụ reconciliation.

## GAS-ETL — Heartbeat (2026-07-11)

Commit `8f79cd0` (gas-etl): thêm `setupMultiFileCron` — time trigger 6 giờ/lần cho heartbeat. GAS post `gas_start`/`gas_finish` lên registry API. Commit `bdc39af`: registry-driven raw dump đọc active sources từ API (không hardcode).

## ETL Sources Registry API (2026-07-02)

Commit `89a4d5a` (API): thêm ETL source registry CRUD + run-log + GAS session heartbeat.

| Endpoint | Mục đích |
|----------|----------|
| `GET /api/etl-sources` | List active sources (GAS reads trước raw-dump) |
| `POST/PUT/DELETE /api/etl-sources` | CRUD allowlist sources |
| `GET /api/etl-sources/runs` | Ingest history / run-log |
| `POST /api/etl-sources/log-run` | Secret-guarded GAS heartbeat (`gas_start`/`gas_finish`) |

**Code:** `EtlSourceService.cs` (194 lines), `EtlSourceRegistryController.cs` (166 lines), `EtlSource.cs` + `EtlRunLog.cs` domain entities, `EtlSourceDtos.cs`. Join latest run per sheet vào source list.

**DB:** Bảng `etl_source`, `etl_run_log` trong `qcadmin`. `DreamContext.cs` đã đăng ký.

## Lưu ý

- ✅ Đã có entry trong `docs/workspace-architecture.md` service catalog (§2.1, §3.11, §7.2) — cập nhật 2026-07-13
- Submodule refs được bump 2026-07-10 (`8ae91cf`)
- ETL worker branch hiện tại: `claude_feat/single-flow-worker`
- GAS-ETL branch hiện tại: `claude_feat/seo-cost-etl-registry`
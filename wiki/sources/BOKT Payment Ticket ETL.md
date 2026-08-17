---
type: source
title: "BOKT Payment Ticket ETL"
status: in-progress
updated: 2026-06-29
tags: [etl, n8n, google-sheets, apps-script, mysql, seo-payment]
---

# BOKT Payment Ticket ETL

> Tool ingest phiếu thanh toán SEO từ Google Sheet "ĐỐI SOÁT PHIẾU SEO TRÊN BOKT" vào MySQL. Code tại `tools/seo-payment-tickets/`. Liên quan: [[N8N Workflows]], [[Database Architecture]].

## Kiến trúc (quan trọng — đã chốt)
```
Google Sheet (private) → Apps Script OUTBOUND push → n8n webhook → MySQL raw table → n8n parser → parsed/child tables
```
**Vì sao push thay vì pull:** Workspace policy chặn public Apps Script pull endpoint và chặn share sheet cho service account → buộc dùng **Apps Script outbound push**. `clasp push` KHÔNG dùng được (lỗi `Insufficient Permission`) → sửa `.gs` phải paste tay vào editor.

## Tham số chốt
- **Sheet ID:** `1Dh0CyUyxHTri8t385UI1CDK1Eze2CukEv-8-5QWI1iU`
- **Sheet loại trừ:** `3. Ref`, `Nháp`
- **Webhook:** `https://subsytem.task9.pro/webhook/bokt-gas-push`
- **Apps Script state property:** `BOKT_PUSH_STATE={sheetIndex,offset}`

## Apps Script functions (`SheetInspector.gs`)
- `pushManual()` — smoke test, push 5 rows
- `pushNextChunk()` — push chunk 200 row, giữ cursor qua PropertiesService
- `setupAutoPushTrigger()` — tạo time trigger mỗi 5 phút
- `stopAutoPushTrigger()` — gỡ trigger

## N8N workflows
| Vai trò | Tên | ID | Chain |
|---|---|---|---|
| Receiver | BOKT GAS Push Webhook | `X4FSrmHk2Vu0y7k1` | Webhook → Build raw insert SQL → Insert raw rows |
| Parser | BOKT Parse raw → child tables | `wTaikMDdSZOUbe1t` | Manual + mỗi 10 phút → Fetch unparsed raw → Parse raw to SQL → Insert parsed rows |
- Project n8n: `sHhZEhh6NQ6tjKDJ`

## DB tables (`schema.sql`) — DB `seo_data` (read-only từ API; tool này chạy ngoài API)
- `seo_bokt_payment_ticket_raw` (PK id, UNIQUE `(spreadsheet_id, sheet_name, source_row)`, có `raw_row_json`, `source_row_hash`)
- `seo_bokt_payment_ticket_parsed` (parse_status, parse_confidence, parse_errors)
- child: `_domain`, `_brand`, `_url`, `_kv`

## Trạng thái cuối quan sát (handoff 2026-06-28)
`raw=1765, parsed=1354, unparsed=411, sheets=3, domain=368, brand=553, url=547, kv=856`

## ⚠️ Vấn đề đang mở (parser)
- Parser fail trên row có giá trị KV chứa `$` / URL (vd raw_id `3917`): `ER_PARSE_ERROR` do n8n MySQL node xử lý expression với `$`/`//`.
- **Hotfix nhanh:** trong node `Parse raw to SQL` của `wTaikMDdSZOUbe1t`, tắt KV insert (`const ks=[];`) → cho parsed/domain/brand/url chạy tiếp. Giữ `Fetch unparsed raw` ở `LIMIT 1` khi debug, ổn thì nâng lại `LIMIT 100`.
- Về sau làm lại KV bằng parameterized insert.

## Cạm bẫy
- KHÔNG gọi `get_execution` với `includeData=true` cho execution parser fail → output khổng lồ, cạn context.
- KHÔNG dùng `clasp push`.
- KHÔNG lưu/lộ secret — mọi token để `[REDACTED]`.
- Verify sửa `.gs`: script tạm trong `$TMPDIR` prefix `hermes-verify-`, báo là ad-hoc.

## Next steps (cho session sau)
1. Query counts (xem `HANDOFF.md` mục Immediate next steps).
2. Confirm webhook `X4FSrmHk2Vu0y7k1` còn nhận push.
3. Fix parser KV như trên → publish → chạy batch tới `unparsed=0`.
4. Xong ingest thì `stopAutoPushTrigger`.

> Nguồn đầy đủ: `tools/seo-payment-tickets/HANDOFF.md` + `schema.sql` + `SheetInspector.gs` + `parser.py`.

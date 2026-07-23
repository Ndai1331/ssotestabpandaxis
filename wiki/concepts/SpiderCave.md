---
type: concept
title: "SpiderCave"
created: 2026-07-17
updated: 2026-07-18
tags:
  - spidercave
  - bokt
  - blockchain
  - risk
  - feature
status: active
related:
  - "[[Codebase — task9-ui]]"
  - "[[Codebase — task9-api]]"
  - "[[N8N Workflows]]"
  - "[[Authz & Permission Model]]"
sources:
  - "[[workspace-architecture.md]]"
---

# SpiderCave

Trang `/spidercave` — điều tra blockchain (money-flow trace, convergence detection, wallet risk finder) cho dữ liệu Đối soát (BOKT). Liên quan: [[Authz & Permission Model]] (RBAC), [[N8N Workflows]].

## Hai tính năng chính

### 1. BOKT Convergence (API `8b738d9`, 2026-07-07)

API endpoints backing SpiderCave UI page (money-flow trace + convergence detection):

| Endpoint | Mục đích |
|----------|----------|
| `GET /api/spidercave/stats` | Thống kê tổng |
| `GET /api/spidercave/convergence` | Phát hiện convergence |
| `GET /api/spidercave/journeys` | Money-flow trace |
| `GET /api/spidercave/bokt-info/{address}` | BOKT info theo address |
| `GET /api/spidercave/pic-summary/{address}` | PIC summary theo address |

**Code:** `BoktCostCenterService.cs` (443 lines), `SpiderCaveService.cs` (457 lines), `SpiderCaveController.cs` (138 lines), `BoktCostCenterController.cs` (42 lines). Tổng ~1245 lines.

> ⚠️ Endpoint này đã từng thiếu trên test → UI render all-zero stats (API 404). Đã fix bằng cách merge vào test.

### 2. Wallet Risk Finder (workspace `15cb1b3`, 2026-07-17)

Điều tra một ví Ethereum (tối đa 2 hop) từ trang `/spidercave` (mode mặc định "Điều tra một ví"):

```
UI (WalletRiskFinder.razor)
  → POST /api/spidercave/investigations  (JWT, roles ADMIN/HEAD/ASSISTANT)
  → API tạo session (qcadmin.wallet_risk_investigation, coding dùng qcadmin_test)
  → POST webhook n8n "SpiderCave Wallet Risk Finder" (WalletRiskFinder__WebhookUrl)
  → n8n thu thập on-chain evidence (bounded hop-1/hop-2, canonical token allowlist)
  → POST /api/spidercave/internal/investigations/{id}/result
      (header X-N8N-Webhook-Secret = N8N__WebhookSecret, so sánh fixed-time)
  → API chạy WalletRiskScorer (deterministic, C# + tests), enrich BOKT read model
  → persist snapshot immutable vào result_json; UI poll GET 2s/lần
```

**Nguyên tắc thiết kế:**
- n8n chỉ điều phối/thu thập — mọi kết luận risk nằm ở `WalletRiskScorer` (C# deterministic)
- Coverage thiếu → risk `UNKNOWN`, không giả định an toàn
- Callback idempotent theo `resultHash`
- State machine: `QUEUED → SCANNING_* → SCORING → COMPLETED|PARTIAL|FAILED` với optimistic concurrency (cột `version`)
- Fake token/address poisoning chỉ là warning, không cộng điểm

**n8n workflow:** `n8n/spidercave-wallet-risk-finder.json` (workflow ID `mxY9kA8v0ubeGsDy`, sanitized export — credentials by name only)

**DB:** `qcadmin.wallet_risk_investigation` (prod), `qcadmin_test` cho coding

**BOKT brand masking:** Xem [[Authz & Permission Model]] — brand chỉ hiện 4 ký tự cuối, `BoktBrandMasker.cs`. API commit `4eb7490` mask brand + strip "BOKT" wording. `921ea9e` sync BOKT fixes vào watchlist data.

## RBAC

- Wallet Risk Finder: roles `ADMIN/HEAD/ASSISTANT` (JWT)
- Internal callback: `X-N8N-Webhook-Secret` header (fixed-time comparison)
- Permission `tools-seo.seo-request` (cho SEO Request, không trực tiếp SpiderCave)
- **2026-07-17:** Permission label đổi từ "SpiderCave" → "Điều tra Ví Crypto" (`cbe2fbe`). Permission key không đổi, chỉ label hiển thị.

## Files chính

| File | Vai trò |
|------|---------|
| `SpiderCaveController.cs` | API controller — investigations + BOKT endpoints |
| `BoktCostCenterController.cs` | API controller — cost center endpoints |
| `SpiderCaveService.cs` | Business logic — convergence, journeys |
| `BoktCostCenterService.cs` | Business logic — cost center aggregation |
| `WalletRiskScorer` | Deterministic risk scoring (C# + tests) |
| `WalletRiskFinder.razor` | UI page — wallet investigation form + results |
| `BoktBrandMasker.cs` | Brand masking utility (last 4 chars) |

## Lưu ý

- Wallet Risk Finder takeover review + fixes: `plans/reports/review-260717-0005-wallet-risk-finder-takeover-fixes.md`
- BOKT refactor (`aae36ac`): re-aggregate domain spend + identity risk from filtered tickets; added `BoktPaymentTicketDomain` entity
- SpiderCave page đã deploy trên prod UI trước khi API endpoints có trên test →曾经 all-zero stats
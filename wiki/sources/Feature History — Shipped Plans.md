---
type: source
title: "Feature History — Shipped Plans"
updated: 2026-07-20
tags: [plans, history, features]
---

# Feature History — Shipped Plans

> Catalog các plan trong `plans/` để session sau biết NGAY tính năng nào đã build (đừng build lại / đừng grep mò). Drill vào `plans/<folder>/plan.md` khi cần chi tiết. Liên quan: [[Task9 Platform Overview]], [[Codebase — task9-ui]], [[Codebase — task9-api]].

## Đã hoàn thành (completed / mvp)
- **user-activity-logging** (`260425`, `feat/user-activity-log`) — Dashboard SuperAdmin log hoạt động user.
- **ncc-catalog-auditor** (`260428`, `feat/ncc-catalog-auditor`) — Upload báo giá NCC + audit claimed vs. actual.
- **ncc-worker-api** (`260503`, `feat/ncc-worker-api`) — Python Flask worker thay 2 tab NCC (Catalog + Purchase History); auto-pull giá từ Google Sheets public, AI detect columns, recalc metrics, REST API cho Blazor. Xem [[Codebase — Workers (Python)]] (`ncc-worker`).
- **peer-price-benchmark** (`260505`) — So sánh giá peer. Completed 2026-05-05.
- **seo-gsc-ahrefs-integration** (`260511`) — Tích hợp GSC (qua Ahrefs) + Site Explorer cho 134 mainsites; phương án C dùng Ahrefs MCP. Tiền đề của [[Ahrefs Integration]].
- **ahrefs-batch-organic-keywords-drawer** (`260513`) — Drawer organic keywords trong Ahrefs Batch Checker (giống Outgoing).
- **worker-endpoint-registry** (`260606`, `claude_feat/worker-endpoint-registry`) — Chuyển config worker (URL/port/enable) từ env/JSON tĩnh sang bảng `qcadmin` + runtime resolver + admin UI. Diệt env-drift outage (vd GeoBlock trỏ nhầm port chết 5001). Xem [[Database Architecture]].
- **seo-agent-ops-center** (`260607`, `codex_feat/seo-agent-ops-center`, implemented-mvp) — Trang admin AI ops cho SEO report: AI analysis context-aware, action proposals, audit trail, playbook nền qua Ollama do-122. Xem [[Plan seo-agent-ops-center]].
- **worktree-preview-launch-json** (`260612`) — SessionStart hook symlink `services/ui|api` placeholder → code thật + auto-gen `launch.json`, cấp port động tránh đụng 5053/5093 giữa các worktree song song. Xem [[Submodule Worktree Split]].
- **core-worker-git-restructure** (`260613`) — Chuẩn hóa git flow + worktree auto-branch + deploy/release thành 1 quy trình enforce bằng hook + script. Nền của [[Git Flow 3 Tầng]].
- **n8n-db-clone-orchestration** (`260614`) — Chuyển cron clone DB sang n8n orchestration (workflow `TxrqpeGZi9OERH5K`, 03:00 clone `qcadmin@do-187` → `qcadmin_test`+`qcadmin_stag`@do-122). Xem [[N8N Workflows]].
- **domain-request-tracker** (`260625`, SHIPPED 2026-06-29, task9-ui `5c80521`/`2d1461b`) — Trang `/domain-request-tracker` đọc `seo_domain_request_canonical` + UI redesign. Xem [[Plan domain-request-tracker]].
- **n8n-notification-webhook** (`claude_feat/n8n-notification-webhook`, deployed test→staging→prod ~2026-06) — Webhook `POST /api/webhooks/n8n/notifications` (auth `X-N8N-Webhook-Secret`) + NotificationController + agent `/api/notify` + ETL health-check notify. Xem [[Notification System]].
- **spidercave-bokt-convergence** (API `8b738d9`, 2026-07-07) — SpiderCave BOKT convergence endpoints: stats, convergence, journeys, bokt-info, pic-summary. Backing `/spidercave` page money-flow trace. Xem [[SpiderCave]].
- **etl-sources-registry** (API `89a4d5a`, 2026-07-02) — `/api/etl-sources` CRUD allowlist + run-log + GAS session heartbeat. Xem [[ETL Workers]].
- **domain-purchase-request** (API `979b7e9`, 2026-07-13, pilot) — CRUD + 4-step approval + read-only export endpoint. Xem [[SEO Request]].
- **seo-request** (API `6b1d10d`→`fde8d3d`, 2026-07-15→16) — Sheet-export cho Apps Script mirror sync, domain migrate CSV approvals, `tools-seo.seo-request` permission, post-purchase statuses. ~3200 lines API. Xem [[SEO Request]].
- **spidercave-wallet-risk-finder** (workspace `15cb1b3`, 2026-07-17) — Wallet Risk Finder: điều tra ví Ethereum (2 hop), n8n workflow + WalletRiskScorer (C# deterministic) + state machine + idempotent callback. Xem [[SpiderCave]].
- **domain-inventory** (API `1974d75`, 2026-07-17) — `seo_domain_inventories` table + Done hook từ SEO Request + viewer scope list/patch. Permission `tools-seo.domain-inventory`. Xem [[Domain Inventory]].
- **support-asst-ai-fallback** (API `e858ff4` + UI `00aa26b`/`652021f` + agent `771d456`, 2026-07-18) — `POST /api/support-asst/extract-ticket` AI fallback via OllamaAIClient khi UI heuristic parser fail. UI: exact-label match, parse header-row ERP layout, ID Phiếu from page title. Không mở trang wiki riêng — feature nhỏ (190 lines API, 3 files). Ghi chú: xem [[hot.md]] fact #3.
- **seo-request-migrate-stabilization** (API `982258a`→`62704cf` + UI `9e8e2a0`→`b864b0e`, 2026-07-18) — Migrate fixes (BRAND, VND, dates, CK, ticket_number, assistant_note, skip invalid, N+1), UI list tabs + detail layout + PIC×Keyword modal + z-index cleanup. Re-import runbook `tools/seo-request-sheet-bridge/MIGRATE_PILOT.md`. Xem [[SEO Request]].
- **seo-request-pending-pipeline** (API `e68aa72` + UI `bd3dd1e`, 2026-07-19) — Pending pipeline tabs, money rules (SeoRequestMoneyHelper), migrate PIC/alignment validator (282 lines), pending tabs + inventory styling + money UX + annotated charts (+2883 lines UI, SeoRequestDetailModalContent 1532 lines mới). Xem [[SEO Request]].
- **seo-request-pic-inactive-fix** (API `da7fcd0`, 2026-07-19) — Resolve migrate PIC for inactive sheet users (SeoRequestPicLookupHelper 140 lines, include non-deleted inactive users). Xem [[SEO Request]].
- **etl-worker-monitor** (UI `52b3a86` + `82d19bf`, 2026-07-19) — Repurpose `/etl` page into read-only worker monitor (canonical freshness, run log, registry, quarantine). Remove dead Report Runner + Table Registry pages (-877 lines).

## Đang làm / pending
- **self-learning-system** (`260525`, in-progress) — 3 tầng Capture → Reinforce → Prune. Xem [[Self-Learning Memory System]].
- **seo-budget-eval-niche-ui** (`260421`, `feat/seo-budget-eval-niche`, pending) — Thêm Niche UI vào SEO Budget Eval.
- **seo-dashboard-improvements** (`260527`) — Handoff note, SEO dashboard prod chạy tốt, chờ cải tiến.
- **permission-user-team-authz-audit** (`260626`, audit-only — KHÔNG sửa code) — 🔴 phát hiện Broken Access Control (55/60 controller không enforce role). Khuyến nghị PA2 RBAC claim-driven. Xem [[Authz & Permission Model]].

## Tooling ngoài service
- **BOKT payment ticket ETL** (`tools/seo-payment-tickets/`, in-progress) — GAS push → n8n → MySQL. Xem [[BOKT Payment Ticket ETL]].

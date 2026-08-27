---
title: "Audit logs tra cứu và chi tiết"
description: "Xây trang tra cứu nhật ký kiểm toán cho HCS với bộ lọc server-side, bảng dense và modal chi tiết qua BFF."
status: planned
progress: 0%
priority: P1
effort: 3-5d
branch: main
tags: [audit, backend, blazor, security, search, ui]
blockedBy: []
blocks: []
created: 2026-08-27
---

# Kế hoạch page Audit Logs

## Phân biệt nguồn yêu cầu

- **Yêu cầu của user:** có trang để tra cứu audit logs theo họ tên/người dùng, IP, trình duyệt, thao tác, service, API và các thông tin liên quan.
- **Screenshot đính kèm:** chỉ là reference về bố cục bảng, badge HTTP status/method, nút **Chi tiết**, mật độ dữ liệu và thứ tự cột. Không dùng screenshot làm source of truth cho endpoint, schema hoặc quyền.

## Overview

Backend đã có projection audit tập trung, application service, controller GET /api/audit-logs và GET /api/audit-logs/{id}. Gateway đã proxy /api/audit-logs/{**catch-all} về Platform.

UI hiện tại chỉ là AdministrationFeature + GatewayDataPanel, đọc JSON động tối đa 100 dòng; chưa có filter, phân trang, sort, detail, field mapping hoặc trạng thái tra cứu chuyên biệt.

Kế hoạch dùng projection làm read model tập trung. Blazor chỉ gọi BFF, không gọi trực tiếp service/database. Read model là eventual-consistent vì audit records đi qua outbox/event bus.

## Phạm vi MVP

- Bảng server-side paging/sorting với các cột: Chi tiết, HTTP status + method + API, người dùng, IP, thời gian, thời lượng, service, ứng dụng, correlation ID, dấu hiệu lỗi.
- Bộ lọc nhanh và bộ lọc nâng cao: keyword, user, thời gian, status, method, IP, action/API, service, application, correlation ID, có lỗi hay không.
- Modal chi tiết: request context, user/browser, action list, entity-change summary, exception đã sanitize và comments.
- Dùng permission HCS.AuditViewer nhất quán với backend; giữ menu hiện có cho admin.
- Loading, empty, error/retry, 401/403, stale/eventual-consistency và responsive behavior.

Không làm trong MVP: sửa/xóa log, realtime streaming, dashboard biểu đồ, export CSV/PDF, request/response body, token/cookie, hoặc full before/after entity-property diff.

## Phases

| Phase | Deliverable | Status |
|---|---|---|
| Phase 01 — Contract và read query | Contract lọc, projection mapping, query/index/security hardening | Planned |
| Phase 02 — Blazor page và detail | Typed BFF client, page, filter, table, modal, localization/CSS | Planned |
| Phase 03 — Test và smoke verification | Test matrix, build, runtime smoke check và docs handoff | Planned |

Chi tiết: phase-01-audit-contract-and-query.md, phase-02-blazor-audit-page.md, phase-03-verification-and-documentation.md.

## Findings and dependencies

- Báo cáo backend: plans/reports/20260827-audit-logs-backend-review.md.
- Báo cáo UI: plans/reports/260827-blazor-audit-logs-ui-scan.md.
- Projection hiện nhận audit event từ Document, Organization, Work Management và Collaboration; Platform/AuthServer đang dùng ABP AbpAuditLogs nhưng chưa được project vào read model này.
- Client IP hiện lấy từ RemoteIpAddress; phải kiểm tra forwarded headers qua Caddy/YARP và chỉ trust proxy đã cấu hình.
- UserName cần smoke test để xác nhận là họ tên hiển thị thay vì subject/username. Nếu không đạt, bổ sung claim resolver ở phase 01.
- EntityChanges hiện chỉ là summary; before/after property values là backlog riêng vì có rủi ro PII/secret.

Nếu contract/index/schema thay đổi thì chạy migration Platform trước khi test query mới. Gateway route hiện có được giữ nguyên; không cần route mới. User/role phải được cấp HCS.AuditViewer.

## Success criteria

- Admin mở được /administration/audit-logs, tìm đúng record theo filter, đổi page/page size/sort và mở detail.
- API giới hạn 100 record/request, query có thứ tự ổn định, input được normalize/giới hạn và không lộ raw exception/body/token.
- Field trong screenshot được map rõ ràng; null hiển thị —, status có text + màu/icon, API/correlation dài không phá layout.
- Người không có HCS.AuditViewer nhận access denied/403; lỗi gateway/service có retry.
- Build/test/license audit đạt rule repository; smoke test qua https://hcs.localhost ở 375/768/1440px.

## Handoff

Sau khi duyệt plan, chạy cook tự động với file:

/ck:cook --auto /Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/plans/260827-1034-audit-logs-page/plan.md

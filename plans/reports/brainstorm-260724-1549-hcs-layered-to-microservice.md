---
title: Brainstorm — HCS layered → ABP microservice
date: 2026-07-24
status: approved
slug: 260724-1549-hcs-layered-to-microservice
sources:
  - services/HCS_web
  - services/abp-blazor
  - docs/handoff/phase1-sso-context.md
---

# Brainstorm — HCS_web (layered) → abp-blazor (microservice)

**Date:** 2026-07-24  
**Status:** Approved (Approach C)  
**Project:** BD / Hành chính số — rewrite domain lên MS

---

## Problem

`services/HCS_web` = ABP Commercial **layered monolith** (`HC.*`, Blazor Server + **Blazorise**, ~45 entity, ~67 feature, Documents+Workflow+Signing gắn chặt).

`services/abp-blazor` = ABP **microservice template** (`hanhchinhso.*`, MudBlazor hybrid) — platform + Keycloak SSO Phase 1 xong; **domain trống**; `workflow-service/` empty.

Cần chuyển **toàn bộ** nghiệp vụ sang microservice. Src cũ vẫn chạy → rewrite module-by-module, timeline dài OK.

## Requirements (user)

| # | Yêu cầu |
|---|---------|
| R1 | Rewrite toàn bộ module-by-module (không big-bang cutover tối) |
| R2 | Agent đề xuất thứ tự ưu tiên |
| R3 | Timeline dài chấp nhận được nếu end-state = full MS |
| R4 | Multi-tenant **shared DB** (không DB-per-tenant) |
| R5 | Tiến tới **Keycloak** (không Keycloak-only day-1) |
| R6 | **Mobile API + ký số REMOTE_CA** cần parity sớm (core path) |

## Decisions (approved 2026-07-24)

| ID | Quyết định | Giá trị |
|----|------------|---------|
| D1 | Migration style | **Approach C — Fat-core rồi peel** + strangler nhẹ (HCS_web sống đến khi parity) |
| D2 | Core HCS | Document + Workflow + Signing = **1 service** (`document-service` mới; **không** reuse `workflow-service` — đó là Elsa `:44395`) |
| D3 | DB | Shared DB cho tenants; **mỗi MS 1 database** trên cùng Postgres (`hanhchinhso_*`) |
| D4 | Auth path | Giữ Approach A (AuthServer federate KC); Phase cuối đánh giá mobile/KC sâu hơn |
| D5 | UI | Port **MudBlazor theo phase domain** — không mega-rewrite UI trước |
| D6 | Feature freeze | HCS_web: chỉ bugfix; feature mới trên MS |
| D7 | Plan granularity | Full roadmap một plan `260724-1555-hcs-layered-to-microservice`; cook từng phase (Phase 3 slices) |

## Approaches evaluated

| | Approach | Verdict |
|--|----------|---------|
| A | Big-bang carve-out hết service rồi cutover | Reject — dark period dài; lệch “parity sớm” |
| B | Strangler dual-run thuần (route từng API) | Hữu ích vận hành; sync data nặng nếu làm quá sớm |
| C | Fat-core rồi peel + HCS_web freeze | **Chọn** — KISS; parity ký số/mobile; rewrite đủ module |

## Target service map

```
abp-blazor
├── Identity / Administration / AuthServer / Gateway / Blazor Mud   # có sẵn + SSO
├── organization-service     # Departments, Units, Positions, UserDepartments, MasterDatas
├── document-service         # Documents + Workflow* + Signing + Mobile API + sign workers
├── project-service          # Projects / Tasks / Members
├── calendar-service
├── survey-service
├── collaboration-service    # Chat + Notifications + Push
└── reporting-service        # Reports + Signing KPI (+ legacy ETL sau)
```

## Roadmap (ưu tiên đề xuất)

| Phase | Scope | Done khi |
|-------|-------|----------|
| **0** | Convention MS, shared-DB policy, MinIO/Redis/Rabbit, permission prefix, Mud↔Blazorise map, feature inventory → checklist | Entity mẫu + runbook port chạy qua Gateway |
| **1** | organization-service + UI Mud CRUD | Org/MasterData parity |
| **2** | document-service (fat) + Mobile + REMOTE_CA/HSM | E2E trình ký + mobile contract = docs HCS |
| **3** | project-service | Projects/Tasks parity |
| **4** | calendar + survey | Parity từng cụm |
| **5** | collaboration (Chat/SignalR/Push) | Realtime + Firebase worker |
| **6** | reporting + KPI/legacy | Báo cáo + ETL nếu cần |
| **7** | Keycloak sâu + decommission HCS_web | Chỉ MS; archive layered |

## Port rules (mỗi module)

1. Domain/App/Contracts `HC.*` → `hanhchinhso.*` trong service đích  
2. EF migration **mới** (không copy nguyên chuỗi migration HCS)  
3. HttpApi + WebGateway route  
4. Blazorise → MudBlazor theo bảng map Phase 0  
5. Permission seed + KC group/role đã có (`bd-app-hcs`, roles lab)  
6. Checklist parity (feature inventory M01–M67)  

## Risks

| Risk | Mitigation |
|------|------------|
| Tách Doc/WF/Sign sớm → vỡ ký số | Fat document-service đến hết Phase 2 |
| Dual codebase | Freeze feature HCS_web |
| Mud ≠ Blazorise 1:1 | Map component; giữ flow, chấp nhận visual khác |
| Data → per-service DB | ETL theo phase; giữ `TenantId` |
| Secrets/signing plaintext cũ | Không mang pattern xấu sang MS |

## Success metrics

- 100% feature inventory có owner service + trạng thái parity  
- Mobile signing + REMOTE_CA pass scenario docs HCS  
- HCS_web tắt không mất chức năng  
- 1 Postgres host, N DB `hanhchinhso_*`, multi-tenant shared DB  

## Scout summary (context)

- HCS_web: .NET 10, ABP Commercial 10.0.1, Postgres schema `hcs`, Redis, RabbitMQ, MinIO, Bnn SignLib, LibreOffice, Firebase push, SignalR chat  
- abp-blazor: ABP 10.5 MS template, MudBlazor hybrid, Keycloak federation wire sẵn, domain trống  

## Next steps

1. User approve → **done**  
2. Hỏi tạo `/ck:plan` — khuyến nghị **Phase 0 + Phase 1** trước; Phase 2 plan riêng  
3. Cập nhật wiki `hot.md` + journal session  

## Unresolved (plan sau)

- Production hiện shared-DB confirmed; có tenant thật cần migrate data không / volume?  
- Mobile clients: giữ audience AuthServer hay dần trust KC? (Phase 7)  
- Volo.Forms trong HCS đang disabled — có bật lại trên MS không?  
- Tên chính thức `document-service` vs reuse folder `workflow-service`  

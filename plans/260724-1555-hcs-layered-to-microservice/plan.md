---
title: "HCS layered → ABP microservice (full roadmap 0→7)"
description: "Rewrite HCS_web domain vào abp-blazor MS: foundation → org → document/sign/mobile → project → calendar/survey → collab → reporting → Keycloak + decommission"
status: pending
priority: P1
effort: 20-28w
branch: main
tags: [feature, backend, frontend, abp, microservice, migration, hcs, mudblazor, keycloak]
blockedBy: []
blocks: []
relatedPlans:
  - 260724-1542-elsa-workflow-service
  - 260723-1419-bd-sso-phase1
created: 2026-07-24
createdBy: ck:plan
source: plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md
---

# HCS layered → ABP microservice (full roadmap)

## Overview

Rewrite toàn bộ nghiệp vụ `services/HCS_web` (ABP layered + Blazorise) sang `services/abp-blazor` (microservice + MudBlazor) theo **Approach C — fat-core rồi peel**.

- HCS_web: **feature freeze** (chỉ bugfix) đến parity từng module.
- Platform + SSO Keycloak (Approach A) **đã có** — không làm lại Phase 1 SSO.
- **document-service** = Documents + Workflow* + Signing + Mobile API (fat). **Không** dùng `workflow-service/` — folder đó thuộc plan Elsa (`:44395`).
- Multi-tenant: **shared DB** (TenantId filter; không DB-per-tenant trên target lab/prod BD trừ khi đổi quyết định).
- Mỗi MS: DB riêng trên cùng Postgres (`hanhchinhso_*`).
- UI: port MudBlazor **theo phase**, không mega-rewrite trước.

## Scope challenge (HOLD — user chọn full 0→7)

| Q | Answer |
|---|--------|
| Already exists | Platform MS + KC SSO; WorkflowService scaffold = **Elsa**, không phải HCS doc workflow |
| Minimum vs full | User **explicit** full roadmap một plan |
| Complexity | 8 phases, nhiều service — chấp nhận; cook **từng phase**, không cook cả plan một lần |

## Cross-plan

| Plan | Relationship |
|------|----------------|
| [SSO Phase 1](../260723-1419-bd-sso-phase1/plan.md) | **Completed** — prerequisite auth |
| [Elsa WorkflowService](../260724-1542-elsa-workflow-service/plan.md) | **Orthogonal** — owns `services/workflow-service/` + Elsa Studio. HCS dùng `document-service/`. Có thể parallel nếu không đụng cùng file AuthServer seeder cùng lúc |

## Locked decisions

| ID | Value |
|----|-------|
| D1 | Approach C fat-core + HCS freeze |
| D2 | Core = `hanhchinhso.DocumentService` (NOT Elsa WorkflowService) |
| D3 | Shared DB tenants + DB-per-microservice |
| D4 | AuthServer federate KC; Phase 8 đánh giá mobile/KC sâu |
| D5 | MudBlazor per domain phase |
| D6 | Pattern scaffold = LanguageService (Contracts + Host + Tests, flat) |
| D7 | Cook theo phase; Phase 3 (document) có thể tách cook sessions |

## Target services & ports

| Service | Folder | Port | DB |
|---------|--------|------|-----|
| OrganizationService | `services/organization/` | 44370 | `hanhchinhso_Organization` |
| DocumentService | `services/document/` | 44380 | `hanhchinhso_Document` |
| ProjectService | `services/project/` | 44381 | `hanhchinhso_Project` |
| CalendarService | `services/calendar/` | 44382 | `hanhchinhso_Calendar` |
| SurveyService | `services/survey/` | 44383 | `hanhchinhso_Survey` |
| CollaborationService | `services/collaboration/` | 44384 | `hanhchinhso_Collaboration` |
| ReportingService | `services/reporting/` | 44385 | `hanhchinhso_Reporting` |
| WorkflowService (Elsa) | `services/workflow-service/` | 44395 | `hanhchinhso_Workflow` — **out of this plan** |

## Phases

| Phase | Name | Status |
|-------|------|--------|
| 1 | [Foundation & conventions](./phase-01-foundation-conventions.md) | Completed |
| 2 | [Organization service](./phase-02-organization-service.md) | Completed — custom Department removed; ABP OU is canonical |
| 3 | [Document service (fat + mobile + sign)](./phase-03-document-service.md) | In progress (slices 3a–3c4 implemented) |
| 4 | [Project service](./phase-04-project-service.md) | Pending |
| 5 | [Calendar + Survey](./phase-05-calendar-survey.md) | Pending |
| 6 | [Collaboration (Chat/Notify/Push)](./phase-06-collaboration.md) | Pending |
| 7 | [Reporting + Signing KPI](./phase-07-reporting-kpi.md) | Pending |
| 8 | [Keycloak deepen + decommission HCS](./phase-08-keycloak-decommission.md) | Pending |

**Effort (approx):** P1 1–2w · P2 1–2w · P3 6–10w · P4 2–3w · P5 2–3w · P6 3–4w · P7 1–2w · P8 1–2w · **Total ~20–28w**

## Port rules (mọi domain phase)

1. Clone LanguageService scaffold → rename `hanhchinhso.{X}Service`
2. Port Domain logic từ `HC.*` → namespace `hanhchinhso.*` (không copy 76 migration HCS — migration EF mới)
3. Wire: `.abpsln`, gateway YARP, `Default.abprun.json`, OpenIddict API scope/resource + Blazor client scopes, connection string
4. Blazorise pages → MudBlazor theo map Phase 1
5. Permission definitions + seed roles lab (`admin|bacsi|lanhdao|nhanvien`) + gate `bd-app-hcs`
6. Parity checklist từ feature inventory
7. Data ETL optional: script copy `TenantId` + PKs khi cần lab data

## Architecture (end state)

```
Browser MudBlazor :44306
    → WebGateway :44398
        → Organization :44370
        → Document :44380  (+ MinIO, LibreOffice sidecar, Sign workers)
        → Project :44381
        → Calendar :44382
        → Survey :44383
        → Collaboration :44384 (+ SignalR, Push worker)
        → Reporting :44385
    → AuthServer :44372 ← Keycloak :5110
Mobile  → AuthServer tokens → Gateway/Document APIs (parity Phase 3)
```

## Success criteria (whole plan)

- [ ] Feature inventory HCS có owner service + status parity
- [ ] Mobile + REMOTE_CA/HSM E2E trên DocumentService
- [ ] HCS_web process tắt được; archive repo/folder
- [ ] Shared DB + N× `hanhchinhso_*` DBs
- [ ] Không đụng Elsa WorkflowService trừ integration chủ đích sau này

## Risks

| Risk | Mitigation |
|------|------------|
| Phase 3 quá lớn | Cook sub-slices trong phase file; freeze scope peel |
| Elsa vs Document confusion | Folder/port tách; doc ADR |
| Dual maintain | Feature freeze HCS |
| Mud ≠ Blazorise | Component map; parity = flow không pixel |
| Secrets ký số plaintext | User Secrets / env; không commit |
| AuthServer seeder conflict với Elsa plan | Serialize edits OpenIddict seeder |

## Research

- [researcher-01 ABP scaffold](./research/researcher-01-abp-ms-scaffold.md)
- [researcher-02 HCS domain](./research/researcher-02-hcs-domain-surface.md)
- Brainstorm: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`

## NOT in scope

- Zimbra LDAP (SSO Phase 2 riêng)
- GitHub/CI/remote deploy
- Pixel-perfect Blazorise clone
- Keycloak-only (bỏ AuthServer) trước Phase 8 decision
- Merge HCS workflow vào Elsa (optional later)

## Cook guidance

```text
# Chỉ cook 1 phase / session (bắt buộc Phase 3)
/ck:cook --auto /Users/user/Documents/bd-workspace/plans/260724-1555-hcs-layered-to-microservice/phase-01-foundation-conventions.md
```

Không `/ck:cook` cả `plan.md` một lần.

## Unresolved (chốt khi cook phase liên quan)

1. Có migrate data production HCS → MS hay greenfield lab only?
2. LibreOffice: sidecar container vs remote HTTP?
3. Volo.Forms (disabled HCS) — bỏ hẳn?
4. Phase 8: mobile trust KC trực tiếp hay giữ AuthServer audience?

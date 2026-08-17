---
type: meta
title: "Wiki Log"
updated: 2026-07-25
---

# Wiki Log

## 2026-07-25 — Directus v11 SSO lab cook COMPLETED
- Plan: `plans/260725-1726-directus-v11-sso-lab/` DONE ✅ (4 phases, ~5h cook)
- Evidence: Phase 1 (artifacts ported), Phase 2 (image+stack+roles), Phase 3 (OIDC login OK + gate deny log), Phase 4 (docs updated)
- Status: All success criteria met; runbook/docs point v11 only; v12 marked archive
- Next: Any remaining Zimbra LDAP federation → Phase 2

## 2026-07-25 — Directus lab → v11 plan
- Plan: `plans/260725-1726-directus-v11-sso-lab/` (4 phases, ~5h, P1, pending)
- Brainstorm: `plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md`
- Journals: `docs/journals/260725-directus-v11-sso-brainstorm.md`, `docs/journals/260725-directus-v11-sso-plan.md`
- Next: `/ck:cook --auto` plan.md

## 2026-07-25 — Directus lab SoT → v11 (brainstorm)
- APPROVED: thay lab Directus `directus-main` (v12) bằng `directus-main-v11` (11.13.4)
- Lý do chính: v11 không license-gate SSO; OpenID + ROLE_MAPPING sẵn; port compose + `bd-app-axis` gate
- Report: `plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md`
- Journal: `docs/journals/260725-directus-v11-sso-brainstorm.md`

## 2026-07-24 — ABP production deploy runbook
- Added `docs/runbooks/deploy-abp-production.md` (Ubuntu 24+, Docker+Nginx / K8s+Helm)
- Linked from `services/abp-blazor/README.md`; indexed in `wiki/index.md` + `wiki/hot.md`

## 2026-07-24 — HCS→MS full roadmap plan
- Created `plans/260724-1555-hcs-layered-to-microservice/` (8 phases)
- DocumentService `:44380` ≠ Elsa WorkflowService `:44395`
- Red team: pass-with-fixes; cook per phase / Phase3 slices
- Brainstorm source: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`

## 2026-07-24 — HCS layered → microservice brainstorm
- Approach C approved: fat-core rồi peel; Doc+WF+Sign = 1 service; shared DB; tiến KC; mobile/REMOTE_CA Phase 2
- Report: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`
- Decision: [[HCS Layered to Microservice Approach C]]
- Next: `/ck:plan` Phase 0+1 (khuyến nghị)

## 2026-07-23 — Axis rebrand Directus source
- Prefix bảng/collection `directus_*` → `axis_*` (~5094 chỗ); display Directus→Axis (en-US + UI/API fallbacks)
- `SYSTEM_COLLECTION_PREFIX` + `stripSystemCollectionPrefix`; wipe volume lab `bd_axis_*`; re-bootstrap KC
- Caveat: compose vẫn `directus/directus:11.9.2` → runtime tables còn `directus_*` đến khi build image từ fork
- Plan: `plans/260723-1617-axis-rebrand-directus/`

## 2026-07-23 — App access gate
- Groups `bd-app-axis` / `bd-app-hcs`; Directus hook + ABP AuthServer fail nếu thiếu
- Plan: `plans/260723-1555-bd-app-access-gate/`
- Docs: runbook + handoff + hot updated

## 2026-07-23 — Phase 1 SSO COMPLETE
- Handoff AI: `docs/handoff/phase1-sso-context.md`
- Decision: [[SSO Phase 1 Complete]]
- Delivered: KC realm bd, Directus OpenID+ROLE_MAPPING+prompt=login, ABP Approach A+permissions+prompt=login
- Next: Phase 2 Zimbra LDAP

## 2026-07-23 — SSO Phase 1 plan
- Created `plans/260723-1419-bd-sso-phase1/` (4 phases). Ready cook.

## 2026-07-23 — SSO Phase 1 brainstorm approved
- Approach A + KC local + localhost + roles admin/bác sĩ/lãnh đạo/nhân viên.
- Report: `plans/reports/brainstorm-260723-1415-bd-sso-login-flow.md`
- Decision page: [[SSO Phase 1 Approach A]]

## 2026-07-23 — BD reset
- Rewrite agent docs: README, CLAUDE, AGENTS, SKILLS, llms.txt.
- Rewrite docs/: workspace-architecture, PDR, system-architecture, code-standards, codebase-summary.
- Reset wiki hot/index; add BD domain/concept pages; mark Task9 as ARCHIVE.
- Update `.claude/launch.json` + `start-local` skill stub for BD.
- Reason: copy `.claude`/`.agents` từ Task9 vào workspace Bình Dương SSO lab (Directus + ABP + Keycloak), local-only.

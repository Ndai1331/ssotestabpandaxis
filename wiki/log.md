---
type: meta
title: "Wiki Log"
updated: 2026-07-23
---

# Wiki Log

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

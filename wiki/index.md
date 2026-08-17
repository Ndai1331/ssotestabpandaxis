---
type: meta
title: "Wiki Index — BD Second Brain"
updated: 2026-07-25
---

> Đọc thứ tự tối ưu token: `hot.md` → `index.md` → drill page qua wikilink.

# Wiki Index — BD (Bình Dương SSO Lab)

## Domains
- [[BD Platform Overview]] — Workspace lab SSO Zimbra + Keycloak + Directus + ABP
- [[BD SSO Architecture]] — Luồng login, federation, role mapping, DB tách biệt

## Codebase
- [[Codebase — Directus]] — lab target `services/directus-main-v11` (v12 `directus-main` = archive sau cook); KC `:5110`
- [[Codebase — ABP Blazor]] — `services/abp-blazor`, AuthServer + Blazor + microservices

## Concepts
- [[Keycloak Local Lab]] — Port, admin, realm/clients checklist
- [[OIDC Client Mapping]] — Directus + ABP nhận token từ Keycloak

## Decisions
- [[SSO Phase 1 Approach A]] — AuthServer federate KC; 4 roles; localhost POC
- [[SSO Phase 1 Complete]] — Phase 1 DONE; handoff `docs/handoff/phase1-sso-context.md`
- Directus lab → v11 SSO — brainstorm APPROVED `plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md`
- [[HCS Layered to Microservice Approach C]] — Fat-core peel; Doc+WF+Sign chung 1 service; Mud theo phase
- [[Local-Only Phase]] — Chưa GitHub/CI/deploy; sunset Task9 rules

## Meta / Archive
- [[ARCHIVE Task9]] — Wiki/plans Task9 cũ — không dùng làm SoT cho BD

## Sources
- [[workspace-architecture.md]] — SoT kiến trúc (docs/) — mirror ý trong domains
- `docs/handoff/phase1-sso-context.md` — **AI prompt context Phase 1**
- `docs/runbooks/local-sso-lab.md` — Runbook vận hành lab SSO
- `docs/runbooks/deploy-abp-production.md` — Deploy ABP prod (Docker+Nginx / K8s)
- `system-sso-guideline.png` — Diagram gốc SSO

## Active Plans
- **HCS→MS:** `plans/260724-1555-hcs-layered-to-microservice/` (**pending**, active)
- Elsa (orthogonal): `plans/260724-1542-elsa-workflow-service/`
- Phase 1 SSO archive: `plans/260723-1419-bd-sso-phase1/` (**complete**)
- Brainstorm HCS: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`
- Next SSO: Phase 2 Zimbra LDAP (chưa có plan formal)

---
type: meta
title: "Hot Cache"
updated: 2026-07-24T16:30:00
---

# Hot Cache — BD Second Brain

## Last Updated
2026-07-24 — **ABP AppHost CLI runner:** Aspire 13.4.6 (`./aspire/run.sh light|full`) replaces manual ABP Studio for local dev — see [`services/abp-blazor/aspire/README.md`](../services/abp-blazor/aspire/README.md).
2026-07-24 — **Elsa WorkflowService DONE:** Plan `plans/260724-1542-elsa-workflow-service/` completed. WorkflowService `:44395` (Elsa Pro 3.5 + Contracts + Tests), Elsa Studio WASM `:44396`, menu link in Blazor, Keycloak auth via AuthServer OpenIddict, permission seed `Elsa.*`. All 8 phases done; smoke verify checklist complete.
2026-07-24 — **Plan HCS→MS full roadmap:** `plans/260724-1555-hcs-layered-to-microservice/` (8 phases, ~20–28w). DocumentService `:44380` ≠ Elsa WorkflowService `:44395`. Cook từng phase; Phase 3 slices 3a–3h. Active plan set.
2026-07-24 — **HCS→MS brainstorm APPROVED:** Approach C fat-core rồi peel. Report: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`. Source `HCS_web` (layered/Blazorise) → target `abp-blazor` (MS/Mud). Phase 0–7; document+WF+sign = 1 fat service trước. Shared DB tenants; tiến KC; mobile/REMOTE_CA parity Phase 2. Chưa `/ck:plan`.
2026-07-23 — **Axis rebrand (source):** table/collection prefix `directus_`→`axis_`; UI Directus→Axis (en-US). Plan: `plans/260723-1617-axis-rebrand-directus/`. **Caveat:** `docker-compose.bd-lab.yml` vẫn image upstream → runtime DB còn `directus_*` đến khi build fork.
2026-07-23 — **App access gate:** groups `bd-app-axis` / `bd-app-hcs`; Directus hook + ABP OnTokenValidated fail. Plan: `plans/260723-1555-bd-app-access-gate/`. Re-run bootstrap + restart Directus/AuthServer.
2026-07-23 — **Phase 1 SSO DONE.** Handoff AI: `docs/handoff/phase1-sso-context.md`. Runbook: `docs/runbooks/local-sso-lab.md`. Xem [[SSO Phase 1 Complete]].
2026-07-23 — Permission seed ABP roles bacsi/lanhdao/nhanvien; Directus+ABP `prompt=login` (lab logout UX).
2026-07-23 — Directus lab compose: KC hostname `localhost:5110` + backchannel dynamic; ROLE_MAPPING UUIDs filled.
2026-07-23 — Reset workspace Task9 → BD SSO Lab.

## Code Structure Cheatsheet
- **Directus** `services/directus-main` + `docker-compose.bd-lab.yml` (Directus+PG+Redis+KC)
- **ABP** `services/abp-blazor` — AuthServer `:44372` federate KC; Blazor `:44306`
- **Bootstrap** `scripts/keycloak_bootstrap_bd_realm.py` (re-run after KC recreate)
- **Handoff** `docs/handoff/phase1-sso-context.md` ← dán vào prompt chat mới

## Key Recent Facts
- Approach A; realm `bd`; role groups `bd-admin|bacsi|lanhdao|nhanvien`; app groups `bd-app-axis|bd-app-hcs`
- Lab users mặc định cả 2 app; bỏ 1 app group trên KC Admin để test single-app
- KC users `*@benhvien.vn` / `Passw0rd!`; Directus local `admin@local.dev`/`admin123456`; ABP `admin@abp.io`/`Abc@123`
- Browser chỉ `localhost:5110` (không host.docker.internal)
- `prompt=login` bật → dễ đổi user; silent SSO 2-app cần tắt prompt hoặc test có chủ đích

## Active Threads / Open Plans
- **HCS→MS:** `plans/260724-1555-hcs-layered-to-microservice/` — cook Phase 01 foundation trước
- **Elsa WorkflowService:** `plans/260724-1542-elsa-workflow-service/` — orthogonal; `:44395`
- **SSO Phase 2:** Zimbra LDAP User Federation
- Optional: SLO, bỏ/ tune `prompt=login`, full permission matrix

## Critical Rules
- Đọc `docs/handoff/phase1-sso-context.md` + `CLAUDE.md` + `docs/workspace-architecture.md`
- Không follow Task9 rules
- Commit chỉ khi user yêu cầu

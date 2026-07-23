---
type: meta
title: "Hot Cache"
updated: 2026-07-23T15:40:00
---

# Hot Cache — BD Second Brain

## Last Updated
2026-07-23 — **App access gate:** groups `bd-app-directus` / `bd-app-abp`; Directus hook + ABP OnTokenValidated fail. Plan: `plans/260723-1555-bd-app-access-gate/`. Re-run bootstrap + restart Directus/AuthServer.
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
- Approach A; realm `bd`; role groups `bd-admin|bacsi|lanhdao|nhanvien`; app groups `bd-app-directus|bd-app-abp`
- Lab users mặc định cả 2 app; bỏ 1 app group trên KC Admin để test single-app
- KC users `*@benhvien.vn` / `Passw0rd!`; Directus local `admin@local.dev`/`admin123456`; ABP `admin@abp.io`/`Abc@123`
- Browser chỉ `localhost:5110` (không host.docker.internal)
- `prompt=login` bật → dễ đổi user; silent SSO 2-app cần tắt prompt hoặc test có chủ đích

## Active Threads / Open Plans
- **Phase 2:** Zimbra LDAP User Federation
- Optional: SLO, bỏ/ tune `prompt=login`, full permission matrix

## Critical Rules
- Đọc `docs/handoff/phase1-sso-context.md` + `CLAUDE.md` + `docs/workspace-architecture.md`
- Không follow Task9 rules
- Commit chỉ khi user yêu cầu

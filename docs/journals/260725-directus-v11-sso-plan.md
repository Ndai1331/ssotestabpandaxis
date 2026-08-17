# Plan Created — Directus v11 SSO Lab (replace v12)

**Date**: 2026-07-25 17:26  
**Severity**: Medium  
**Component**: Directus + Keycloak SSO lab  
**Status**: Pending Cook  

## What Happened

Approved brainstorm (2026-07-25 17:21) → formalized implementation plan. Plan dir: `plans/260725-1726-directus-v11-sso-lab/` with 4-phase roadmap: port compose + extensions → boot + role mapping → smoke SSO login + gate → docs SoT + archive v12. Effort estimate ~5h, priority P1. User will next run `/ck:cook --auto` to execute.

## The Brutal Truth

This is a **clean decision** — switching from v12 (license-gated SSO) to v11 (built-in OpenID driver) removes technical debt without adding complexity. The brainstorm was focused and approved quickly. Feels right because v11 gives us what we need without the license bypass stain. But there's a shadow here: we've been running v12 in lab for weeks. The fact that switching to v11 is actually *simpler* means we should have considered it earlier. Hindsight.

That said — moving fast now. Plan is clear, dependencies are orthogonal (ABP HCS plan stays untouched), and phasing is real.

## Technical Details

**Phase breakdown:**
1. **Port compose + extension + env** (~1.5h): Copy `docker-compose.bd-lab.yml` (drop `BD_LAB_ALLOW_SSO`), reuse `bd-lab-extensions/directus-extension-bd-app-gate/`, port `.env.sso.example`, test Dockerfile build
2. **Boot + roles + ROLE_MAPPING** (~1.5h): Keycloak bootstrap (reuse `scripts/keycloak_bootstrap_bd_realm.py`), seed role UUIDs via first-boot manual paste (not copy from v12)
3. **Smoke SSO + gate** (~1h): Login via Keycloak as `*@benhvien.vn`, verify gate requires `bd-app-axis` claim, test ABP Keycloak parity (no ABP changes)
4. **Docs SoT + archive v12** (~1h): Update `docs/workspace-architecture.md`, `CLAUDE.md`, `wiki/hot.md`; mark v12 path as archive/lab-unmaintained

**No blockers vs. HCS plan** — SSO Keycloak is shared, but ABP AuthServer clients/secrets unchanged. HCS microservice migration runs in parallel.

## Root Cause Analysis

v12 was "working" but required a runtime license bypass (`BD_LAB_ALLOW_SSO` env gate in Directus core). This is a code smell: when a version has native feature support (v11 OpenID built-in), we should prefer it over custom gates. We chose v12 initially because it was latest; we didn't validate whether newer = better-for-lab.

## Lessons Learned

1. **Built-in feature > custom gate every time**: If v11 has `OpenID driver` and v12 adds license gate, choose v11 for lab. Always prefer native over patch.
2. **Plan before commit to version**: Next time we pick a version for lab, ask: "Does this version have the feature I need without license bypass?" Validate feasibility before weeks pass.
3. **Fast brainstorm + plan + cook is the move**: This was approved + planned in 5 min. No red-team, no hedging. Trust the design when it's clear.

## Next Steps

1. User runs `/ck:cook --auto` on `plans/260725-1726-directus-v11-sso-lab/plan.md`
2. Cook phases 1–4 sequentially
3. Success: Compose v11 lab up, Keycloak `:5110` bootstrap done, login → gate enforced, docs point v11
4. Archive v12 path + close loop

## References

- Brainstorm report: `plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md`
- Plan directory: `plans/260725-1726-directus-v11-sso-lab/`
- Prior SSO phase 1: `plans/260723-1419-bd-sso-phase1/phase-02-directus-openid.md`
- v12 compose (archive): `services/directus-main/docker-compose.bd-lab.yml`
- Gate extension: `services/directus-main/bd-lab-extensions/directus-extension-bd-app-gate/`

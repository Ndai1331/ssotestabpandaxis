# Phase 04 — Validate, document, and hand off

## Overview

Priority P1 · In progress · 20% (1/5 steps) · Estimate 2h · Depends on Phases 02–03. Prove real browser behavior and leave a reproducible local-lab recovery path.

Status evidence: builds passed per implementation handoff. No test, gateway-health, browser, SSO, or README validation evidence is recorded; the phase completion gate is not met.

## Steps

1. [x] Build targeted Blazor Client/host/gateway projects, then `dotnet build hanhchinhso.abpsln`; build passed per implementation handoff (command output not persisted here).
2. [ ] Add/extend component tests where the solution’s existing test infrastructure supports them: safe return URL, unauthenticated/authenticated rendering, and quick-link availability. If no WASM component-test harness exists, record the gap rather than introducing a test framework solely for this UI.
3. [ ] Run integration checks: gateway `/health-status`, CORS OPTIONS, route through WebGateway, then a real post-hydration WASM request.
4. [ ] Execute E2E smoke in a clean browser: `/` anonymous, `/login`, AuthServer/Keycloak redirect/callback, refresh, a protected Organization/Document route, logout and back navigation.
5. [ ] Update local ABP README/Aspire README with exact run order, relevant origins/ports, diagnostic table for `Failed to fetch`, restart/hard-refresh instructions, and known limits of the MVP workspace.

## Success criteria

- All plan-level test cases produce recorded results; no browser CORS/mixed-content/`Failed to fetch` for proven route.
- No secret or token appears in docs, logs, test artifacts, or source.
- User can restart local stack, hard-refresh, and reproduce successful SSO/workspace flow from README.

## Risks / rollback

| Risk | L×I | Mitigation and rollback |
|---|---:|---|
| Local Keycloak/infra unavailable | M×M | run compile/static checks; mark E2E pending with dependency and do not claim pass. |
| Docs become stale after port changes | M×M | source URLs from effective config and verify after restart; revert docs with related config if Phase 02 is reverted. |

## Completion gate

Only mark complete when build, browser fetch, SSO round-trip, workspace state, and README verification are observed.

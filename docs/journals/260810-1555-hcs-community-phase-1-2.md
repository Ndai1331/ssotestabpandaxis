# HCS Community Phase 1–2: đúng hướng, chưa được phép tự mãn

**Date**: 2026-08-10 15:55
**Severity**: High
**Component**: HCS Community Blazor, Web Gateway BFF, AuthServer, navigation
**Status**: Ongoing

## What Happened

Phase 1 wired anonymous protected UI routes through `BffRedirectToLogin` in `src/HCS.Blazor.Client/Routes.razor`, preserving a validated deep link through the existing BFF → AuthServer → Keycloak flow. The BFF rejects unsafe return origins, while proxied API and hub calls keep `401/403` rather than receiving browser redirects. Phase 2 made Chat a real permission-gated page under `Collaboration.Chat` and left it as the only explicitly enabled Community business menu item; excluded commercial menus were removed.

## The Brutal Truth

This feels better than the old skeleton, but it is still dangerously easy to mistake movement for completion. We started Phases 1–2 before finishing the Phase 0 acceptance inventory. That was the mistake: now the code has a safer login path and one honest menu item, yet 46 routes can still look like product surface while most are generic placeholders. That is exactly the kind of false progress that wastes a later team's night.

## Technical Details

Targeted regression passed on 2026-08-10: `HCS.WebGateway.Tests` was **38/38** and `HCS.AuthServer.Tests` was **11/11**. The security review chose one BFF login endpoint with an absolute-origin allowlist, no client-side token exposure, and no redirect of `/api/**` or `/hubs/**`. It also kept the Community boundary clean: commercial modules, packages, migrations, UI assets, and credentials were not copied. The remaining security evidence is not optional: there is no real-browser Keycloak trace for deep-link callback, logout/back, expired cookie, forbidden user, or CORS/antiforgery rejection.

## What We Tried

- Reused the existing BFF and Keycloak provisioning path instead of adding a second browser OIDC client or password form.
- Enabled only Chat after gating it with `Collaboration.Chat`; broad menu enablement was rejected because policy/API parity does not yet exist.
- Kept generic `BusinessFeature.razor`/`GatewayDataPanel` routes for compatibility, but they were not accepted as migrated features.

## Root Cause Analysis

We did not freeze the route/menu/permission/API acceptance matrix first. Without that inventory, implementation can protect a route without proving that the route deserves to exist, has a real vertical behind it, or is authorized consistently at menu, UI, gateway, and service layers.

## Lessons Learned

Do not call a rendered placeholder feature parity. Gate every menu item behind a mapped capability, policy-backed API, non-placeholder UI, and negative authorization test. Unit tests are necessary but do not prove the browser/OIDC boundary.

## Next Steps

- Main implementation agent and product owner: complete and approve the Phase 0 matrix for all 46 routes before declaring Phase 1 or 2 complete.
- Auth/Gateway owner: run and record the real-Keycloak browser matrix, including logout, expiry, denial, API/hub `401`, and CORS/antiforgery rejection, before release readiness is claimed.
- Blazor and service owners: hide or replace each placeholder route; enable it only after its policy, API, UI, and negative authorization coverage are accepted.

# Phase 03 — Add SSO login landing and workspace home

## Overview

Priority P1 · In progress · 60% (3/5 steps) · Estimate 4h · Depends on Phase 02. Replace template marketing cards with a Vietnamese administrative workspace that follows the licensed HCS page’s information hierarchy without importing business-only widgets or licensed material.

Status evidence: `Index.razor*` replaces ABP starter content; `Login.razor*` provides SSO entry and local-only return-url validation. Build passed per implementation handoff. Responsive, authorization, deep-link, and post-login behavior have not been browser-verified.

## UX and data flow

```text
Anonymous / or /login → public landing → /Account/Login → existing OIDC challenge
Authenticated callback → / → workspace greeting + permitted quick links
Unavailable feature → disabled/coming-soon state; no remote call
```

Use existing `CascadingAuthenticationState`, `CurrentUser`, `RedirectToLogin`, MudBlazor/LeptonX components, and `NavigationManager`. The SSO button must use the framework endpoint; it must not collect passwords or issue token requests in WASM.

## Related code files

- Modify: `apps/blazor/hanhchinhso.Blazor.Client/Pages/Index.razor`, `Index.razor.cs`, `Index.razor.css`.
- Optional, only if separate public URL improves the verified flow: create `Pages/Login.razor` and `Login.razor.css`; adjust `Routes.razor` and localization JSON for route text.
- Do not modify HCS reference code, AuthServer login views, domain services, or gateway code in this phase.

## Steps

1. [ ] Extract functional content map from `services/HCS_web_with_license/src/HC.Blazor/Components/Pages/Index.razor`: auth gate, filter/KPIs, activity widgets, quick actions. Mark each target item implemented, deferred, or unavailable.
2. [x] Replace ABP starter content with a Vietnamese anonymous/authenticated workspace and authorized Organization quick link.
3. [x] Implement `/login` SSO entry with a same-app local return URL and full navigation to `/Account/Login`.
4. [x] Use responsive MudBlazor composition and page-scoped CSS; use text/icons already present, not HCS proprietary assets.
5. [ ] Validate direct deep link, browser Back, refresh after callback, missing permission, and service unavailable state.

## Success criteria

- Anonymous user sees Vietnamese SSO entry, no password fields, then receives existing Keycloak-authenticated session.
- Authenticated user sees workspace instead of ABP starter content; every visible enabled action maps to an existing, authorized route.
- Mobile and desktop layouts contain no clipped primary action at 320px and 1280px.

## Risks / compatibility / rollback

| Risk | L×I | Mitigation and rollback |
|---|---:|---|
| Public page bypasses route protection | M×H | only landing anonymous; all business routes stay `[Authorize]`; test protected direct URL. |
| Return URL enables open redirect | M×H | accept only relative local paths; otherwise `/`; test malicious absolute URL. |
| UI invokes unfinished HCS service | H×M | links only to present pages; explicitly defer widgets; remove independent page/style files to rollback. |

## Next

Phase 04 performs full local regression and documents the recovery runbook.

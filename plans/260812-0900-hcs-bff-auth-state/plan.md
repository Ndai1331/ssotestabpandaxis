---
title: "HCS Blazor BFF authentication state synchronization"
description: "Make the Blazor authentication cascade reflect the authenticated BFF cookie after OIDC login."
status: pending
priority: P1
effort: 4h
branch: main
tags: [bugfix, frontend, auth, bff, blazor]
blockedBy: []
blocks: []
created: 2026-08-12
---

# HCS Blazor BFF authentication state synchronization

## Overview

`/bff/user` authenticates after the OIDC callback, but the client-side `CascadingAuthenticationState` remains anonymous. This causes a protected route to render while menu/header components still show Login. Add one BFF-backed `AuthenticationStateProvider`, make the gate use that same source of truth, then verify the real Docker flow in a private browser window.

## Cross-plan context

Related to [HCS Community feature parity](../260810-0900-hcs-community-feature-parity/plan.md), Phase 1. This is a focused corrective plan within that phase, not a separate blocker: the established BFF endpoint, cookie, Keycloak client, and database schema stay unchanged.

## Scope and guardrails

- Keep browser tokens inaccessible: read only the already-sanitized claims returned by `GET /bff/user`; do not add browser OIDC, token storage, or a second login path.
- Preserve `401/403` behavior for API/hub requests and the gateway return-URL allowlist.
- Treat a failed/non-200 BFF profile request as anonymous; avoid throwing during app startup.
- Remove temporary agent debug telemetry from the authentication gate while touching it.

## Phases

| # | Phase | Status | Files owned |
|---|---|---|---|
| 1 | [Create a single BFF-backed auth state](./phase-01-bff-auth-provider.md) | Pending | `src/HCS.Blazor.Client/Authentication/**`, `HCSBlazorClientModule.cs`, `Routes.razor`, `Navigation/HCSMenuContributor.cs` |
| 2 | [Add regression coverage](./phase-02-regression-tests.md) | Pending | client test project/files; existing gateway tests only if endpoint contract needs coverage |
| 3 | [Rebuild and validate the Docker login flow](./phase-03-docker-validation.md) | Pending | no production source edits |

## Acceptance criteria

- After one successful login, `/bff/user`, `AuthenticationState.User.Identity.IsAuthenticated`, Lepton menu authorization, and header account UI agree on the same authenticated principal; the intentional empty sidebar is replaced by the authenticated **Trao đổi** entry.
- The recovery menu item **Trao đổi** is visible only to authenticated users; an anonymous, expired-cookie, or failed-profile case has no authenticated UI. Existing service-level permission checks remain authoritative.
- No access/refresh/ID token is emitted to JavaScript, logs, test artifacts, or configuration.
- Targeted tests and `dotnet build HCS.slnx --no-restore` pass; rebuilt `blazor` and `web-gateway` containers pass an incognito login/refresh/logout smoke test at `https://hcs.localhost`.

## Risks and rollback

| Risk | Mitigation |
|---|---|
| DTO/claim parsing diverges from Gateway response | Keep one internal DTO matching `isAuthenticated`, `name`, and public `claims`; test 200, 401, malformed payload, and no claims. |
| Two components fetch/notify independently | Provider owns retrieval and `NotifyAuthenticationStateChanged`; the gate awaits its state instead of maintaining a separate boolean decision. |
| Docker image uses stale client artifacts | Explicitly rebuild/recreate the `blazor` and `web-gateway` services, then test the public Caddy URL in a new incognito session. |

## Execution order

1. Implement Phase 1 and compile the client/solution.
2. Implement and run Phase 2 tests.
3. Rebuild/restart Phase 3 services and record browser/network evidence for first login, refresh, logout, expiry/anonymous access, and a permitted menu item.

## Unresolved questions

None. The gateway’s existing public-claim contract is sufficient for this correction.

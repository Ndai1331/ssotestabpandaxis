# Phase 02 — Apply minimal gateway/client transport correction

## Overview

Priority P1 · In progress · 40% (2/5 steps) · Estimate 2h · Depends on Phase 01. Make the smallest evidence-backed change that lets hydrated WASM requests reach the gateway safely.

Status evidence: client `RemoteServices:Default` now targets `http://localhost:44398/`; host `DocumentService` points at the same gateway. Build passed per implementation handoff. This is not runtime evidence and Phase 01 remains the blocking acceptance gate.

## Architecture / possible changes

- Client: `RemoteServices` base URLs must match the real gateway origin and path.
- Gateway: allow only the Blazor origin; preserve credentials/exposed ABP headers and the existing YARP route/cluster path.
- Host: only align `RemoteServices`/OIDC metadata if SSR and WASM contracts demonstrably differ.
- HTTPS: choose one consistent local scheme based on launch profile and trusted development certificate; never “fix” by wildcard CORS or disabling auth.

## Related code files

- Modify only evidence-selected files from: client `wwwroot/appsettings*.json`; host `appsettings*.json`, `hanhchinhsoBlazorModule.cs`; gateway `appsettings*.json`, `hanhchinhsoWebGatewayModule.cs`.
- No new service, client secret, scope, or Keycloak client.

## Steps

1. [x] Preserve existing user diffs while updating the selected client/host configuration files.
2. [x] Apply the current source-level gateway URL/service mapping correction.
3. [ ] Retain trailing-slash and `AllowCredentials` compatibility expected by ABP; do not introduce `AllowAnyOrigin` with credentials.
4. [ ] Restart only affected local apps; run gateway health, CORS preflight, anonymous and authenticated proxy checks.
5. [ ] Verify client-side post-hydration request in a fresh browser session; document exact restart/hard-refresh action.

## Success criteria

- Browser preflight and real request succeed without CORS/mixed-content/network exception.
- Auth failure remains a normal 401/403, not a network failure; authorized request reaches expected service.
- Existing AuthServer login and gateway Swagger remain usable.

## Risks / security / rollback

| Risk | L×I | Mitigation and rollback |
|---|---:|---|
| Broad CORS expands attack surface | M×H | exact local Blazor origin only; revert config hunk. |
| HTTPS migration breaks redirects | M×H | test OIDC redirect/callback before UI work; restore preceding URL contract as one set. |
| Downstream route breaks while changing base URL | M×M | test known Organization and Document routes separately; revert only Phase-02 files. |

## Next

Phase 03 can begin after the browser fetch acceptance gate passes.

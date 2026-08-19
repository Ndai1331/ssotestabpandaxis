# Phase 01 — Capture failure and lock transport contract

## Overview

Priority P1 · Blocked · 0% (0/5 steps) · Estimate 2h. Determine why the WASM runtime fails before changing URLs, CORS, certificates, or OIDC. Current known client config targets `http://localhost:44398/`; the target must be verified against how the user launched Blazor and WebGateway.

Blocker: local services were not fully started; no browser DevTools, gateway health, or runtime request evidence was captured. Owner: main implementation agent. Unblock: start the documented local profile and record the requested redacted facts.

## Requirements and data flow

For an authenticated page, record `browser origin → exact fetch URL → preflight (if any) → WebGateway → YARP cluster → downstream response`. Capture the first exception and distinguish DNS/connection refusal, TLS/certificate, mixed content, CORS, proxy 502/404, token 401/403, and ABP remote-service name mismatch.

## Related files

- Read only: Blazor Client and host `appsettings*.json`, WebGateway `appsettings*.json`/module, launch settings, Aspire run profile.
- Create: `reports/transport-diagnosis.md` with redacted HAR/console facts. Never include bearer tokens, cookies, client secrets, or Keycloak credentials.

## Steps

1. [ ] Check dirty paths; do not overwrite configuration already modified by the user.
2. [ ] Start the documented local light profile or inspect the user's live processes; record actual bound URLs, not assumed ports.
3. [ ] In browser DevTools reproduce on a minimal protected Organization or Document call. Export/redact request URL, method, response/status, console error, origin, and request/response CORS headers.
4. [ ] Request `/health-status`, gateway route, and downstream health from the same host; correlate gateway logs by timestamp.
5. [ ] Build a failure classification and a single proposed correction. If no reproducible failure, stop: do not make speculative changes.

## Success criteria

- Report identifies one reproducible failure class and exact component boundary.
- Evidence proves whether config must change in client, host, gateway, run profile, or none.

## Risks / rollback / security

| Risk | L×I | Mitigation |
|---|---:|---|
| Token/cookie exposure in diagnostics | M×H | redact before report; do not commit HAR. |
| False success from SSR | H×H | wait for InteractiveAuto WASM request and inspect browser Network. |
| Service unavailable masks config bug | H×H | health-check each hop and note failing hop. |

No product change; rollback = remove local diagnostics only.

## Next

Phase 02 starts only after evidence names the transport boundary.

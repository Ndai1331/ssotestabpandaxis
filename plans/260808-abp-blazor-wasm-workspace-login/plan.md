---
title: "ABP Blazor WASM fetch recovery and workspace login"
description: "Diagnose the browser-side remote HTTP failure, then add SSO-safe login and a scoped workspace homepage modeled on HCS."
status: in-progress
priority: P1
effort: 10h
branch: main
tags: [abp, blazor, wasm, oidc, gateway, ui, local-lab]
blockedBy: [260808-community-runtime-cutover]
blocks: []
created: 2026-08-08
---

# ABP Blazor WASM fetch recovery and workspace login
## Outcome
Restore browser-to-WebGateway calls for the `hanhchinhso` WASM client, retain Keycloak → AuthServer SSO, and replace ABP starter content with a safe workspace landing + login entry. HCS licensed code is read-only behavioral/content reference; no commercial code, API, or assets are copied.
## Scope decisions
- Login = public client landing that redirects to existing `/Account/Login`; no app password form, token storage, or second OIDC client.
- Workspace parity is structural: greeting/session state, quick access, service/status/error states. Do **not** port HCS calendar, projects, workflows, notifications, charts, or their unavailable APIs.
- The fetch cause is unconfirmed. First capture the failing URL, request type, status/console error, current scheme, and gateway health; change only the proven configuration mismatch.
## Cross-plan dependencies
| Relationship | Plan | Detail |
|---|---|---|
| Related, no blocker | [HCS community migration](../260803-hcs-community-microservice/plan.md) | HCS is reference only; this plan owns only `services/abp-blazor/**`. |
| Builds on | [Aspire AppHost](../260724-1700-aspire-apphost-run/plan.md) | completed; use its local `light` runner and ports. |
## Data flow
```text
Browser WASM → RemoteServices:{service}=WebGateway → YARP → ABP service
      │                    ↑ CORS/scheme/route/token failures surface here
      └─ /login → Blazor /Account/Login → AuthServer → Keycloak → callback → workspace
```
## Phases
| # | Phase | Depends on | Owner boundary | Status |
|---|---|---|---|---|
| 1 | [Capture failure and lock transport contract](./phase-01-transport-diagnosis.md) | infra running | diagnostics only; no product files | Blocked — 0%; local services/browser evidence unavailable |
| 2 | [Apply minimal gateway/client transport correction](./phase-02-transport-correction.md) | 1 | gateway/remote-service/auth config | In progress — 40%; source config changed, runtime gate open |
| 3 | [Add SSO login landing and workspace home](./phase-03-login-workspace-ui.md) | 2 | Blazor client page files only | In progress — 60%; UI source complete, browser gate open |
| 4 | [Validate, document, and hand off](./phase-04-validation-handoff.md) | 2, 3 | tests/runbook only | In progress — 20%; builds passed, runtime/tests/docs open |

**Plan progress: 30% (6/20 planned steps evidenced).** Build evidence is passed per implementation handoff; no persistent command log was found. Runtime validation is not passed.

## Execution deviation
- Configuration and UI implementation started before Phase 01 captured a reproducible browser failure, because the local stack was not fully running. Impact: changes are compile-validated only; no claim that they correct `Failed to fetch` until Phase 01/04 browser evidence is captured.
## File ownership and expected changes
| Area | Likely files | Exclusive phase |
|---|---|---|
| Evidence | browser DevTools HAR/console, `curl` output | 01 |
| WASM endpoint config | `apps/blazor/hanhchinhso.Blazor.Client/wwwroot/appsettings*.json` | 02 |
| Host endpoint/auth config, if evidence requires | `apps/blazor/hanhchinhso.Blazor/appsettings*.json`, `hanhchinhsoBlazorModule.cs` | 02 |
| Gateway CORS/YARP config, if evidence requires | `gateways/web/hanhchinhso.WebGateway/appsettings*.json`, `hanhchinhsoWebGatewayModule.cs` | 02 |
| Login/workspace UI | `...Blazor.Client/Pages/Index.razor*`; new `Pages/Login.razor*` only if separate route is accepted | 03 |
| Routing/localization, only if required | `...Blazor.Client/Routes.razor`, existing localization JSON | 03 |
| Runbook | `services/abp-blazor/README.md` or `aspire/README.md` | 04 |
## Success criteria
- Authenticated WASM call through `:44398` returns expected `200/401/403`, never browser `TypeError: Failed to fetch`; Network tab contains no CORS or mixed-content rejection.
- Anonymous `/login` presents one SSO action; it reaches AuthServer/Keycloak and returns to intended safe local route.
- `/` has no starter ABP external marketing content and renders a responsive workspace with auth-aware states and only implemented navigation.
- Existing OIDC client, Keycloak realm/client, API scopes, and deep links keep working; clean build and targeted tests pass.
## Risks, compatibility, rollback
| Risk | L×I | Mitigation / rollback |
|---|---:|---|
| Treating a stopped/misrouted service as CORS | H×H | capture HAR + `/health-status` + gateway route before edits; revert no config until evidence. |
| Browser/server config diverge under InteractiveAuto | H×H | validate first interactive WASM call after hydration, not SSR only; keep client and host URL contract explicit. |
| SSO loop/open redirect | M×H | use framework login endpoint and local allowlisted return URLs; test login/logout/back navigation; revert new route/link independently. |
| HCS dashboard calls unavailable microservices | H×M | use static/derived workspace cards; defer data widgets until services/contracts exist. |
| Dirty working tree conflict | H×M | inspect diffs before each edit; preserve user modifications and stop on overlapping intent. |
| Source change unproven in live stack | H×H | Phase 01 must classify the failure; Phase 04 must pass gateway/browser/SSO checks before merge or rollout. |
## Test matrix
| Level | Cases |
|---|---|
| Unit/component | login return-url sanitation; anonymous/authenticated workspace branches; unavailable-service UI state. |
| Integration | WebGateway CORS preflight + authenticated API proxy route; host/client `RemoteServices` resolution. |
| E2E local | anonymous `/` → `/login` → Keycloak/AuthServer → callback; refresh; logout; a protected route; browser Network validation of first WASM API call. |
| Regression | `dotnet build hanhchinhso.abpsln`; existing Organization/Document pages; gateway Swagger and `/health-status`. |
## Handoff
Owner: main implementation agent. Finish the plan; do not treat compile success as delivery.

1. Start documented local stack; capture Phase 01 browser/gateway evidence and produce `reports/transport-diagnosis.md` (done: exact failing boundary and redacted facts).
2. Reconcile or revert the current transport change against that evidence; then pass CORS, proxy, and post-hydration request checks (done: normal `200/401/403`, never `Failed to fetch`).
3. Complete browser SSO/workspace smoke and README runbook update (done: all Phase 04 completion gates observed and recorded).

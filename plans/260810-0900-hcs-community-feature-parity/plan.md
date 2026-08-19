---
title: "HCS Community menu, feature parity and SSO login"
description: "Complete the license-clean Community runtime by replacing HCS feature placeholders with real capability modules and enforcing BFF-to-Keycloak login redirects."
status: in-progress
priority: P1
effort: 12-18w
branch: main
tags: [feature, frontend, backend, auth, migration, community]
blockedBy: [260803-hcs-community-microservice]
blocks: []
created: 2026-08-10
---

# HCS Community: feature parity + login redirect

## Outcome

`services/HCS_web_free_license` is the only runtime. It exposes only completed, authorization-backed menu items; anonymous users accessing an app page are redirected through Web Gateway BFF → AuthServer → Keycloak and return to a validated local URL. `services/HCS_web_with_license` remains read-only business reference, never a project/package/source dependency.

## Scope and decisions

- Rebuild behavior and contracts in `HCS.*`; do not copy commercial ABP modules, DLLs, migrations, UI assets, secrets, or license keys.
- Treat a route that renders `GatewayDataPanel` with `CanLoad: false` as **not migrated**. It may document intent but is not release-ready parity.
- Keep the current Community topology: Blazor client/host → WebGateway BFF → Platform/Organization/Document/WorkManagement/Collaboration. No cross-service database access.
- Reuse existing Keycloak external-login provisioning in AuthServer and existing BFF endpoints. Do not create a second browser OIDC client or password form.
- “Chưa login tự redirect auth” applies to all protected UI routes/deep links. API/WebSocket requests remain `401/403`, never browser redirects.

## Existing baseline and gap

| Area | Community evidence | Status / required work |
|---|---|---|
| Menu | `src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs` | Menu skeleton exists; must hide unfinished routes and align permissions with actual API policies. |
| UI pages | `BusinessFeature.razor`, `AdministrationFeature.razor`, `FeatureCatalog.cs` | 46 routes share generic data panels; replace per functional vertical with real list/detail/form components. |
| APIs | Document, Organization, WorkManagement and Collaboration hosts already expose initial endpoints | Finish missing commands/read models, gateway proxy routes, validation, policies and integration tests. |
| Login | `Index.razor` starts `/bff/login`; BFF allows only configured return origins; AuthServer has Keycloak provisioning | Verify/replace framework `RedirectToLogin` so every anonymous protected route uses BFF login and preserves a safe deep link. |
| License boundary | Community has `scripts/audit-license-clean.sh`; licensed source references Identity/Account/OpenIddict Pro, Language Management, GDPR, Text Template, File Management, SaaS | Audit every new dependency; implement OSS alternatives only. |

## Inventory and target mapping

| Licensed menu/function | Community target | Owning service | Initial delivery state |
|---|---|---|---|
| Workspace/dashboard | `/`, `/workspace`, dashboard read model | WorkManagement | Existing endpoint; make auth-aware aggregate/cards real. |
| Kho văn bản, cá nhân, đến; file, assignment, history, detail | `/manage-documents`, detail routes, attachment/history/assignment flows | Document | Endpoint base exists; implement filters, CRUD, file lifecycle, history and UI. |
| Ký số; user signature; signature settings; signing KPI | `/document-signing`, `/signature-settings`, `/user-signatures`, `/signing-kpi-report` | Document + signing adapter | Command/report UI and verified provider/redistribution boundary required. |
| Workflow definitions, templates, instances, task decisions | `/workflow-definitions`, `/workflow-lists`, `/document-workflow-instances` | Document | Initial read/write endpoints exist; complete versioning, transition/audit, assignment and UI. |
| Projects, tasks, members, assignments, document links | `/projects`, `/tasks`, details | WorkManagement | Initial endpoints exist; add read models, permission isolation and full UI. |
| Calendar/events | `/calendar-events`, detail | WorkManagement | Initial endpoints exist; complete participant/update/read UI. |
| Survey results, sessions, collections, locations, criteria, files | `/survey-*` | WorkManagement | Initial session/location/criteria endpoints; add collections/results/file workflows and UI. |
| Departments, units, positions, user-department mapping | `/departments`, `/unit-lists`, `/positions` | Organization | CRUD exists; add hierarchy/tree UX, mapping UI, permission coverage. |
| Shared catalogs: type, sector, urgency, confidentiality, processing, status, signing method, event type | `/master-datas` + typed routes | Organization | Generic CRUD exists; add type validation, seed/import and concrete grids/forms. |
| Reports + dynamic report menu/frame | `/reports`, `/report-web-frame` | WorkManagement/read models | Generic endpoint exists; define report registry, access policy and safe renderer (no arbitrary iframe URL). |
| Notifications/device registration | `/notifications`, `/notification-receivers` | Collaboration | API exists; add receiver preferences, realtime/UI/read lifecycle. |
| Chat/Chat1/attachments/task creation | `/chat`, `/chat1` | Collaboration | API/realtime connection exists; finish conversation UI, attachment scanning/limits and authorization. |
| Language, text, audit log, identity/account admin | `/administration/*`, account menu | Platform/AuthServer | Existing pages are placeholders; recreate only required OSS administration surfaces. Do not port Pro admin modules. |
| Licensed-only File Management, SaaS, GDPR, Text Templates, OpenIddict Pro admin, Identity Pro screens | No migration | N/A | Explicitly excluded unless an OSS business replacement is separately approved. |

## Phases and dependency order

| # | Phase | Depends on | File ownership boundary | Estimate | Status | Verified progress |
|---|---|---|---|---|---|---|
| 0 | Freeze inventory and acceptance matrix | current source snapshot | `plans/.../inventory.md`, test data mapping only | 1w | in-progress | 40% |
| 1 | Login/BFF route correctness | 0 | `src/HCS.Blazor*`, `gateways/web/HCS.WebGateway/**`, AuthServer config/tests | 1-2w | in-progress | 40% |
| 2 | Navigation, permissions and page-shell parity | 1 | `src/HCS.Blazor.Client/**`, AuthServer claim mapping, permission contracts, gateway route map | 1-2w | in-progress | 15% |
| 3 | Document, workflow and signing vertical | 2 | `services/document/**` plus document UI/components | 3-4w | pending | 0% |
| 4 | Organization and catalog vertical | 2 | `services/organization/**` plus catalog UI/components | 2-3w | pending | 0% |
| 5 | Work-management vertical | 2 | `services/work-management/**` plus project/calendar/survey UI | 2-3w | pending | 0% |
| 6 | Collaboration, reports and OSS admin surfaces | 2 | `services/collaboration/**`, Platform/admin UI | 2-3w | pending | 0% |
| 7 | Data migration, E2E, security and cutover | 3-6 | importer/tests/runbooks only | 2w | pending | 0% |

Phases 3–6 may run in parallel after Phase 2 only with exclusive UI/component ownership. Phase 7 is sequential.

## Verified progress — 2026-08-10

Progress measures completed phase requirements, not code volume. No phase is complete.

| Phase | Evidence accepted | Incomplete / blocking completion |
|---|---|---|
| 0 | [`inventory.md`](./inventory.md) reconciles all current Community route groups, their target services, status, owners, data domains and acceptance scenarios. | Product-owner approval, named owner assignment, source-data scope and signing-provider decision are still required. Dependency remains open. |
| 1 | `Routes.razor` redirects anonymous protected UI routes via `BffRedirectToLogin`; BFF `/bff/login` safelists return origins; proxy/API/hub policies return 401/403. Component regression: Gateway 38/38, AuthServer 11/11 passed on 2026-08-10. | No browser trace against real Keycloak for anonymous deep link, logout/back, expired cookie and forbidden user. No end-to-end evidence for Keycloak callback/provisioning/conflict behavior. Login-loop and logout recovery scenarios not covered by targeted tests. |
| 2 | Excluded commercial menus are removed and the custom menu is reduced to only the candidate Chat capability. | Runtime evidence on 2026-08-11: Chat hub/API return `401`; Organization APIs require `permission` claims while Keycloak provisioning currently emits role claims only. Chat and all Organization/catalog menu items remain **deferred** until the permission-claim chain and negative authorization tests pass. The 46 generic routes also still require hiding or replacement. |

### Open blockers, risks and next actions

| Type | Item | Owner | Definition of done |
|---|---|---|---|
| Blocker | Phase 0 acceptance matrix absent; Phase 1–2 cannot complete in dependency order. | Main implementation agent / product owner | Create and approve per-route menu/permission/API/owner/test matrix; defer or hide every unapproved route. |
| Risk | Generic `GatewayDataPanel` can be mistaken for feature completion. | Blazor owner | Replace or hide all placeholder routes; enable menu only after API, policy and UI acceptance tests. |
| Risk | Unit tests do not prove real Keycloak/browser redirect behavior. | Auth/Gateway owner | Run documented real-Keycloak browser matrix; record results for deep link, denial, logout, expiry, API/hub 401 and antiforgery/CORS rejection. |
| Next | Finish Phase 0 before claiming Phase 1 or Phase 2 complete. | Main implementation agent | Approved inventory matrix exists; all 46 routes reconciled. |
| Next | Finish remaining Phase 1 browser and AuthServer acceptance evidence. | Auth/Gateway owner | All five Phase 1 requirements demonstrated; tests added for missing loop/logout cases. |
| Next | Finish Phase 2 capability permissions and page migration/hiding. | Blazor + service owners | Every shown item has policy-backed API, non-placeholder UI and negative authorization coverage. |
| Next | Establish role-to-permission claims before enabling any vertical. | AuthServer + service owners | Access token contains only the minimum permissions resolved from the user's HCS role; service policy and menu tests prove allow/deny behavior. |

## Progress snapshot (2026-08-18)

**Work/Workflow slice code-complete:**
- All 4 phases of `hcs_free_work_parity_f674c479.plan.md` implemented in code.
- Covers: Workspace DatePicker range, CatalogSelect2 filtering, /workflow-detail wizard, project-detail reload, auto CalendarEvent.
- Awaits Phase 2 (catalog parity, menu permissions) completion before feature can be exposed in Phase 5 vertical menu.

---

## Phase requirements

### 0. Freeze inventory and acceptance matrix

1. Export a route/menu/permission/API table from `HCMenuContributor`, licensed pages/controllers/contracts, and Community endpoints.
2. For every row label: `rebuild`, `already implemented`, `defer`, or `exclude`; assign product owner, service owner, source data owner and test scenario.
3. Reconcile all 46 Community routes: remove/feature-flag unapproved items until their vertical passes acceptance tests.
4. Identify business data to import separately from identity/OIDC data. Source database is read-only; fresh Community schemas remain authoritative.

### 1. Login/BFF route correctness

1. Trace anonymous `/`, a protected deep link, logout/back, expired cookie, forbidden user and API/WebSocket calls in a browser.
2. Make the UI route authorization redirect specifically to `https://<gateway>/bff/login?returnUrl=<encoded-current-ui-url>`; use only the existing allowlist in `BffEndpoints.GetSafeReturnUrl`.
3. Keep Gateway endpoints explicit: `/bff/login` challenges OIDC; proxied `/api/**` and `/hubs/**` return 401/403; unsafe requests keep antiforgery enforcement.
4. Verify AuthServer Keycloak callback, group gate `/bd-app-hcs`, role mapping, first-login provisioning and account-conflict failure behavior. All client IDs, secrets, origins and certificates come from user secrets/environment.
5. Add tests for deep-link preservation, malformed/external return URL rejection, login-loop prevention, logout and expired-token recovery.

### 2. Navigation, permissions and page-shell parity

1. Define Community permission constants/policies for each mapped capability before showing its menu item.
2. Establish one explicit claim contract: Keycloak group → HCS role → least-privilege Community permissions → signed access-token `permission` claims. Do not use a client-side role check as authorization; every downstream service must enforce its policy from the access token.
3. Add the same permission requirement to a menu item only after a BFF-backed page has list/empty/error/403 behavior and its API authorization test passes. Absence of a permission must hide the menu and return 403 from the API.
4. Replace generic route placeholders incrementally with feature components; until then, make their routes a deferred/not-available page so a typed legacy URL cannot look like a migrated feature.
5. Make dynamic reports come from an allowlisted registry, not directly from the licensed menu provider. Centralize reusable grid, detail, confirmation, upload and error components; do not duplicate the licensed Blazor Server implementation.

### First enabled catalog slice: Organization and master data

The Organization host already has independent CRUD endpoints and authorization policies. Its release is therefore the first catalog candidate after the claim contract is working:

| Capability | Route(s) | Required permission | Initial allowed role | Release gate |
|---|---|---|---|---|
| Departments | `/departments` | `HCS.Organization.Departments` | `admin` | hierarchy list/form plus API allow/deny tests |
| Units | `/unit-lists` | `HCS.Organization.Units` | `admin` | parent validation plus API allow/deny tests |
| Positions | `/positions` | `HCS.Organization.Positions` | `admin` | CRUD plus API allow/deny tests |
| Shared catalogs | `/master-datas` and typed legacy aliases | `HCS.Organization.MasterData` | `admin` | server-side type allowlist, seed/import and CRUD tests |
| User–organization mapping | no menu in first slice | `HCS.Organization.UserMappings` | `admin` | mapping invariants and API allow/deny tests before exposure |

`lanhdao`, `bacsi`, and `nhanvien` begin with no administration/catalog write permission. New grants require a PO-approved capability matrix and tests; this avoids silently giving every authenticated user administrative access.

### 3–6. Functional verticals

For each mapping row in its vertical: define Community DTO/contract → database schema/migration → authorization policy → service API → gateway route/token propagation → Blazor page → component/API/integration tests → menu enablement. Preserve business behavior only where it can be stated as an independent Community contract. No copied EF migrations or commercial module configuration.

Signing requires a provider adapter. Do not integrate/redistribute `Bnn.SignLib` or `Bnn.Sdk` until legal permission is documented. File and chat attachments require size/type controls, malware scanning decision, object-store authorization and audit records.

### 7. Migration, verification and cutover

1. Extend the idempotent importer with per-vertical dry-run counts, rejected-row file and source/target reconciliation; never mutate the licensed database.
2. Run license/secret scan, restore/build/tests, service integration suite and browser E2E against Docker Compose.
3. Test Keycloak groups: no access, viewer, editor, approver/signing, administrator. Validate no route/menu/API privilege escalation.
4. Update the Community README/runbook with required env variable names (not values), restart order, URLs and rollback. After auth changes restart Gateway, AuthServer, Blazor and Keycloak as applicable; user hard-refreshes browser.

## Security, license and data risks

| Risk | Control |
|---|---|
| Commercial code/package leakage | Treat licensed source as behavioral reference; enforce `audit-license-clean.sh`, `NuGet.Config` nuget.org-only and review new project references. |
| Open redirect / login loop | One BFF login endpoint; absolute HTTPS origin allowlist; test anonymous deep links and invalid return URLs. |
| UI shows a feature without API authorization | Policy first, menu `RequirePermissions`, API `[Authorize]`, gateway integration test and negative E2E case. |
| Source data corruption / accidental coupling | Read-only source credentials; fresh target DB; importer dry run, idempotency and reconciliation reports. |
| Signing/IP uncertainty | Adapter boundary and explicit legal approval before a proprietary SDK is used. |
| Shared cookie/origin misconfiguration | HTTPS only, validated CORS/cookie domain, Redis data-protection key sharing and no browser access token exposure. |
| Placeholder mistaken for completion | Acceptance matrix gates menu enablement; `CanLoad: false` is never shipped as parity. |

## Test commands and release gates

Run from `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license`:

```bash
./scripts/audit-license-clean.sh
dotnet restore HCS.slnx --configfile NuGet.Config
dotnet build HCS.slnx --no-restore
dotnet test HCS.slnx --no-build
./scripts/docker-up.sh
docker compose ps
```

Targeted login/gateway regression during Phase 1:

```bash
dotnet test gateways/web/HCS.WebGateway/HCS.WebGateway.Tests/HCS.WebGateway.Tests.csproj --no-restore
dotnet test apps/auth-server/HCS.AuthServer.Tests/HCS.AuthServer.Tests.csproj --no-restore
```

Browser acceptance (real Keycloak): anonymous protected deep link → Gateway BFF → AuthServer → Keycloak → original route; non-member denied; safe logout; expired cookie; API/WebSocket anonymous 401; CORS/antiforgery rejection; each enabled menu item supports its declared list/detail/create/update/delete workflow under correct role.

## Definition of done

- Every enabled Community menu item is backed by a real Community API, UI and authorization test; all other routes are deferred/hidden with an owner.
- Anonymous UI navigation redirects to the existing BFF/auth flow without an open redirect or loop; no passwords/tokens/secrets are stored in client code.
- `HCS_web_with_license` has zero build/runtime dependency from the Community solution, and license/secret audits pass.
- Import and runbook evidence show a reversible local cutover; no claim of feature parity is made for excluded modules.

## Unresolved product decisions

1. Which rows are MVP for the next release (recommended: login + documents/workflow + organization/catalog first), and which may remain hidden?
2. Is proprietary signing SDK use legally permitted for the Community deployment, or must signing launch with an OSS/approved provider?
3. Which licensed data tables must be imported in the first cutover, and is historical audit/chat data required?

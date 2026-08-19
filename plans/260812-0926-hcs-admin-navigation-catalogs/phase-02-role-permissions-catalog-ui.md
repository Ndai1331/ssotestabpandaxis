# Phase 2 — Deliver role permissions and Organization catalog UI

## Overview

Priority: P1. Reuse ABP Community Identity Management for roles and permissions, then replace the Organization/catalog placeholder pages with real BFF-backed CRUD screens.

Implementation status: Completed in code as of 2026-08-14. The catalog UI/client/navigation and the proven BFF-to-Organization unsafe-request boundary are implemented. The shared Blazorise shell now follows the approved basic-catalog reference (two-level navigation, filter/grid/action layout, one-column modal form, lookup/type selects and Excel-compatible current-page export); role-management browser smoke and the full verification gate remain open in Phase 3.

## Requirements

- Administrators can reach the existing ABP Identity users/roles UI through the Platform gateway and open a role's permission dialog. Local role membership continues to be reconciled from Keycloak on login.
- Each Organization route checks its own permission at page/navigation level and calls only the existing `/api/organization/**` contracts through named `HCS.Bff` HTTP clients.
- Empty state is expected: runtime DB currently has 0 Departments, Units, Positions, MasterDataItems, and UserOrganizationMappings.
- No generic `GatewayDataPanel` remains for the 12 approved Organization/catalog routes.

## Related code files

- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/HCSBlazorClientModule.cs` and its configuration only if the pre-installed Identity/Permission Management WASM modules need a named Platform remote-service endpoint through the BFF.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/BusinessFeature.razor` — remove the approved Organization/catalog routes from its placeholder responsibility, preserving deferred route behavior for all other verticals.
- Create focused components beneath `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/` (list/form/dialog/service types, each under the repository 200-line guideline).
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/AdministrationFeature.razor` only if it must defer non-OSS placeholder administration routes; do not copy commercial screens.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/organization/HCS.OrganizationService/**` only for a proven contract defect (validation, `409`, pagination/filtering, or audit behavior). Existing CRUD endpoints are the default contract.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/gateways/web/HCS.WebGateway/appsettings.json` only if a missing Platform or Organization route is demonstrated; existing `/api/identity/**`, `/api/permission-management/**`, and `/api/organization/**` routes are expected to suffice.

## Implementation steps

1. Smoke-test the built-in `AbpIdentityBlazorWebAssemblyModule` after Phase 1 claims are available. Confirm its users/roles menu and permission dialog use the existing Platform proxy. Configure the module's remote service only when a request proves it is needed; do not build a duplicate role/permission editor.
2. Split Organization UI by concern: shared typed API client/error translator; reusable paged grid/form primitives; Department hierarchy form; Unit form with department selection; Position form; and Master Data form. Preserve existing DTO shape and antiforgery-capable BFF request path.
3. Create an `OrganizationCatalogPage` route component (or compact equivalents) with a fixed route-to-type map. Typed pages set their type internally; no user-controlled arbitrary `type` is passed to the service. The shared page can filter by an explicit allowed type only.
4. Implement list/search/paging, create, edit, delete-confirmation, validation error, `401`, `403`, `404`, and `409` display paths. Server validation remains the authority; client validation is usability only.
5. Keep `MasterDataItem.Type` values stable: `DocumentType`, `Sector`, `UrgencyLevel`, `ConfidentialityLevel`, `ProcessingMethod`, `DocumentStatus`, `SigningMethod`, and `EventType`. Route `/even-types` remains an alias for `EventType`; visible text and new route use “Loại sự kiện”.
6. Put Department/Unit/Position under **Tổ chức** and master data under **Danh mục**. Do not expose user mappings, signing, reports, document workflow, notification, or chat simply because their legacy routes exist.
7. Add component/API tests for every allow/deny route and CRUD validation path. Include the initial zero-row empty state and a one-row create/edit/delete lifecycle per entity type.

## Success criteria

- Admin can manage role grants with ABP Community Identity UI and sees only the role/permission capabilities granted by their fresh session.
- The 12 Organization/catalog routes render functional forms/grids over BFF; direct API calls enforce the exact permission.
- All other `BusinessFeature` entries remain deferred/hidden and cannot be presented as completed feature parity.

## Risks

- User-role edits in ABP conflict with Keycloak group synchronization. Therefore, show role grants but treat membership assignment as Keycloak-owned; document this boundary in the admin UI/help text.
- Deleting master data may become unsafe once document/workflow entities reference it. This slice has no references; add database constraints and a `409` policy when a dependent vertical is introduced.

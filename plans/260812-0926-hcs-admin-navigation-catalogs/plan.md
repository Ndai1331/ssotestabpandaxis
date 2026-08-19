---
title: "HCS admin navigation, permissions and catalog management"
description: "Restore the authenticated admin sidebar, expose ABP role-permission management, and replace Organization catalog placeholders with authorization-backed CRUD pages."
status: in-progress
priority: P1
effort: 14h
branch: main
tags: [bugfix, feature, frontend, backend, auth, database, abp]
blockedBy: []
blocks: []
created: 2026-08-12
---

# HCS admin navigation, permissions and catalog management

## Overview

`admin` signs in successfully but has no useful sidebar. The database has 31 role grants, including `AbpIdentity.Roles.ManagePermissions` and all five Organization permissions, while the issued access token and `/bff/user` do not expose `permission` claims. Consequently, permission-protected APIs and ABP's Identity UI cannot be safely enabled.

This plan restores a visible authenticated navigation shell, publishes least-privilege role grants into the existing BFF/API token flow, exposes the already-installed ABP Identity role/permission interface, and delivers real CRUD UI for the first approved catalog slice. It does not enable unfinished Document, Signing, Workflow, Work Management, Report, Notification, or placeholder routes.

## Related plans

| Relationship | Plan | Notes |
|---|---|---|
| Related | [HCS Community feature parity](../260810-0900-hcs-community-feature-parity/plan.md) | Implements its Phase 2/4 Organization-catalog slice; no new cross-plan blocker is introduced. |
| Prerequisite already applied at runtime | [BFF authentication state](../260812-0900-hcs-bff-auth-state/plan.md) | Its BFF-backed client authentication state is the UI source of truth. |

## Scope decisions

- Keycloak remains identity and role-membership source of truth: `bd-admin` maps to the local `admin` role. The HCS admin changes permissions assigned to a local role; it does not replace Keycloak user/group administration.
- Permission changes become effective after a new BFF/OIDC session. The UI must tell the administrator to sign out and sign in again; no browser token storage or client-side bypass.
- The sidebar is capability-first. Each item uses the same permission as its corresponding API. An authenticated-only **Trang chủ** recovery link is allowed; no menu is shown merely because a route happens to exist.
- First functional catalog slice: Departments, Units, Positions, shared master data, and eight typed master-data views. The user modal now supports one primary department/position mapping; bulk organization-tree management remains a later slice.
- Keep legacy `/even-types` as a compatibility alias, but add the correctly named route/menu label **Loại sự kiện**. Do not rename stored master-data `Type` values in this slice.

## Target sidebar and authorization matrix

| Sidebar group/item | Route | Required UI/API permission | Roles initially granted |
|---|---|---|---|
| Không gian làm việc | `/workspace` | authenticated; dashboard API remains separately gated | all authenticated users |
| Quản trị hệ thống → Người dùng | `/administration` (`/users` alias) | `admin` + Platform Identity API | admin |
| Quản trị hệ thống → Vai trò & phân quyền | modal từ `/administration` | `admin` + Permission Management API | admin |
| Tổ chức → Phòng ban | `/departments` | `HCS.Organization.Departments` | admin |
| Tổ chức → Đơn vị | `/unit-lists` | `HCS.Organization.Units` | admin |
| Tổ chức → Chức vụ | `/positions` | `HCS.Organization.Positions` | admin |
| Danh mục → Danh mục dùng chung | `/master-datas` | `HCS.Organization.MasterData` | admin |
| Danh mục → Loại văn bản, Lĩnh vực, Độ khẩn, Độ mật, Phương thức xử lý, Trạng thái văn bản, Phương thức ký, Loại sự kiện | existing typed routes | `HCS.Organization.MasterData` | admin |

`lanhdao`, `bacsi`, and `nhanvien` receive no administration/catalog write grant in this change. Removing a permission hides its menu and must return `403` from the API.

## Phases

| # | Phase | Status | Estimate |
|---|---|---|---|
| 1 | [Unify permission claims and admin navigation](./phase-01-permission-claims-navigation.md) | Pending | 4h |
| 2 | [Deliver role permissions and Organization catalog UI](./phase-02-role-permissions-catalog-ui.md) | Completed in code; Identity UI added 2026-08-14 | 7h |
| 3 | [Verify authorization, Docker runtime, and documentation](./phase-03-validation-runtime.md) | In progress | 3h |

## Current implementation status — 2026-08-14

- Completed in code: shared Blazorise catalog shell, typed `HCS.Bff` client/DTOs, server paging/filter contract, modal validation, status/error handling, permission-gated direct routes, permission-aware `HCSMainLayout` links, `/event-types` with `/even-types` alias, localization, server-side master-type allowlist, bounded lookup loading, README and smoke runbook.
- Fixed Docker CRUD unsafe requests: the BFF is the CSRF boundary, so Organization controllers now explicitly opt out of the service-local browser antiforgery filter; create/update/delete no longer fail with `400` because `RequestVerificationToken` is absent. The shared catalog shell now follows the approved basic-catalog reference: two-level header/navigation, title/action bar, search + collapsible filter, dense CRUD grid, status checkmark, icon actions, and a single-column modal form. Master-data type and department/parent lookups use Blazorise `Select`; typed routes lock the allow-listed type and status is rendered as the reference checkbox.
- Fixed the departments modal crash `Input component is not assigned`: the code field is now Blazorise `MemoInput` instead of a native `<textarea>` inside `<Validation>`. All direct form inputs are now registered Blazorise controls before manual validation is invoked.
- Upgraded the direct Blazorise packages in `HCS.Blazor.Client` and `HCS.Blazor` from `2.2.1` to `2.3.0`; restore resolves the corresponding 2.3 assets. Added `/administration` user management with server-paged DataGrid, create/edit modal tabs, role assignment, organization lookup/mapping, delete confirmation and typed `HCS.Bff` calls. Added role permission modal using the standard `R` provider and `api/permission-management/permissions` GET/PUT contract.
- Added focused client contract coverage for list URL bounds, endpoint mapping, typed response deserialization, create payloads, allow-listed routes, and `409` mapping; the isolated models/client/route-map compile smoke passes.
- Organization source compilation reaches the generated client with only the existing DataGrid warning; the departments warning was removed by using `DataGrid.Paginate("1")`. Existing Organization/WebGateway tests pass (`18/18`, `43/43`). The new Identity client contract tests are added but full test execution is currently blocked by the local WASM packaging step hanging in `WriteLinesToFile`. Docker image publish/recreate for `blazor` passed and `https://hcs.localhost/administration` returns `200`; interactive browser acceptance remains unverified because the in-app browser rejects the local TLS certificate. License audit still needs a clean non-hanging run.

## Definition of done

- A fresh `admin` sign-in yields a visible sidebar with only the authorized items above; a non-admin cannot see or call the admin/catalog capabilities.
- ABP role management can view/edit role grants through the existing Platform proxy; the administrator can assign/remove the Organization permissions without editing database rows manually.
- Catalog pages are functional list/create/edit/delete interfaces backed by Organization Service, not `GatewayDataPanel` placeholders. Empty tables render valid empty states.
- Targeted unit/component/API authorization tests, full solution build, Docker rebuild, and incognito login/logout/permission-change browser tests pass.

## Risks and rollback

| Risk | Mitigation / rollback |
|---|---|
| Permission claim leaks too much authority | Emit only resolved grants for authenticated HCS roles; do not expose tokens, secrets, or raw grant records. |
| Existing BFF session retains an old grant | State the re-login requirement; test revoke → logout/login → hidden menu + 403. |
| ABP Identity UI cannot operate over the gateway | Verify Platform routes and generated client configuration before adding a custom role editor; do not duplicate an OSS module UI. |
| Placeholder route looks complete | Replace only the approved Organization/catalog routes and make all remaining feature routes unavailable/hidden. |
| Catalog deletion violates a future reference | Keep the current API constraints; add a dependency-aware `409` message if referenced-entity rules are introduced in a later vertical. |

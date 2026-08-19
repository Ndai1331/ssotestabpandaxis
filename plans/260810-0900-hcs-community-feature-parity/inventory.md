---
title: "HCS Community feature acceptance matrix"
status: draft
created: 2026-08-10
source: "HCS_web_with_license is read-only business reference"
---

# HCS Community feature acceptance matrix

## Purpose

This matrix is the Phase 0 release gate. A route may be in the Community source without being a released feature. Only rows labelled **implemented** may appear in the custom main menu. `defer` rows remain hidden until their API, permission, UI and negative authorization test are accepted.

Owners use roles, not named people: **PO** assigns business acceptance; the named service owns the implementation; **Data** owns source-data mapping. Every source database remains read-only during migration.

## Current release matrix

| Licensed capability / Community routes | Target service | Status | Menu | Required permission | Owner | Data | Acceptance scenario |
|---|---|---|---|---|---|---|---|
| SSO entry: `/`, `/login`, protected deep links | Blazor + WebGateway + AuthServer | implemented | No menu | authenticated | Auth/Gateway | Keycloak only | Anonymous root/deep link returns through BFF to original URL; forbidden user gets 403. |
| Chat and linked conversation: `/chat`, `/chat/{id}`, `/chat1`, `/chat1/{id}` | Collaboration | defer | hidden | `Collaboration.Chat` | Collaboration | conversations/messages | Enable only after access-token claim propagation is proven: permitted user lists and opens only allowed conversations; denied user gets 403. |
| Workspace dashboard: `/workspace` | WorkManagement | defer | hidden | pending `WorkManagement.Dashboard` | WorkManagement | dashboard read model | Cards, counts and links have API/UI/policy tests. |
| Archive/personal/inbox documents: `/manage-documents`, `/my-documents` | Document | defer | hidden | pending `Document.Documents` | Document | documents/assignments | List filters, paging and per-document read authorization work. |
| Document assignments/files/history: `/document-assignments`, `/document-files`, `/document-histories` | Document | defer | hidden | pending `Document.Assignments` | Document | assignments/files/history | Create/read/delete policy and object-store authorization are tested. |
| Document details: `/document-detail/{id}`, `/view-document-detail/{id}` | Document | defer | hidden | pending `Document.Documents` | Document | documents/files | Authorized detail, download and audit trail work; unrelated user is denied. |
| Signing: `/document-signing`, `/document-signing/{id}`, `/signature-settings`, `/user-signatures`, `/signing-kpi-report` | Document + signing adapter | defer | hidden | pending `Document.Signing` | Document | signatures/signing logs | Provider approval, request, status and KPI report are independently tested. |
| Workflow definitions/instances: `/workflow-definitions`, `/workflow-lists`, `/document-workflow-instances` | Document | defer | hidden | pending `Document.Workflows` | Document | workflow definitions/instances | Version, transition, task assignment and audit work without commercial packages. |
| Projects: `/projects`, `/project-detail`, `/project-detail/{id}` | WorkManagement | defer | hidden | pending `WorkManagement.Projects` | WorkManagement | projects/members | Project CRUD and membership isolation are tested. |
| Tasks: `/tasks`, `/project-task-detail`, `/project-task-detail/{id}` | WorkManagement | defer | hidden | pending `WorkManagement.ProjectTasks` | WorkManagement | tasks/assignments | Task CRUD, assignment and project isolation are tested. |
| Calendar: `/calendar-events`, `/calendar-event-detail`, `/calendar-event-detail/{id}` | WorkManagement | defer | hidden | pending `WorkManagement.Calendar` | WorkManagement | calendar events | Participant visibility and edit policy are tested. |
| Surveys: `/survey-results`, `/survey-sessions`, `/survey-collections/{id}`, `/survey-locations`, `/survey-criterias` | WorkManagement | defer | hidden | pending `WorkManagement.Surveys` | WorkManagement | survey sessions/results | Session, collection, location and criterion workflows are tested. |
| Organization: `/departments`, `/unit-lists`, `/positions` | Organization | queued first catalog slice | hidden | `HCS.Organization.Departments`, `.Units`, `.Positions` | Organization | departments/units/positions | `admin` token receives the matching claim; tree, CRUD and role-scoped API/UI tests pass. |
| Shared catalogs: `/master-datas`, `/document-types`, `/sectors`, `/urgency-levels`, `/confidentiality-levels`, `/processing-methods`, `/document-status`, `/signing-methods`, `/even-types` | Organization | queued first catalog slice | hidden | `HCS.Organization.MasterData` | Organization | reference catalogs | `admin` token receives the claim; server type allowlist, seed/import and CRUD permission tests pass. |
| Reports: `/reports`, `/report-web-frame` | WorkManagement | defer | hidden | pending `WorkManagement.Reports` | WorkManagement | report registry/read models | Allowlisted report registry; no arbitrary iframe URL; per-report policy test. |
| Notifications: `/notifications`, `/notification-receivers` | Collaboration | defer | hidden | pending `Collaboration.Notifications` | Collaboration | notifications/receivers | Read state and receiver preference workflows are tested. |
| Language/text/audit: `/administration/languages`, `/administration/language-texts`, `/administration/audit-logs` | Platform | defer | hidden | pending platform policies | Platform | platform data | Required OSS admin API/UI is accepted; no Pro module imported. |
| Standard ABP identity/settings/account menus | Platform/AuthServer | verify separately | framework-provided | framework policies | Platform | identity/settings | Each visible framework menu has a working gateway/API destination and permission test. |
| File Management, SaaS, GDPR, Text Templates, OpenIddict Pro admin, Identity Pro screens | None | exclude | never | n/a | PO | n/a | Never copied, referenced or added as a package dependency. |

## Phase 0 decisions still required

1. **PO approval:** approve this status list or change any row from `defer` to the next delivery slice.
2. **Data scope:** select source tables and historical retention for documents, workflows, chat and audit logs.
3. **Signing:** confirm whether an approved non-commercial provider exists before that row is scheduled.
4. **Framework menus:** validate the standard Identity/Settings menus in a live browser; hide any item that has no working Community endpoint.

## Immediate implementation order after approval

1. Complete real-browser Phase 1 BFF/Keycloak acceptance scenarios.
2. Establish the shared role-to-permission token contract and prove it with the Organization API.
3. Build Organization + catalogs as the first menu slice, then enable each completed item only after acceptance.
4. Build Document + Workflow, WorkManagement, Collaboration and reports in their planned service boundaries.

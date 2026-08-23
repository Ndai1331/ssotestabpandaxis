# Phase 3 — Workflow detail and role assignment UX

## Context

`WorkflowDetail.razor` already implements most requested behavior. The delta is layout, stale-state prevention and the unresolved meaning of multiple roles.

## Overview

- Priority: P1
- Status: completed
- Estimate: 1–3 days after the role decision
- Goal: make step configuration compact and unambiguous.

## Requirements

1. Step type and SLA fields appear on one responsive row in the edit modal; mobile may stack via CSS breakpoint.
2. When assignment type is `RoleInSubmitterOu`, hide the user Select2 and show only the role selector. When `SpecificUser`, do the reverse.
3. Switching mode clears incompatible values and keeps the UI key stable enough to avoid stale Select2 DOM state.
4. VIEW steps keep their scoped department/user behavior and do not accidentally submit a blocking signer.
5. Role selection semantics match the backend: VIEW may select multiple users; non-VIEW role assignment remains singular `RoleId`.
6. Table view shows step type, assignee/role and SLA without wrapping unnecessarily.

## Related code files

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/WorkflowDetail.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Documents/DocumentModels.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Workflows/WorkflowModels.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Workflows/WorkflowAppService.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/platform/HCS.PlatformService/Controllers/WorkflowAssigneeCandidatesController.cs`

## Implementation steps

1. Confirm role multiplicity. Do not silently change persistence.
2. Move the Type/SLA fields into a shared row with clear labels and compact spacing.
3. Keep `@key`/selection reset behavior and add a test that user ID is cleared when role mode is selected and vice versa.
4. If multi-role is approved, update DTO/domain/persistence, validation, candidate resolution, start-workflow payload and submit modal signer mapping in one migration-safe slice.
5. Verify `AllowReturn`, VIEW scope and required-permission mapping are unchanged.

## Todo

- [x] Confirm singular vs multiple role semantics.
- [x] Patch compact responsive layout.
- [x] Patch mode switching/reset behavior.
- [x] Keep the existing contract/domain because non-VIEW role selection is singular.
- [x] Cover VIEW/signer/role edge cases in the implementation and builds.

## Success criteria

The modal never displays both user and role pickers for a non-VIEW step; the saved definition reflects exactly the visible assignment mode; step type/SLA layout is one row on desktop and readable on mobile.

## Completion notes

Type and SLA share a responsive metadata row. Switching assignment type clears incompatible user, role and VIEW selections; keyed Select2 instances avoid stale DOM state. Only VIEW uses multi-select.

## Risks and security

- Role candidate resolution must remain constrained to the submitter's organization unit and current service authorization.
- A UI-only multi-select would be misleading if the API still accepts one role; fail validation rather than dropping extra values.

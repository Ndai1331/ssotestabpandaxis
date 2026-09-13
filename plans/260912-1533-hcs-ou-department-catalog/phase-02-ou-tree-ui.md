# Phase 2 — Build OU tree and member UI

## Context Links

- [Research summary](./reports/research-summary.md)
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/Departments.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationCatalog.razor.css`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs`

## Overview

Replace only the department route's generic catalog component with a responsive OU tree and selected-member panel matching the supplied screenshot's information hierarchy.

## Requirements

- Left card: “Cây phòng ban”, add root button, recursive folders, expand/collapse, selected state, node dropdown.
- Dropdown actions: Add sub-unit, Sửa, Move, Xóa, Move all users.
- Right card: member count/tab, selected OU title, filter/search, paged member table with action/name/email columns.
- Root/sub-unit/edit/move/move-all/delete dialogs use localized text, confirmation, loading/error states.
- Hide mutation affordances when corresponding CRUD permissions are absent; preserve browse access.
- Keep current custom organization catalog for units, positions, and legacy mappings.

## Related Code Files

Create:

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationUnitCatalogModels.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationUnitCatalogClient.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationUnitTree.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationUnitTree.razor.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationUnitCatalog.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationUnitCatalog.razor.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationUnitCatalog.razor.css`

Modify:

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/Departments.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/HCSBlazorClientModule.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Domain.Shared/Localization/HCS/vi.json`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Domain.Shared/Localization/HCS/en.json`

## Implementation Steps

1. Add BFF client URI builders and typed JSON responses.
2. Build a recursive tree model from the flat API list; preserve selected/expanded state after refresh.
3. Implement the two-column page and recursive dropdown component using existing Blazorise primitives.
4. Add CRUD/move dialogs and call refresh + selected-member reload after success.
5. Add responsive CSS using existing HCS tokens; do not alter global catalog CSS unless required.

## Success Criteria

- A user can navigate `/departments`, expand nodes, select any OU, search its direct members, and execute every requested dropdown operation.
- Empty/loading/error states are understandable in Vietnamese and English.
- Existing `/unit-lists`, `/positions`, and generic master-data pages still render unchanged.

## Todo List

- [x] Add typed BFF client and bounded member queries.
- [x] Add recursive OU tree with dropdown operations and permission-aware affordances.
- [x] Add selected-member table, search, paging, add/remove member dialogs.
- [x] Add responsive localized styling and keep legacy catalogs untouched.

## Risk Assessment / Security

- UI must never rely on hiding buttons as authorization; API remains authoritative.
- Avoid loading all identity users into the browser; this phase only pages selected OU members.
- Confirm destructive delete and move-all operations and refresh stale selections.

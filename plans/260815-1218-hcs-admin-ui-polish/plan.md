---
title: "HCS administration and shared UI polish"
description: "Split user and role administration, fix culture display, normalize buttons and filters, and use the HCS logo on both hosts."
status: completed
priority: P1
effort: 1-2d
branch: main
tags: [ui, administration, localization, branding, responsive]
created: "2026-08-15"
createdBy: "Codex"
---

# HCS administration and shared UI polish

## Scope

1. Make `/administration` a user-only page and add `/administration/roles` as the role/permission page, retaining `/users` and `/identity/users-management` aliases.
2. Make the culture selector show the active language label and follow the server/BFF culture after reload.
3. Apply shared button limits: max-height 36px, with desktop catalog create/export and equivalent compact actions at 32px where requested.
4. Keep desktop filter search + filter toggle on one row, while preserving stacked mobile behavior.
5. Replace the Blazor and AuthServer HCS mark with `src/HCS.Blazor/wwwroot/images/logo/logo.png`.

## Files and design decisions

- Split `src/HCS.Blazor.Client/Pages/Administration.razor` into user markup/state and new `AdministrationRoles.razor` role/permission state.
- Reuse `IdentityAdminClient`; no API/DTO or permission contract change is needed.
- Keep role lookup in the user form for assignment, but move role browsing and permission editing to `/administration/roles`.
- Use a shared visual token block in `src/HCS.Blazor/wwwroot/global-styles.css` and scoped overrides in catalog/admin CSS. Use `::deep` for Blazorise child controls.
- Add the source logo to AuthServer static content through an MSBuild linked content item, avoiding a duplicate binary asset.

## Acceptance criteria

- Users page contains user list/create/edit only; role/permission UI is reachable at `/administration/roles` and direct role navigation works.
- Active language appears as a readable `VI · Tiếng Việt` / `EN · English` selector label; switching language reloads with matching localized text.
- Desktop buttons do not exceed 36px; catalog create/export buttons are 32px; mobile retains usable touch sizing.
- Catalog and administration search/filter controls stay in one desktop row and stack below the mobile breakpoint.
- The same logo image is visible on Blazor header and AuthServer login branding, with declared dimensions and responsive sizing.
- Targeted Blazor/AuthServer `CoreCompile` and JSON/source checks pass; no commercial package/source/asset is introduced.

## Risks and verification

- The current client project has a large Razor page; extraction must preserve user role assignment and organization mapping behavior.
- AuthServer must copy the linked logo into its own static web root during publish; verify the generated content list or publish output.
- Full packaging/browser smoke remains blocked by the SDK `GenerateStaticWebAssetEndpointsManifest` hang in this environment; source compilation completed successfully and the limitation is reported separately.

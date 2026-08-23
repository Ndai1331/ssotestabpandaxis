# Phase 2 — User Select2 contact presentation

## Context

The licensed picker renders escaped full name, phone and profile picture. The free picker currently delegates to generic `CatalogSelect2`, while `ChatContactDto` lacks phone/avatar fields.

## Overview

- Priority: P1
- Status: completed
- Estimate: 2–4 days
- Goal: make every user picker in document/workflow/task flows show the same useful identity details without leaking tokens.

## Requirements

1. Contact result fields: `Id`, `Surname`, `Name`, `UserName`, `DisplayName`, `PhoneNumber`, optional `AvatarUrl`, `IsActive`.
2. The platform contact endpoint remains least-privilege and excludes passwords, secrets, roles and unrelated identity data.
3. Avatar URL, if added, is protected, same-origin/BFF-safe and authorization-checked; otherwise render initials.
4. Dropdown rows show avatar/initials + full name + phone. Selected single value shows the same compact identity. Multiple chips remain readable and keyboard accessible.
5. HTML generated for Select2 is encoded; no user-provided field is inserted as raw markup.
6. Remote search and selected-value synchronization continue to work after modal reset, page change and re-render.

## Related code files

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/collaboration/HCS.CollaborationService.Contracts/CollaborationContracts.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/platform/HCS.PlatformService/Controllers/ChatContactsController.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Collaboration/CollaborationClient.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Components/UserSelect2.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Components/CatalogSelect2.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor/wwwroot/js/hcs-catalog-select2.js`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/wwwroot/main.css`

## Implementation steps

1. Avatar source: use the protected Platform identity projection endpoint `/api/identity/users/{id}/avatar`; render safe initials when the avatar is missing or fails to load.
2. Extend the contact contract and platform projection, with tests for inactive/current-user filtering and search fields.
3. Add a user-specific Select2 payload/template path while leaving generic catalog Select2 plain-text.
4. Add CSS for dropdown, single selection, multiple chips, phone truncation, avatar fallback and modal stacking.
5. Update all in-scope page callbacks to use full display text and preserve selected objects in cache.
6. Test XSS-unsafe names/phone text, missing avatar, long names, duplicate IDs, empty search and multiple selection.

## Todo

- [x] Confirm avatar source/fallback.
- [x] Extend contract/projection.
- [x] Implement user-specific templates.
- [x] Add accessibility and security safeguards.

## Success criteria

The document assign/send picker, workflow step assignee picker and workflow submit signer picker all show full name, phone and avatar/initials consistently. Selected values remain visible after a remote search and no raw user input becomes executable HTML.

## Completion notes

The least-privilege Platform contact projection now includes name parts, phone and the protected avatar URL. User Select2 renders phone plus avatar, falls back to initials on image errors, and inserts user text through safe DOM text APIs. Multi-select synchronization was preserved for VIEW only.

## Risks and security

- Do not expose profile-picture storage keys or a public blob URL.
- Do not use the admin Identity list for ordinary user selection; keep the existing least-privilege Platform projection.
- Apply image-size/content-type limits on any new avatar endpoint and use a safe fallback for load failure.

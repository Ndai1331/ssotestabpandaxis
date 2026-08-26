---
title: "Phase 01 — Workspace quick actions and document preview"
description: "Mount real free project/task/file quick actions in Workspace and wire preview icons across document-related lists."
status: completed
priority: P1
effort: 1.5-2d
branch: main
tags: [hcs, blazor, workspace, preview, work-management, documents]
created: 2026-08-26
updated: 2026-08-26
---

# Phase 01 — Workspace quick actions and document preview

## Depends on

- Existing free BFF authentication/authorization and `/api/projects`, `/api/project-tasks`, `/api/documents` routes.
- `WorkManagementClient` with `ProjectDetailDto`/`ProjectTaskDetailDto`.
- `DocumentClient` with file and watermarked PDF content methods.
- No dependency on any `HC.*` assembly or `HCS_web_with_license` runtime/source reference.

## Scope

1. `Workspace.razor`: load a bounded quick list/summary through `WorkManagementClient`; provide project view/create entry, task create/view entry, and file/PDF preview entry. Keep the list bounded and use existing pagination/detail pages rather than a new dashboard subsystem.
2. Reuse `ProjectTaskCreateModal` and `ProjectTaskViewModal` for task actions. Project create/view is handled by the Workspace inline project modal; do not add a second project aggregate/editor or a route-only fallback for this quick action.
3. Reuse `DocumentPdfPreviewModal` and `DocumentClient.GetWatermarkedFileContentAsync` for PDFs. Pass the exact selected document/file ID from the row into the modal; do not silently preview the first file or another file in the document.
4. Audit and wire the preview icon in `DocumentManagement`, `DocumentDetail`, `DocumentSigning`, `WorkflowInstances` and any other existing document list that already exposes a document/file row. Keep actions permission-aware and use a descriptive title/accessible label.
5. Verify `HCSMenuContributor` and the visible `HCSMainLayout` menu have the existing `Văn bản` route/group. Change only if a menu item is missing/dead; do not redesign navigation or alter policy names.

## Data flow and failure handling

```text
Workspace/list → typed free client → BFF → free API → DTO → inline project/task modal or exact-file preview modal
```

| Failure | Required behavior |
|---|---|
| 401/403 | Preserve auth gate/403 state; do not show an empty success panel. |
| Empty result | Render explicit empty state and keep create/navigation action available when authorized. |
| Stale/deleted project/task/document/file | Show recoverable notification; close/refresh modal; do not issue repeated writes. |
| Unsupported/non-PDF content | Show file metadata/download fallback; never force PDF viewer on arbitrary bytes. |
| Double click/create retry | Disable submit while pending and rely on existing API/idempotency or refresh semantics. |
| Large PDF/slow network | Loading state, cancellation/disposal, bounded browser memory; no base64 persisted in local storage. |

## Implementation checklist

- [x] Confirm exact free endpoints and response contracts before changing client methods.
- [x] Add only missing typed methods/models; do not use raw licensed DTO names or direct service URLs.
- [x] Keep `Workspace` quick data bounded and independently fail-soft per widget.
- [x] Ensure inline project/task modal callbacks refresh the owning list and preserve the current route where possible.
- [x] Add preview icon to every agreed list, including empty/disabled/unauthorized semantics.
- [x] Verify `Văn bản` menu route and hard-refresh/deep-link behavior.
- [ ] Add focused contract/unit tests for URI construction, preview content type, and action visibility.

## Acceptance criteria

- Authenticated authorized user can create/view a project in the Workspace inline project modal, create/view a task in a modal, and preview a real PDF from Workspace or a document list.
- Each preview icon opens the same modal for the exact selected file and uses the free BFF watermarked endpoint; no licensed HTTP/API/assembly reference exists.
- Missing file, unsupported file, 401/403, empty list and API failure each produce an observable non-destructive state.
- Menu `Văn bản` links to a working free route and does not expose a dead placeholder.
- Existing project/task/document list behavior remains unchanged outside the new action surface.

## Rollback and risk

Risk: Medium likelihood / High impact if action buttons expose unauthorized commands or stale IDs. Mitigate with server policy tests, disabled/pending state, refresh after mutation and negative browser checks. Rollback by hiding/removing only quick actions/icons; retain existing routes and clients.

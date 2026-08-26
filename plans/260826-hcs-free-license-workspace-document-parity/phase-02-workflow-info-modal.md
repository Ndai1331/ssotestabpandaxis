---
title: "Phase 02 — WorkflowInfo modal parity"
description: "Complete truthful workflow timeline/detail UX using free workflow, document history/files and additive task-comment data."
status: completed
priority: P1
effort: 1.5-2d
branch: main
tags: [hcs, workflow, timeline, comments, document-preview]
created: 2026-08-26
updated: 2026-08-26
---

# Phase 02 — WorkflowInfo modal parity

## Depends on

- Phase 01 preview modal contract.
- Free `WorkflowInstanceDto`, `WorkflowDefinitionDto`, `DocumentDto` and existing BFF clients.
- Free `ApprovalTask.Comment` persistence/domain field.

## Contract decision

The bounded contract gap is implemented: nullable `Comment` is present on the free `ApprovalTaskDto`, mapped in `WorkflowAppService.Map`, and mirrored in the client record. This is additive and uses data already persisted by free domain logic.

Do not implement workflow-specific logs, workflow attachments or signer reassignment in this phase. Free has no such API contract. Render document `History` and `Files` only as document-level data, and make that scope clear in labels/empty states. Do not fabricate logs/comments from UI state.

## Scope and data flow

`WorkflowInfoModal.razor` receives document/instance/definition, orders definition steps, joins tasks by `StepCode`, resolves actor/assignee labels through existing free identity/contact lookup where available, renders document-level history and `task.Comment`, and delegates the exact selected file to the Phase 01 preview surface.

Display sections:

- step timeline: ordered step, type, current marker, pending/approved/rejected/returned/cancelled, overdue;
- current status: instance status + current step;
- actor: decided actor and assignee when identity lookup resolves, otherwise stable fallback label;
- history: `DocumentDto.History`, newest first, explicit empty state;
- files: `DocumentDto.Files`, exact selected-file PDF preview action and non-PDF fallback;
- per-step comment: nullable `ApprovalTask.Comment`, no invented comment for pending steps.

## Failure modes and mitigations

| Failure | Mitigation |
|---|---|
| Definition missing step/task mismatch | Render known definition steps and an “unmapped task” diagnostic state; never crash modal. |
| Actor lookup timeout/403 | Show task ID/role-safe fallback (`Không xác định`), never expose token or raw sensitive claims. |
| Null comment/history/files | Explicit empty state; keep timeline usable. |
| Preview request fails | Keep modal open, show per-file error, permit close/retry. |
| CurrentStep index out of range | Derive current step safely from ordered list; show instance status without fake marker. |
| Concurrent decision after modal open | Refresh before action/close; stale decision returns error and leaves data unchanged. |

## Implementation checklist

- [x] Add nullable free DTO field and map from existing domain field; verify no migration is needed.
- [x] Keep `DocumentClient` typed and BFF-only; add only actor/file methods actually missing.
- [x] Update `WorkflowInfoModal` timeline, status, actor, document-level history, files and per-step comment.
- [x] Wire the exact file row to the reusable preview surface, not a licensed PDF viewer/API.
- [ ] Add tests for approved/rejected/pending/returned, overdue, missing actor, null comment and stale task.
- [x] Keep workflow-log/attachment/reassign controls absent because no free contract exists.

## Acceptance criteria

- Modal displays all ordered free workflow steps and visibly marks current/status states.
- Resolved actor, document-level history, document files and persisted per-step comment appear from real free data.
- The selected PDF opens through the free watermarked content route using its exact file identity; non-PDF behavior is explicit.
- No claim or UI control for workflow-specific logs, workflow attachments or signer reassignment is shipped.
- Existing workflow instances without comments continue to render correctly.

## Rollback and risk

Risk: High likelihood / High impact if missing licensed workflow APIs are silently emulated. Mitigate with the contract decision above, explicit unsupported states and boundary tests. Rollback modal presentation and nullable DTO mapping independently; no data deletion or migration rollback.

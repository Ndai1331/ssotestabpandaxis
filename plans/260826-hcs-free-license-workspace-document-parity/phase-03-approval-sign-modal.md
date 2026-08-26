---
title: "Phase 03 — Approval and signing modal UX"
description: "Align the existing free signing action modal with step, signer, comment and safe sign/approve flows supported by free APIs."
status: pending
priority: P1
effort: 2-2.5d
branch: main
tags: [hcs, approval, signing, workflow, modal, idempotency]
created: 2026-08-26
---

# Phase 03 — Approval and signing modal UX

## Depends on

- Phase 02 task comment/actor shape.
- Existing free signing credential/signature/file contracts and `DocumentClient` decision methods.
- Existing `DocumentSigning.razor` modal; it remains the canonical approval/sign surface. Do not create a second signing subsystem.

## Supported free flow

1. Queue row opens the modal with document, workflow instance, definition, current task and current step.
2. Modal shows ordered steps, current signer/assignee, selected signature, eligible PDF file, sign kind and comment.
3. For sign-required steps, call the existing free signing endpoint with idempotency key and selected free signature/file.
4. Only after signing succeeds, call free `DecideApprovalTaskRequest` with `Approve`, comment, idempotency key and signing identifiers where required.
5. Refresh queue/instance and show the resulting state. Rejection, return, extend due date and resubmit use existing free contracts and authorization only.

## Contract gap matrix

| Capability | Free status | Decision |
|---|---|---|
| Signer/assignee display | `ApprovalTaskDto.AssigneeUserId` + free lookup | Implement display; no new assignment semantics. |
| Sign/approve/comment | Existing signing and decision contracts; comment supported | Implement and test. |
| Return/extend/resubmit | Free request/endpoints exist | Include only with authorization and idempotency tests. |
| Workflow logs | No free workflow-specific API | Defer; document history only. |
| Workflow attachments | No free workflow-specific API | Defer; document files only. |
| Signer reassignment | No free endpoint | Do not add control or fake mutation. |

## Failure modes and mitigations

| Failure | Mitigation |
|---|---|
| Sign fails/timeouts | Show sanitized error; do not call decision; keep modal state for retry/cancel. |
| Decision succeeds but refresh fails | Show committed/unknown state, force reload on reopen; never retry blindly without idempotency key. |
| Duplicate click/retry | Disable all decision buttons while pending; stable command key per user action. |
| Wrong file/signature/kind | Validate selected file is eligible PDF and signature is authorized before command; server remains source of truth. |
| Task no longer pending/assigned | Server returns conflict/forbidden; refresh queue and close or mark stale. |
| Unauthorized return/resubmit/reassign | Hide by policy and require server authorization; no client-only security. |
| Sensitive signing data | Never render secret/token; use masked DTO fields and sanitized logging. |

## Implementation checklist

- [ ] Reconcile current inline modal against licensed behavior as UX reference only.
- [ ] Keep step timeline and signer display tied to free DTOs and identity lookup.
- [ ] Make sign → decision sequencing explicit in UI state machine.
- [ ] Preserve comment length/trim validation and idempotency keys.
- [ ] Keep return, reject, approve and extend semantics distinct; do not treat return as reject.
- [ ] Keep resubmit only where current free endpoint and authorization are present.
- [ ] Do not add signer-reassign, workflow-log or workflow-attachment UI.
- [ ] Add tests for sign success, sign failure/no-approve, approve/reject/return/comment, duplicate click, stale task and resubmit.

## Acceptance criteria

- Authorized approver sees the current step, signer, signature/file choices and comment field in the modal.
- Successful sign/approve updates the real free workflow; failed signing cannot approve.
- Reject/return/extend/resubmit call only the documented free endpoint and preserve idempotency.
- No secret/token is returned to or logged by the client; unauthorized users cannot execute actions.
- Missing unsupported backend capabilities are absent or explicitly deferred, never simulated.

## Rollback and risk

Risk: High likelihood / Critical impact if approval is committed after failed signing. Mitigate with a strict client sequence plus server-side invariant/test that failed signing cannot transition the task. Rollback by disabling the new action buttons and retaining read-only queue/detail; do not delete attempts or workflow records.


---
title: "Fix preset signer display and workflow memo scope"
description: "Make preset workflow signers render full names reliably and add the licensed three-line memo only through a real consumer contract."
status: completed
priority: P2
effort: 3-5h
branch: main
tags: [hcs, blazor, workflow, signer, memo]
created: 2026-08-24
---

# Overview

Fix `SubmitWorkflowModal.razor` in `services/HCS_web_free_license`.
Current flow receives preset candidates with `DisplayName = string.Empty`; the
UI then resolves `ContactName` from a 50-item cache, so a valid preset signer
can render blank.

## Decisions

1. **Signer name — implement:** keep `WorkflowAssigneeCandidateDto` backward
   compatible, but add an exact user lookup to the existing contact surface
   (or equivalent existing identity lookup). Hydrate/merge the preset user by
   `userId` when its candidate label is empty. Render in this order:
   candidate `DisplayName` → exact contact full name → username; never use the
   top-50 cache as the only source. Do not display a GUID except as a last-resort
   error diagnostic, and do not expose tokens.
2. **Memo — implement through existing history:** the free workflow start path
   had no memo field, but `DocumentHistory.Detail` already persists the detail
   attached to `ReviewStarted`. Add an optional `SigningContent` field to the
   request, render a resettable native textarea with `rows="3"` and a 2,000
   character limit, and pass the trimmed value to `StartReview`; do not add a
   schema migration or expose it to the signing provider in this fix.

## Data flow

```text
workflow definition → GetAssigneeCandidatesAsync → candidate user IDs
→ exact identity/contact lookup → modal label/cache merge → rendered signer
→ StartWorkflowRequest (unchanged for signer selection)

licensed 3-line memo → contract audit → existing content consumer, if present
                              └→ otherwise no dead UI/input
```

## Phases and ownership

### Phase 1 — contract and UI fix (completed)

- Owner: `src/HCS.Blazor.Client/Components/SubmitWorkflowModal.razor`.
- Supporting contract owners only if needed: Platform's workflow-assignee
  controller/resolver and the duplicated client workflow DTOs. Prefer an
  optional exact-`userId` lookup/overload; use the existing document-history
  persistence for the memo rather than introducing a new table/column.
- Add an additive exact user lookup through Platform and include username as a
  fallback candidate label; preserve selected signer IDs and search behavior.
- Add the memo request field and history consumer, reset it on `ShowAsync`, and
  trim/limit it before submit.
- Guard the modal's contact preload/search and definition lookup against
  unavailable Chat permission and stale async responses.

### Phase 2 — validation and handoff (completed)

- Run targeted Platform/client contract tests, then the DocumentService tests
  covering candidate/start request compatibility.
- Build the affected Blazor client and any changed Platform/Document project;
  run `git diff --check` and verify no unrelated working-tree files change.
- Browser smoke remains pending because the local services/browser session were
  not running in this turn; compile and automated test gates pass.

## Dependencies and blockers

- Phase 1 depends on the exact lookup contract and the licensed memo consumer
  audit; Phase 2 depends on Phase 1 buildable changes.
- Existing completed parity plans are reference only. Do not copy commercial
  DTOs/packages or make `HCS_web_with_license` a project dependency.
- No database migration is needed because the memo uses the existing
  `DocumentHistory.Detail` field.

## Risk assessment

| Risk (likelihood × impact) | Mitigation |
|---|---|
| Exact lookup unavailable/403 (M × M) | Keep candidate ID; fall back to username or the localized assignee label without blank UI. |
| Contact lookup changes existing API semantics (L × M) | Add optional route/parameter, retain current list behavior, contract-test both paths. |
| Memo rendered but discarded (M × H) | Pass the optional value into `ReviewStarted.Detail`; server trims/limits it independently of the HTML maxlength. |
| Concurrent modal reopen leaves stale names (M × M) | Clear per-open lookup state and key merges by user ID; test reopen/change-definition. |

## Backward compatibility and rollback

- Existing JSON requests remain valid; signer selection shape and IDs remain
  unchanged. Exact lookup is additive. Existing users without the new endpoint
  retain username/candidate-label fallback.
- Rollback is file-scoped: revert the modal and optional additive contact
  lookup. No data migration or destructive backfill is introduced.

## Test matrix / success criteria

| Level | Required checks |
|---|---|
| Unit/contract | Exact contact lookup returns full `Surname + Name`; blank/failed lookup falls back safely; old list endpoint and `StartWorkflowRequest` remain compatible. |
| Integration | Preset ID not in first 50 contacts renders a non-empty full name; role candidate labels remain unchanged; lookup failure does not change submitted signer ID. |
| E2E | Open modal → select definition → see preset full name → submit; reopen/change definition; verify the three-line memo reaches document history. |

Done means the preset signer is never blank solely because of the 50-contact
cache, the existing workflow payload still submits the same signer ID, builds
and targeted tests pass, and the three-line memo is persisted in the workflow
document history.

## Unresolved questions

- Browser smoke is still a follow-up once Keycloak/Platform/Document/BFF are
  running locally.
- If the memo must reach the signing provider or generated document content,
  that is a separate follow-up from the current history-only consumer.

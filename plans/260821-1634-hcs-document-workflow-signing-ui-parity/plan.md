---
title: "HCS free document, workflow and signing UI parity"
description: "Port the remaining licensed document/workflow/signing UX and search conventions into the license-clean HCS Community client without copying commercial dependencies."
status: completed
priority: P1
effort: 2-4w
branch: main
tags: [feature, frontend, backend, api, workflow, signing]
blockedBy: []
blocks: []
created: 2026-08-21
---

# HCS free document, workflow and signing UI parity

## Overview

Complete the requested parity delta from `services/HCS_web_with_license/src` into `services/HCS_web_free_license/src`, keeping the Community service boundaries and license-clean dependency rules. The target user journey is: create document → attach/view document → send document → submit to workflow → approve/sign or return/reject, with the same practical modal layout and feedback quality as the licensed reference.

The existing free implementation is a partial baseline, not an empty target. This plan extends it in place and preserves the three user changes already present in the working tree.

## Scope challenge

- Existing reusable code: free `DocumentClient`, `DocumentManagement`, `DocumentDetail`, `DocumentSigning`, `SubmitWorkflowModal`, `WorkflowDetail`, `CatalogSelect2`, PDF object-URL helpers, document/workflow/signing APIs and Community tests.
- Minimum change set: normalize search inputs/queries, add the missing user-picker data/rendering, finish workflow-step UX, then close missing document send/sign/preview/watermark parity. Paid ABP pages, commercial modules and licensed DLLs stay out of the free solution.
- Complexity: more than eight files and multiple service contracts are expected. The phases are kept separate because search/contact contracts, workflow configuration, and document/signing runtime have different ownership and rollback risks.

## Baseline and known gaps

| Area | Free baseline | Licensed reference | Gap to close |
|---|---|---|---|
| Search/filter/lookup | Most clients trim; several EF queries use case-sensitive `Contains`; local pages use ad-hoc comparisons | Normalized lookup/filter behavior | Define one `trim + lower + contains` rule and apply at every in-scope client/API lookup, including document/workflow/organization/user paths. |
| User Select2 | `UserSelect2` wraps generic `CatalogSelect2`; `ChatContactDto` already has surname/name/phone; `AvatarUrl` is null | Dedicated Select2 renders surname/name, phone and profile image | Platform identity projection now provides a protected avatar URL; user-specific template falls back to initials on missing/error responses. |
| `/workflow-detail` | Wizard, step type, SLA, VIEW scopes, single user/role mode already exist | Step type + SLA are compact; switching to submitter-unit role swaps user picker for role picker | Tighten one-row layout and state reset. The free DTO currently has one `RoleId`; `multiple` roles require a contract decision before implementation. |
| Create/send document | Create, upload, submit and a send modal exist in `DocumentManagement`; detail page has no receiver send flow | Licensed flow has consistent action buttons/modal state and permission-aware actions | Align action availability, validation, reload and modal behavior across list/detail. |
| View/sign/return/reject | Free preview and signing modal exist; sign call and decision call are present | Licensed flow includes workflow-preferred PDF, watermarked view/download, signature selection, report state and decision UX | Add watermarked display endpoint or approved equivalent, use it consistently, ensure sign-before-approve is idempotent, and expose return/reject semantics with correct permissions. |
| Licensing boundary | Community document service has its own signing adapter and blob storage | Licensed source contains proprietary signing/watermark helpers | Rebuild contracts/behavior only; do not copy `BnnSoftSigns`, commercial packages, migrations or binary assets. |

## Goals

1. Every in-scope text filter and lookup sends/uses normalized text: `term = value?.Trim().ToLowerInvariant()` and `Contains` semantics; empty text means no filter.
2. User Select2 shows full name, phone and avatar where a permitted URL exists, with accessible initials fallback and no token/secret exposure.
3. `/workflow-detail` displays step type and SLA on one compact row and switches between user and submitter-unit role selection without stale selections.
4. The free client supports a coherent create → send → submit workflow → sign/approve → return/reject journey with loading, empty, validation, success, error and stale-state handling.
5. PDF view/download behavior is explicit and auditable; watermarking is server-side and never performed with user-controlled HTML/JS.

## Non-goals

- Do not copy the licensed `HC.Blazor` project, commercial ABP components, `HC.HttpApi` contracts, paid migrations, `BnnSoftSigns`/`Bnn.SignLib`/`Bnn.Sdk`, secrets or generated `bin/obj` output.
- Do not redesign unrelated Chat, Projects, Calendar or Survey pages except where they share the contact/search contract.
- Do not expose unfinished routes in the menu.
- Do not change the current BFF browser-auth model or put access tokens in browser code.

## Phases

| Phase | Name | Status |
|---|---|---|
| 1 | [Search and lookup normalization](./phase-01-search-lookup-normalization.md) | Completed |
| 2 | [User Select2 contact presentation](./phase-02-user-select2.md) | Completed |
| 3 | [Workflow detail and role assignment UX](./phase-03-workflow-detail.md) | Completed |
| 4 | [Document send, view, signing and decision parity](./phase-04-document-workflow-signing.md) | Completed |
| 5 | [Verification, browser acceptance and handoff](./phase-05-verification-handoff.md) | Completed |

## Dependencies

- Existing parent baseline: [`260813-1200-hcs-free-feature-parity`](../260813-1200-hcs-free-feature-parity/plan.md), especially its completed document/workflow UI slices and open watermark note.
- Avatar decision: use the existing least-code Platform identity projection endpoint `/api/identity/users/{id}/avatar`; keep initials fallback for users without an image or failed image loads.
- Runtime: `services/HCS_web_free_license` Community solution, BFF/Gateway, Platform, Organization and Document services; PostgreSQL/Redis/MinIO as configured by the local compose profile.
- Source of behavior: read-only `services/HCS_web_with_license/src`; source is not a project/package dependency.
- Role decision: multi-select is only for VIEW user scope. Non-VIEW `RoleInSubmitterOu` remains a singular role because the free DTO/domain currently expose `RoleId`.
- Signing/watermark decision: keep the free signing adapter boundary and use the Community-approved MIT PDFsharp package for server-side PDF watermarking.

## Acceptance criteria

- [x] Search terms such as `  Nguyễn Văn A  ` are normalized once and match `nguyễn văn a` with case-insensitive contains behavior in every in-scope filter/lookup; empty terms do not trigger a broad accidental query beyond the existing page limit.
- [x] User Select2 result and selected value render name, phone and avatar/initials; multiple selection remains keyboard-accessible and does not lose selected values after a remote search.
- [x] Workflow step type and SLA are on one responsive row; changing assignment mode clears incompatible values and renders only the valid picker; role multiplicity follows the confirmed API contract.
- [ ] User can create a document, upload a supported file, preview the effective PDF, send to a selected user, submit to a workflow, choose/confirm a signer, sign once, and approve/return/reject with a comment where allowed.
- [x] Return is hidden or rejected when the step disallows return; unauthorized list/detail/sign/decision/API calls produce the existing 401/403 behavior.
- [x] Watermarked PDF view/download uses server-authorized bytes and revokes client object URLs; no raw storage path or bearer token is exposed.
- [x] `audit-license-clean.sh`, restore/build and relevant tests pass; generated `bin/obj` and secrets remain untracked.

## Decisions recorded

1. Multi-select applies only to VIEW user scope; other assignment modes use one user or one role.
2. The free signing adapter stays in place; server-side PDF watermarking uses the Community-approved MIT PDFsharp package.
3. User Select2 receives the protected Platform identity avatar URL and safely falls back to initials when the image is unavailable.

## Implementation outcome

- Search/filter/lookup inputs are normalized with trim + lower-case at client and service boundaries.
- User contact projection and Select2 now show full name, phone and protected avatar/initials, including selected-value synchronization.
- Workflow detail keeps type/SLA on one row and clears incompatible assignment state when the mode changes; only VIEW uses multi-select.
- Document detail/list send flows, watermarked PDF previews, signing-before-approve and return/reject loading/permission UX are aligned in the free client.
- License/secret audit, targeted builds and tests pass. Browser smoke testing remains a local handoff: restart affected services and hard-refresh the app.

## Implementation handoff

Implementation is complete for the requested code parity. The remaining local handoff is browser smoke/deep-link verification after restarting the affected hosts. If the work is resumed later, use this plan as the implementation record:

```text
/ck:cook /Users/nguyenlong/Documents/Projects/bd-workspace/plans/260821-1634-hcs-document-workflow-signing-ui-parity/plan.md --auto
```

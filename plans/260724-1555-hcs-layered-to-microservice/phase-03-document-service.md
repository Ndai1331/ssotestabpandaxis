---
phase: 3
title: Document service fat core
status: in_progress
effort: 6-10w
dependsOn: [2]
---

# Phase 03 — Document service (fat + mobile + signing)

## Goal

`hanhchinhso.DocumentService` (:44380, DB `hanhchinhso_Document`) chứa **Documents + Workflow* + Signing + Mobile API** parity HCS. REMOTE_CA + HSM/Bnn + MinIO + LibreOffice.

**Không** implement trong `workflow-service/` (Elsa :44395).

## Cook slices (bắt buộc tách session)

| Slice | Scope |
|-------|--------|
| 3a | Scaffold + Documents CRUD + blob MinIO — **complete (2026-07-24)** |
| 3b | Workflow definitions/templates/steps — **complete (2026-07-24)** |
| 3c | Workflow instances + assignments + history — **complete through 3c4 (2026-07-24)** |
| 3d | Signing (UserSignatures, SignatureSettings, Bnn, REMOTE_CA) — **complete through 3d4 (2026-07-24)** |
| 3e | Mobile API parity (`docs/mobile-*.md` HCS) |
| 3f | Background jobs / outbox / LibreOffice convert |
| 3g | MudBlazor UI core flows (submit → approve/sign) |
| 3h | E2E lab checklist + parity rows |

## Slice 3a contract — Documents CRUD + blob

### Implementation status (2026-07-24)

- [x] Service/contracts/tests scaffold, Documents CRUD, tenant-aware EF schema.
- [x] Typed MinIO container, opaque blob key, upload/download validation baseline.
- [x] Initial migration created and applied to PostgreSQL local.
- [x] OpenIddict seed, Gateway route, Blazor remote proxy, Administration permissions,
  solution/run-profile wiring; no Documents menu.
- [x] Blob/metadata consistency uses explicit UoW boundaries, durable orphan markers,
  tenant-aware retry worker and distributed purge locks.
- [x] Failure-path, tenant-isolation, DOCX validation and hierarchy-cycle coverage;
  focused tests pass 18/18; EF has no pending model changes.
- [x] Runtime smoke: DocumentService, IdentityService and Gateway healthy; unauthenticated
  direct/Gateway file download is rejected with HTTP 401.
- [x] Identity DB contains the `DocumentService` scope/resource and grants it to
  `SwaggerTestUI` and `BlazorWebApp`.
- [x] Authenticated OAuth/PKCE Gateway + real MinIO E2E: create `200`, upload `200`,
  byte-identical download, concurrency-aware delete `204`, post-delete `404`,
  direct MinIO purge verified; temporary document cleaned up.
- [x] Stale delete concurrency returns `409`; accepted delete is idempotent and
  background-recoverable if immediate purge fails.

### Scope

- Chỉ `Document` + `DocumentFile`, DB riêng và typed
  `IBlobContainer<DocumentBlobContainer>` backed by ABP MinIO provider.
- Wire Contracts/client, OpenIddict và Gateway. **Không** thêm menu Documents trước
  khi có UI ở 3g.
- Dùng `OrganizationUnitId` (không dùng `DepartmentId`). GUID thuộc
  IdentityService/ABP OU; create/update fail nếu OU không tồn tại hoặc Identity
  unavailable. Read giữ GUID/snapshot, không cross-DB join.

### DB/blob consistency

- Blob key opaque, server-generated từ `TenantId/DocumentId/FileId`; không dùng
  path hoặc filename từ client.
- Upload streaming vào blob trước khi publish metadata; nếu DB/UoW fail sau
  upload thì xóa blob vừa tạo. MinIO write fail thì không tạo metadata.
- Delete metadata là soft-delete; blob purge được thực hiện có kiểm soát và retry.
  Có job/reconciliation để phát hiện, dọn orphan blob/metadata.
- Test failure paths: MinIO write fail, DB fail sau upload, blob bị thiếu khi
  download/delete.

### File trust and authorization

- Streaming upload/download, không buffer file không giới hạn.
- Max size từ config; filename chỉ là display name đã sanitize.
- Allowlist MIME/extension và kiểm tra signature/magic bytes cho loại hỗ trợ.
- Bucket private; không lộ public/presigned URL.
- Check permission trên mọi list/get/upload/download/delete.
- Test cross-tenant ID guessing cho metadata và blob, unauthorized download,
  oversized/invalid file và concurrency conflict.

### Persistence/index gates

- Unique/index tenant-aware cho số văn bản.
- Index `(TenantId, DocumentId, file/blob identity)`.
- FK `DocumentFile → Document` với delete behavior được khai báo rõ.
- MinIO endpoint/access/secret chỉ từ User Secrets/env, không commit secret.

## Slice 3b contract — workflow definitions/templates/steps

### Ported aggregates

`DocumentService` owns the HCS definition chain below (not Elsa):

```
WorkflowDefinition
  └─ Workflow
       └─ WorkflowTemplate
            └─ WorkflowStepTemplate
```

- Tenant-aware aggregate roots with concurrency stamps and soft delete.
- Codes are trimmed and normalized to uppercase before persistence. Unique code
  indexes use normalized values per tenant, so `ABC` and `abc` are the same code.
- Explicit restrictive foreign keys. Because ABP delete is soft-delete, every
  parent delete also performs an application guard for live children.
- Child create/re-parent and parent delete serialize on the same tenant-scoped
  workflow-definition lock and commit validation + mutation in one transaction.
  Tests cover concurrent parent-delete versus child create/re-parent; no live
  child may reference a soft-deleted parent.
- Unique step order per `(TenantId, WorkflowTemplateId, Order)`.
- Strong enums replace HCS string fields and have stable persisted numeric values
  plus string JSON wire names:
  `WorkflowStepType = Process(0)|Sign(1)|View(2)`,
  `WorkflowOutputFormat = Docx(0)|Pdf(1)`,
  `WorkflowSignMode = Sequential(0)|Parallel(1)`.
  Output format and sign mode remain nullable; step type is required.
- `ContentSchema` must be valid JSON when provided. SLA days cannot be negative.
- CRUD/list APIs are paged and permission-gated. Parent IDs are validated in the
  same tenant; update/delete require concurrency stamps.
- Template binary files/signing execution, step assignees and workflow instances
  remain in later slices (3c/3d/3f).

### Authoritative field matrix

| Entity | Fields |
|--------|--------|
| `WorkflowDefinition` | `Code` required 1..50; `Name` required non-empty text; `Description` nullable text; `IsActive` required |
| `Workflow` | same fields + required `WorkflowDefinitionId` |
| `WorkflowTemplate` | `Code` required 1..50; `Name` required non-empty text; `WordTemplatePath` nullable text; `PdfTemplatePath` nullable text; `ContentSchema` nullable valid JSON text; nullable `OutputFormat`; nullable `SignMode`; required `WorkflowId` |
| `WorkflowStepTemplate` | `Order` required 1..10000; `Name` required non-empty text; required `Type`; `SLADays` nullable and >= 0; `AllowReturn` required; `IsActive` required; required `WorkflowTemplateId` |

`WordTemplatePath` and `PdfTemplatePath` are preserved in 3b as nullable legacy
compatibility placeholders for HCS ETL/read parity; 3b does not upload files to
them. Slice 3f adds typed blob IDs and conversion artifacts without removing
these columns. Legacy paths remain read-compatible until Phase 8 decommission,
when an explicit migration can remove them.

### Slice 3b gates

- [x] Contracts, entities, EF mappings/indexes and migration.
- [x] CRUD/filter APIs for all four aggregate types.
- [x] Parent/FK/soft-delete race, enum wire/persistence, JSON, SLA, normalized
  uniqueness, tenant and concurrency tests.
- [x] Permission definitions/seeding and affected-host builds.
- [x] PostgreSQL migration applied; EF has no pending model changes.

### Slice 3b completion notes (2026-07-24)

- Four-level definition chain implemented with tenant-scoped mutation lock,
  restrictive FKs, soft-delete child guards and optimistic concurrency.
- Strict string enum wire contract rejects numeric/unknown values; domain guards
  prevent undefined enum persistence.
- Integration suite passes 25/25, including cross-tenant parent guessing,
  host/tenant normalized uniqueness, unique step order, stale concurrency and
  delete-vs-create/re-parent races.
- Migration `20260724134243_AddWorkflowCatalog` is applied to PostgreSQL local;
  EF reports no pending model changes. Final code review: GO, zero blockers.
- Follow-up debt: enforce tenant equality for ETL/direct SQL at the database/import
  boundary, and either fully map audit DTO fields or narrow the public DTO base.

## Slice 3c contract — workflow instances, assignments and history

### Cook sub-slices

| Slice | Scope |
|-------|-------|
| 3c1 | Step-assignee configuration + submit preview + atomic workflow submission |
| 3c2 | Approve/process, return, reject, cancel and returned-workflow resubmit |
| 3c3 | Parallel workflow, VIEW unlock scopes, signer replacement and status queries |
| 3c4 | Overdue/extension policy, immutable logs/history, migration and runtime E2E |

Signing cryptography, signatures and signed-file generation remain in 3d/3f.
Slice 3c records the `SIGN` action and result-file reference contract, but does not
perform HSM/REMOTE_CA signing.

### Owned runtime model

```
WorkflowStepTemplate
  └─ WorkflowStepAssignmentConfiguration

Document
  └─ DocumentWorkflowInstance
       ├─ DocumentAssignment
       └─ DocumentWorkflowInstanceLog
  └─ DocumentHistory
```

- `WorkflowStepAssignmentConfiguration` replaces the HCS
  `WorkflowStepAssignment` definition entity. A step may have multiple active
  configurations; at most one active configuration is primary. It belongs to one
  step and uses:
  `SpecificUser(0)`, `RoleInSubmitterOrganizationUnit(1)`,
  `ScopedAssignee(2)`. It stores normalized ABP `OrganizationUnitId`, user and
  role references; there is no `Department`/`UserDepartment`.
- Configuration fields are: required `WorkflowStepTemplateId`, required
  `AssigneeType`, nullable `RoleId`, required `IsPrimary`, required `IsActive`
  and concurrency stamp. Users and OUs are normalized child rows
  `WorkflowStepAssignmentUser(UserId)` and
  `WorkflowStepAssignmentOrganizationUnit(OrganizationUnitId)` with unique
  `(TenantId, ConfigurationId, external-id)` indexes; JSON lists are accepted
  only by the HCS import adapter and are normalized before persistence.
- Mode invariants: `SpecificUser` requires at least one user and forbids role/OUs;
  `RoleInSubmitterOrganizationUnit` requires exactly one role and forbids
  configured users/OUs; `ScopedAssignee` requires a non-empty user/OU union and
  permits an optional role to filter users resolved from configured OUs.
- Configuration → step uses a restrictive FK. Config create/update/re-parent and
  step delete use the same tenant workflow-catalog lock and transaction. A step
  with live configurations cannot be soft-deleted. Every child row carries the
  same tenant as its configuration; application and ETL import validate equality.
- OU/user/role GUIDs are external IdentityService references. Application writes
  validate them remotely and fail closed when IdentityService is unavailable.
  DocumentService does not create cross-database FKs or joins.
- `DocumentWorkflowInstance` owns immutable
  `DocumentWorkflowCommittedStep` rows captured at submission. Each snapshot
  contains template step ID, order, name, type, `AllowReturn`, SLA days,
  and the fully resolved candidate/selected receiver IDs plus VIEW
  OU/user scope rows. Runtime never re-reads mutable template/config execution
  fields. Template/config update or soft-delete is allowed after commit because
  the snapshot remains authoritative for the in-flight instance.
- Sign mode is instance-wide, not a step field. Nullable template `SignMode`
  resolves to the stable default `Sequential` at preview/submission and the
  non-null resolved value is persisted on `DocumentWorkflowInstance`.
- One document may have only one active instance (`InProgress` or `Overdue`) per
  tenant. A returned/rejected/cancelled/completed instance remains immutable
  history; resubmit creates a new instance.
- Runtime assignment receiver is an ABP user ID. Assignments reference the
  instance directly in addition to document and step, preventing ambiguous
  lookup when a document is resubmitted.
- Logs and document histories are append-only application records. They are not
  exposed through generic update/delete APIs.

### Stable enums

All enums persist as the numeric values below and serialize as strict strings;
integer/unknown JSON values are rejected.

- `DocumentWorkflowStatus`:
  `Draft(0)`, `InProgress(1)`, `Overdue(2)`, `Completed(3)`,
  `Rejected(4)`, `Returned(5)`, `Cancelled(6)`.
- `DocumentAssignmentAction`:
  `Process(0)`, `Sign(1)`, `View(2)`.
- `DocumentAssignmentStatus`:
  `Pending(0)`, `Done(1)`, `Rejected(2)`, `Revoked(3)`.
- `WorkflowRuntimeAction`:
  `Submit(0)`, `Approve(1)`, `RequestSign(2)`, `ConfirmSign(3)`,
  `Return(4)`, `Reject(5)`,
  `Cancel(6)`, `AssignUser(7)`, `UpdateSigner(8)`, `MarkOverdue(9)`,
  `Extend(10)`, `Complete(11)`, `Resubmit(12)`.

### Submission and assignment rules

- Submission is serialized by tenant + document lock and committed in one
  transaction: validate document/workflow/template/steps, ensure no active
  instance, resolve assignees, create instance/assignments, append submit
  log/history and update document status.
- Active steps must be non-empty and have unique order. Each actionable
  `Process`/`Sign` step must resolve at least one enabled Identity user.
- `SpecificUser` resolves configured default users; a caller selection may pick
  one candidate but cannot inject a user outside the resolved set.
- `RoleInSubmitterOrganizationUnit` uses a deterministic primary OU: the
  submitter's earliest-created active ABP user↔OU membership. Candidate scope is
  that OU followed by its ancestor chain to root; nearest OU wins when the same
  user appears more than once. Candidates must be enabled users with the
  configured direct ABP user-role membership and membership in one of those OUs.
  Preview returns the chosen primary OU and candidate provenance. Submission
  re-resolves it and fails when membership changed, the submitter has no OU or no
  enabled candidate exists.
- `ScopedAssignee` resolves the union of configured ABP OUs and users. A caller
  selection must belong to that resolved candidate set.
- Multiple active configurations compose by union of enabled candidates keyed by
  `UserId`; no intersection is applied. Duplicate provenance is selected
  deterministically from a primary configuration first, then smallest OU depth,
  then configuration creation time and configuration ID. The final preview order
  is primary provenance first, OU depth, normalized display name and user ID.
- Each actionable step has exactly one selected runtime receiver. A single final
  candidate is auto-selected; when the union contains multiple candidates the
  caller must choose exactly one `UserId` from the final union. `IsPrimary`
  affects provenance/ranking only and never bypasses explicit selection.
  Parallel mode still selects exactly one receiver independently for each
  actionable step. VIEW scopes may resolve many viewers and do not create
  actionable assignments.
- VIEW scopes use only ABP OU/user IDs. Sequential mode unlocks VIEW steps when
  reached and advances past them; parallel mode unlocks all committed VIEW steps
  at submission.
- Sequential mode creates pending assignments only for the current actionable
  step and advances after its completion. Parallel mode creates all actionable
  assignments at submission and completes when every committed actionable step
  has a completed assignment.
- Assignment actions require the authenticated user to own a current pending
  assignment. Replays with the same action are idempotent; stale concurrency or
  conflicting terminal actions are rejected.

### Authorization and document eligibility

- Preview and submit require `DocumentService.Documents.SubmitWorkflow`.
  Cross-user override requires the separate
  `DocumentService.Documents.SubmitWorkflowAll` permission.
- Without override, the current user must be the document creator, current
  receiver, or an enabled member of the document's ABP `OrganizationUnitId`.
  A document without any matching owner/receiver/OU is creator-only.
- Submission is allowed only when the document is not soft-deleted, has no active
  workflow, is not in a terminal workflow status, and has at least one available
  non-pending-deletion file unless a valid workflow template file will be
  materialized by slice 3f. Until 3f exists, template-file submission is rejected.
- Preview applies the same document record-level rule and returns a short-lived
  opaque preview token bound to tenant, document, workflow, submitter, template
  concurrency stamp and resolved candidate hash. Submit revalidates every rule
  and candidate; the token is an optimization, never authorization evidence.

### Safe SIGN boundary before 3d

- Slice 3c cannot complete a `Sign` assignment. The public action records an
  idempotent `RequestSign` log/intent while leaving the assignment `Pending`.
- Only the internal 3d signing completion command may emit `ConfirmSign`, and it
  must supply a verified `DocumentFileResultId` belonging to the same tenant,
  document and instance. That command atomically marks the assignment `Done` and
  advances/completes the workflow.
- Client-provided file IDs never prove a signature. Sequential or parallel
  workflows containing a `Sign` step therefore remain in progress until 3d is
  installed; 3c tests assert they cannot falsely complete.

### Authoritative runtime field matrix

| Record | Required/nullable fields and invariants |
|--------|-----------------------------------------|
| `DocumentWorkflowInstance` | required `TenantId?`, `DocumentId`, `WorkflowId`, `WorkflowTemplateId`, `InitiatorUserId`, non-null resolved `SignMode`, `Status`, `StartedAtUtc`; nullable `CurrentCommittedStepId`, `DeadlineAtUtc`, `FinishedAtUtc`, `OverdueAtUtc`, `PreviousInstanceId`; non-negative `ExtensionCount`, `TotalExtensionBusinessDays`; concurrency stamp |
| `DocumentWorkflowCommittedStep` | required instance/template-step IDs, order 1..10000, name, type, allow-return; nullable non-negative SLA; immutable after insert; unique `(TenantId, InstanceId, Order)` and `(TenantId, InstanceId, TemplateStepId)` |
| `DocumentWorkflowCommittedReceiver` | required committed-step ID and enabled `UserId`; `IsSelected`, `IsPrimary`, nullable provenance OU/role; unique receiver per committed step |
| `DocumentWorkflowCommittedViewScope` | required committed-step ID and exactly one of `OrganizationUnitId`/`UserId`; unique normalized scope |
| `DocumentAssignment` | required instance/document/committed-step/receiver IDs, action, status, `AssignedAtUtc`, `IsCurrent`; nullable `ProcessedAtUtc`, verified `DocumentFileResultId`; `ProcessedAtUtc` is null while pending and required after terminal assignment state; concurrency stamp |
| `DocumentWorkflowInstanceLog` | append-only; required instance, action, `OccurredAtUtc`; nullable assignment, actor user, from/to status, actor role and note; role/status max 50, note max 2000 |
| `DocumentHistory` | append-only; required document, instance, action, `OccurredAtUtc`; nullable from/to user and comment; comment max 2000 |

- `CurrentCommittedStepId` is required for active sequential instances, nullable
  for active parallel and all terminal instances. `FinishedAtUtc` is null while
  active and required for `Completed|Returned|Rejected|Cancelled`.
- `PreviousInstanceId` is nullable on first submission, unique when present and
  must reference a terminal `Returned` instance for the same tenant/document.
- All timestamps are UTC. Deadline is distinct from finished time; HCS
  `FinishedAt` deadline overloading is normalized during ETL.
- Document status mapping is exact:
  `InProgress → WORKFLOW_IN_PROGRESS`,
  `Overdue → WORKFLOW_OVERDUE`,
  `Completed → WORKFLOW_COMPLETED`,
  `Returned → WORKFLOW_RETURNED`,
  `Rejected → WORKFLOW_REJECTED`,
  `Cancelled → WORKFLOW_CANCELLED`.
  Instance transition and `Document.CurrentStatus` update share one transaction.

### State transitions and deadlines

- `InProgress -> Completed|Returned|Rejected|Cancelled|Overdue`.
- `Overdue -> InProgress` only through a valid extension; otherwise it may be
  cancelled by policy.
- `Returned` resubmission creates a new `InProgress` instance and links it with
  `PreviousInstanceId`; the old instance is not reopened.
- Return is permitted only when the current step snapshot has `AllowReturn`.
- Initiator cancel is allowed only before any `Sign` assignment is completed.
- `StartedAtUtc` is required. `FinishedAtUtc` is nullable and set only for terminal
  statuses. `OverdueAtUtc` is set only while overdue. Extension count and total
  business days are non-negative and append an audit log containing the reason.

### Persistence and concurrency gates

- Restrictive FKs inside DocumentService:
  instance → document/workflow/template/previous instance;
  committed step → instance/template step; committed receiver/view scope →
  committed step; assignment → instance/document/committed step/result file;
  configuration → template step; log → instance/assignment;
  history → document/instance.
- Tenant-aware indexes cover active instance per document, pending assignments
  per receiver, instance timeline, document history and current step.
- All state-changing commands use an instance/document distributed lock,
  transactional UoW and concurrency stamp. The lock is held through commit.
- Database constraints/checks enforce non-negative counters, valid timestamp
  shape and single-active-instance semantics where PostgreSQL supports it.

### Slice 3c gates

- [x] Assignee configuration CRUD with batch ABP OU/user/role validation.
- [x] Atomic sequential and parallel submission with committed-step snapshots.
- [x] Approve/sign-record, return, reject, cancel and resubmit transitions.
- [x] VIEW scopes, signer replacement, overdue and extension behavior.
- [x] Append-only runtime logs/history and paged status/assignment queries.
- [ ] Tenant isolation, authorization, concurrency, replay and race tests.
- [x] PostgreSQL migration applied; EF has no pending model changes.

### Slice 3c1 completion notes (2026-07-24)

- Submission preview uses a short-lived protected token bound to tenant,
  document, workflow/template, initiator, template stamp and resolved candidate
  semantics. Submit revalidates record access, mutable catalog data, Identity
  candidates and document concurrency before persistence.
- Sequential and parallel submissions persist immutable step, receiver and VIEW
  snapshots, assignments, submit log/history and document status in one
  transaction. Nullable template sign mode resolves to `Sequential`.
- Catalog mutation, submit and file deletion share ordered distributed locks
  (`catalog -> document`) so a submitted snapshot cannot race catalog edits or
  loss of the final eligible document file.
- Migration `20260724143728_AddWorkflowRuntime` is applied to PostgreSQL local.
  Host and tenant uniqueness use separate partial indexes; EF reports no pending
  model changes.
- Submission regression suite passes 7/7, covering sequential/parallel
  persistence, stale candidate and document stamps, token tampering, duplicate
  active workflow rejection and null sign-mode defaulting.
- Remaining slice 3c test debt is tracked by the unchecked tenant,
  authorization, replay and race gate and will be closed with 3c2–3c4.

### Slice 3c2 completion notes (2026-07-24)

- Process approval advances sequential workflows from immutable selected
  receivers, and parallel workflows complete only after every actionable
  assignment is done. Final completion updates the document in the same
  transaction.
- Return honors snapshotted `AllowReturn`; reject, return and cancel terminate
  the instance and revoke remaining pending assignments. Cancel is restricted
  to the initiator and is forbidden after a verified SIGN completion.
- SIGN remains a safe boundary: `RequestSign` is idempotent audit intent and
  leaves the assignment pending. Revoked or terminal assignments cannot append
  signing intent.
- Returned workflows resubmit into a new instance linked by
  `PreviousInstanceId`; the protected preview token binds the returned source
  and the old instance remains immutable.
- Focused runtime suite passes 12/12 and full DocumentService passes 43/43.
  Final review: GO after closing cancelled-replay authorization and revoked-SIGN
  request bypasses.

### Slices 3c3–3c4 completion notes (2026-07-24)

- Sequential VIEW scopes unlock only when execution reaches their order;
  parallel scopes unlock immediately. Runtime queries authorize initiators,
  assignees, direct VIEW users and current members of committed ABP OU scopes.
  Locked future scope identifiers are redacted.
- Current pending SIGN assignments may be replaced only by the initiator with
  another enabled user from the immutable committed candidate set. Identity
  validation fails closed and replacement writes atomic log/history records.
- Submission derives the instance deadline from actionable snapshot SLA values:
  sequential uses their sum and parallel uses their maximum. Business-day
  arithmetic preserves UTC time and skips weekends.
- Overdue marking requires the dedicated scheduler/admin
  `WorkflowRuntime.MarkOverdue` permission. Initiator extensions require a
  reason, restore `InProgress`, move the deadline by bounded business days and
  accumulate counters with immutable audit.
- Migration `20260724150845_HardenWorkflowRuntimeConstraints` adds database
  checks for extension counters, terminal/overdue timestamp shape and assignment
  pending/processed shape. It is applied to PostgreSQL and EF reports no pending
  model changes.
- [ ] Authenticated Gateway runtime E2E and final tester/reviewer GO.

## Slice 3d contract — signing

### Cook sub-slices

| Slice | Scope |
|-------|-------|
| 3d1 | `SignatureSetting` + `UserSignature` metadata, permissions, secure credential write path — **complete** |
| 3d2 | Signed-artifact model + electronic signing execution — **complete** |
| 3d3 | REMOTE_CA adapter (HMAC-SHA256) + Bnn/HSM adapter boundary — **complete** |
| 3d4 | Atomic SIGN confirmation, retry/idempotency, migration and provider E2E — **complete** |

### Ownership and security boundary

- DocumentService owns signing metadata and orchestration. IdentityService remains
  authoritative for ABP users and OUs; there is no `Department` or
  `UserDepartment`.
- `SignatureSetting` contains non-secret provider configuration:
  normalized unique `ProviderCode`, `ProviderType`, `ApiEndpoint`, timeout,
  supported sign modes, layout/blob references, dimensions, output naming and
  activation flags.
- `UserSignature` belongs to one ABP `IdentityUserId` and references a provider
  by normalized `ProviderCode`. It stores sign type, token/key reference,
  signature/seal blob references, validity window and activation state.
- Provider or user credential material is never returned by an API, included in
  audit payloads, logged or copied to immutable workflow snapshots. Write DTOs
  accept a replacement secret only over authorized endpoints and read DTOs expose
  `HasSecret` only.
- At-rest credential storage uses ASP.NET Core Data Protection encryption with a
  purpose scoped to tenant + user signature + provider. Encryption keys must be
  persisted outside PostgreSQL through the existing shared Data Protection
  configuration. REMOTE_CA secret remains Base64 after decrypting because that
  is the provider's HMAC key format.
- Update with null/blank secret preserves the existing encrypted credential.
  Credential clearing requires a separate explicit operation and permission.
  Deactivate/delete is rejected while a live signing attempt references the
  record.
- `ApiEndpoint` must be absolute HTTP(S). Production forbids cleartext HTTP and
  loopback/private-network endpoints unless explicitly allowlisted by deployment
  configuration; redirects are disabled in provider clients to reduce SSRF risk.

### Stable enums and invariants

- `SignatureType`: `Electronic(0)`, `Digital(1)`.
- `SignatureProviderType`: `Hsm(0)`, `RemoteCa(1)`, `UsbToken(2)`.
- `SigningAttemptStatus`:
  `Pending(0)`, `Processing(1)`, `Succeeded(2)`, `Failed(3)`,
  `Cancelled(4)`.
- Enums persist with the numeric values above and use strict string JSON.
- Provider code is trimmed and normalized uppercase. Host and tenant uniqueness
  use separate partial indexes for active/soft-deleted rows.
- User signature validity requires `ValidTo >= ValidFrom` when both exist.
  Active digital signatures require a token reference and credential; electronic
  signatures require neither. All signature/layout/seal images are private typed
  blob references, never caller-controlled filesystem paths or public URLs.
- User-signature create/update validates the referenced ABP user through the
  existing internal IdentityService batch endpoint and fails closed. A normal
  user may manage only their own signatures; an administrative override requires
  a separate permission.

### Signing image migration runbook

- `AddSigningAssets` is intentionally fail-fast when legacy `UserSignatures`
  or a non-null `SignatureSettings.LayoutImageBlobName` exists. Never bypass
  that guard or replace missing assets with `Guid.Empty`.
- Before upgrading a populated environment: stop signing writes, export the
  three legacy blob-name columns with tenant/user/provider ownership, copy each
  source object into the private `hanhchinhso-signing` container using a
  server-generated key, decode and normalize it as PNG/JPEG, then calculate
  size and SHA-256 from the normalized bytes.
- Insert one `SigningAssets` row per copied object with the exact tenant,
  `SignatureImage`/`SealImage` owner user, or ownerless `LayoutImage`; update
  `SignatureAssetId`, `SealAssetId` and `LayoutAssetId` in the same database
  transaction. Abort and remove copied objects if any source is missing,
  malformed, duplicated across tenants, or has ambiguous ownership.
- Verify every required signature reference is non-null and resolves to the
  correct tenant/kind/owner, compare row and blob counts plus hashes, take a
  database backup, then apply `AddSigningAssets`. Keep the export and validation
  report until signing E2E passes; rollback restores the backup and legacy
  container rather than running the destructive migration `Down`.

### Signing execution and workflow atomicity

- `RequestSign` remains intent-only. A new provider attempt uses an idempotency key
  derived from tenant, workflow instance, assignment, source file hash and
  selected user signature. Only one non-terminal attempt exists for that key.
- The source `DocumentFile` is immutable. A successful operation writes a new
  private blob, verifies it is a non-empty PDF and records its SHA-256 hash before
  publishing a new signed `DocumentFile` linked to its source.
- Provider network calls are not held inside a database transaction. State moves
  `Pending → Processing`, performs the external call, then under the shared
  document lock atomically publishes the artifact, marks the attempt succeeded,
  confirms the pending SIGN assignment and advances/completes the workflow.
- Empty, malformed, mismatched or unverifiable provider output fails the attempt
  and leaves the SIGN assignment pending. Blob/DB compensation follows the 3a
  orphan-cleanup contract.
- Retry reuses the same attempt/idempotency key. A succeeded attempt returns the
  existing result and cannot call the provider again. Stale concurrency,
  replaced/revoked signer, terminal instance or changed source file fails closed.
- REMOTE_CA signs through a typed `HttpClient`; HMAC-SHA256 canonicalization and
  response parsing are isolated behind `IRemoteCaSigningProvider`. Bnn/HSM is
  isolated behind `IBnnSigningProvider`; proprietary `Bnn.SignLib` types do not
  leak into application contracts or workflow domain entities.
- Provider logs contain correlation/attempt IDs, provider code, duration and
  result category only. Tokens, secrets, PDF/image bytes, HMAC headers and full
  provider response bodies are forbidden.

### Slice 3d gates

- [x] 3d1 metadata/contracts/permissions, Identity validation and encrypted
  credential round-trip tests.
- [x] Tenant isolation, owner/admin authorization, secret redaction, stale
  concurrency, provider-code uniqueness and validity-window tests.
- [x] Signed artifact and electronic-sign tests, including blob/DB compensation.
- [x] REMOTE_CA HMAC canonical-vector tests, timeout/redirect/SSRF controls and
  fake-provider integration tests.
- [ ] Atomic SIGN confirmation, replay/race/replaced-signer tests and authenticated
  Gateway E2E.
- [x] Migration applied to PostgreSQL; EF has no pending model changes; final
  tester and reviewer GO.

### Slice 3d4 completion notes (2026-07-24)

- Electronic and digital execution share one idempotent orchestration path.
  REMOTE_CA/Bnn calls run outside a conventional ABP unit of work; final
  artifact publication, SIGN confirmation, workflow advancement and attempt
  success commit atomically under the document lock.
- Completion also holds the tenant signing-metadata lock used by every
  signature/provider mutation. Active state, concurrency stamps, provider
  policy and credential validity are rechecked under that lock, closing
  revoke/update/delete races during a slow provider call.
- Invalid or unchanged provider PDF output fails closed. Provider, artifact
  upload and post-upload completion failures retain durable cleanup ownership,
  clear safe reservations and permit immediate retry on the same attempt.
- Focused execution coverage is 7/7 (electronic, digital, provider retry,
  unchanged output, expiry, revoke race and artifact-save retry). Full
  DocumentService coverage is 76/76; production build has zero errors and EF
  reports no pending model changes. Reviewer and debugger gates are GO.
- Authenticated Gateway signing E2E remains open and is intentionally retained
  in the combined gate above.

## Source (HCS) — high coupling

- `Documents*`, `DocumentFiles`, `DocumentAssignments`, `DocumentHistories`
- `DocumentWorkflowInstances*`, `WorkflowDefinitions`, `Workflows`, `WorkflowTemplates`, `WorkflowStep*`
- `SignatureSettings`, `UserSignatures`, `SigningKpiReports` (KPI có thể defer Phase 7 — chỉ execution ở đây)
- Domain.Shared: `BnnSoftSigns`, `RemoteSigns`
- Integrations: MinIO, LibreOffice, Bnn.SignLib, REMOTE_CA HMAC
- Workers: background signing / convert nếu có trong HCS

## Target layout

```
services/abp-blazor/services/document/
  hanhchinhso.DocumentService/          # Host + Data + HttpApi controllers + signing services
  hanhchinhso.DocumentService.Contracts/
  hanhchinhso.DocumentService.Tests/
# optional later:
  hanhchinhso.DocumentService.Worker/   # nếu tách hangfire/background
```

## Infra

- MinIO bucket `hanhchinhso_documents` (lab)
- LibreOffice: sidecar compose **hoặc** remote URL (chốt khi cook 3f)
- Secrets: User Secrets / env — không commit REMOTE_CA secret

## Cross-service

- Org refs (`OrganizationUnitId`): GUID tham chiếu **ABP Identity Organization Unit**;
  validate theo batch qua endpoint nội bộ IdentityService, không gọi
  OrganizationService cho Department và không chuyển tiếp quyền Identity của
  người thao tác.
- Identity users: ABP current user + Identity lookup
- DocumentService dùng confidential client `DocumentService.Internal` với
  client-credentials scope `IdentityService`. Secret chỉ cấu hình đồng thời qua
  env/User Secrets:
  `OpenIddict__Applications__DocumentServiceInternal__ClientSecret` ở
  IdentityService/AuthServer seed context và
  `IdentityValidation__ClientSecret` ở DocumentService. Thiếu secret, token lỗi
  hoặc IdentityService không khả dụng đều fail closed trước khi ghi cấu hình.
- Request nội bộ chuyển tenant bằng ABP `__tenant` header; IdentityService vẫn
  áp dụng data filter tenant khi kiểm tra user/OU/role.

## Mobile parity

- Đọc `HCS_web/docs/mobile-*.md` — replicate endpoints/DTOs cần thiết
- Auth: Bearer từ AuthServer (giữ Approach A)
- Push device token: có thể stub → full ở Phase 6 Collaboration

### Slice 3e contract — Document mobile API

| Slice | Scope | Status |
|-------|-------|--------|
| 3e1 | Mobile signing inbox + aggregated workflow detail/timeline | ✅ Complete |
| 3e2 | Mobile workflow actions + eligible signature selection | Pending |
| 3e3 | Submit/resubmit compatibility over preview-token flow + native file upload | Pending |
| 3e4 | Authenticated Gateway E2E and HCS parity checklist | Pending |

- Mobile APIs are a DocumentService facade over the existing document,
  workflow and signing aggregates; they must not recreate monolith tables or
  bypass domain application services.
- Inbox authorization is applied in SQL before counts and pagination. `All` is
  only the union of instances where the current user is initiator, has any
  current or historical assignment, has an unlocked direct VIEW scope, or is a
  current member of an unlocked ABP OU VIEW scope. It is never a tenant-wide
  workflow query.
- `SentToMe` is the assignment/unlocked-VIEW subset above and retains terminal
  history; `CanAct` is true only for a current pending assignment owned by the
  caller. `SentByMe` is the initiator subset and `CanResubmit` is true only for
  the caller's returned instance. `Following` is an explicit empty stub with
  count zero, matching current HCS behavior; a real watch/follow model is
  deferred unless separately planned.
- Inbox uses whitelisted sort fields, a deterministic ID tie-breaker and a
  bounded page size. It pages authorized instance IDs first, then performs
  bounded projections for document/current-step/assignment data.
- Detail returns one authorized aggregate for the mobile action screen:
  instance, document summary, committed-step timeline, assignments, workflow
  logs, document history and non-deleted workflow files. Comments/notes remain
  untrusted content and clients must encode or sanitize them at the render
  boundary. It applies the same
  initiator/assignee/unlocked ABP OU-view authorization as runtime queries and
  never exposes blob names.
- Until an explicit workflow-instance attachment link exists, detail exposes
  only non-deleted files referenced by immutable `SourceFileId`,
  `CurrentSignedFileId` or
  `DocumentAssignment.DocumentFileResultId` for that instance. It does not
  return every file on the document and does not claim legacy attachment parity.
- File listing and `GET /api/document-management/files/{id}/content` enforce
  object-level document access with the same initiator/assignment/unlocked-VIEW
  predicate (plus personal-document ownership where applicable). An opaque GUID
  is an identifier, not authorization; cross-user ID guessing fails closed.
- Action endpoints accept explicit assignment/instance concurrency stamps and
  delegate to `IWorkflowActionAppService` plus
  `ISigningExecutionAppService`. `APPROVE`, `RETURN`, `REJECT`,
  `ELECTRONIC` and `DIGITAL` remain strict strings; mobile cannot upload a
  caller-produced signed PDF.
- `CanAct` and `CanResubmit` are presentation hints only. Every mutation derives
  authorization and state again through the existing workflow/signing
  application services.
- Eligible-signature lookup is always scoped to the current ABP user and
  filters active records by validity window, requested signature type and
  active provider capability. It exposes `HasSecret`, never the encrypted or
  plaintext credential.
- Submit/resubmit keeps the preview-token contract. A returned instance is
  resubmitted by previewing with `PreviousInstanceId` then submitting the
  signed preview; this preserves immutable candidate snapshots and avoids the
  legacy monolith's mutable all-in-one submit DTO.
- Preview requires an explicit `SourceFileId`. The server validates that it is
  a non-deleted file owned by the selected document and records both its ID and
  SHA-256 in the protected preview token. Submit revalidates the file/hash under
  the document lock and persists immutable
  `DocumentWorkflowInstance.SourceFileId`. There is no “latest file” fallback.
  Resubmission selects and binds a new source on the new instance while the
  returned instance keeps its original source reference.
- Before slice 3f conversion is available, a source selected for a SIGN
  workflow must be PDF. The first signature consumes the immutable instance
  source; each later signature consumes only `CurrentSignedFileId`. Caller input
  cannot substitute another document file.
- Native PDF/DOCX upload reuses
  `POST /api/document-management/files/{documentId}` and download by opaque
  file ID. Signature/seal asset upload reuses the typed signing-asset
  controller; no path-based blob API is introduced.

### Slice 3e gates

- [ ] Inbox modes, stable paging/filtering and tenant/user isolation tests.
- [ ] Detail authorization, locked OU scope redaction, timeline/log/history and
  opaque-file tests.
- [ ] Electronic/digital mobile action E2E, stale concurrency, retry and
  eligible-signature redaction tests.
- [ ] Preview-token submit/resubmit plus native upload/download integration
  tests.
- [ ] Authenticated Gateway E2E, full suite/build, reviewer/debugger/tester GO.

## Success criteria

- [ ] E2E: tạo văn bản → trình ký → ký REMOTE_CA (hoặc Bnn lab) → hoàn tất
- [ ] Mobile API critical paths green theo docs HCS
- [ ] Blob round-trip MinIO
- [ ] DOCX↔PDF path works (LibreOffice)
- [ ] Parity checklist Document/Workflow/Sign = done (KPI report có thể partial)
- [ ] Mud UI flow chính usable (không cần mọi màn Blazorise)

## Risks

| Risk | Mitigation |
|------|------------|
| Scope creep peel Signing sớm | Forbidden trong phase này |
| Elsa confusion | README DocumentService + ADR |
| Perf indexes HCS | Port indexes quan trọng trong migration mới |
| Plaintext secrets HCS | Không copy; dùng secure config |

## Depends

Phase 2 Organization (master data/org cho assignment). Phase 1 MinIO.

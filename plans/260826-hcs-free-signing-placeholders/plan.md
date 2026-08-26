---
title: "HCS free-license workflow signing placeholders"
description: "Port the licensed workflow submission/signing preparation semantics into HCS free-license: validate electronic signature, merge submitter metadata, and persist signing notes/placeholders before provider signing."
status: completed
priority: P1
effort: 3-4d
branch: main
tags: [hcs, free-license, workflow, signing, placeholders, note-content]
created: 2026-08-26
updated: 2026-08-26
createdBy: ck:scout
---

# HCS free-license workflow signing placeholders

## Outcome

Khi trình một workflow có bước ký trong `services/HCS_web_free_license`:

- server kiểm tra current user có chữ ký điện tử đang active, trong thời hạn và có ảnh;
- chuẩn bị file ký trước khi tạo review, thay các merge field của người trình ký:
  `<<PreparedBySign>>`, `<<PreparedFullName>>`, `<<VitriVieclam>>`, `<<ViTriLamViec>>`,
  `<<Position>>`, `<<PositionName>>`, `<<Department>>`, `<<PhongBan>>`, và `<<ContentToBeApproved>>`;
- giữ metadata/hash/blob nhất quán cho file DOCX/PDF sau merge;
- khi ký từng bước, thay `<<SignNN>>`, `<<FullNameNN>>`, `<<NoteContentNN>>` trước/đồng thời với provider signing;
- `NoteContent` được lấy từ `SignDocumentRequest.Note` và không bị mất khi provider được gọi.

`services/HCS_web_with_license` chỉ là behavioral reference read-only. Không thêm project/package/runtime dependency tới licensed tree hoặc licensed `HC.*` assembly; mọi code phải dùng contract/domain/provider có sẵn trong free.

## Scout evidence and constraints

| Finding | Consequence |
|---|---|
| Licensed calls `PrepareSubmissionPlaceholdersAsync` after choosing the signing file and before assignments/history. | Free preparation belongs in `WorkflowAppService.StartAsync` after document files are copied/attached and before `StartReview`. |
| Free `StartWorkflowRequest.SigningContent` is currently only persisted as review history detail. | Keep that history behavior and also use the same bounded content for merge fields. |
| Free has `UserSignature` blobs and active/default/validity metadata in `DocumentServiceDbContext`. | Validate and load the electronic signature in DocumentService; do not trust a client-provided image. |
| Free DocumentService has no identity/organization EF reference. | Resolve display name through the existing Platform API and add a minimal authorized Organization lookup for department/position names; do not cross-read another service database. |
| Free provider adapters operate on PDF and already receive `SignerName` and `Note`. | Merge name/note placeholders in the PDF before adapter execution; preserve the provider note payload. |
| Licensed spelling differs from the request (`ViTriLamViec`, `PositionName`, `NoteContentNN`). | Support both the exact requested aliases and the licensed numbered/canonical variants. |
| Free has no placeholder/merge tests. | Add focused pure/helper tests plus workflow/signing regression tests; build the DocumentService and relevant gateway/client projects. |

## Locked architecture

```text
SubmitWorkflowModal
  → POST /api/workflows/instances (existing free contract)
  → WorkflowAppService.StartAsync
      → choose/copy template/source files
      → WorkflowSubmissionPreparationService
          → validate UserSignature in document DB
          → resolve current user + org display metadata over authorized HTTP
          → merge DOCX/PDF and update blob/hash/pair metadata
      → StartReview + WorkflowInstance + assignments/history

DocumentSigning
  → POST /api/signing/attempts (existing free contract)
  → SigningAppService.SignAsync
      → resolve step suffix from SignNN placeholder
      → merge FullNameNN/NoteContentNN in working PDF/DOCX-derived PDF
      → existing Electronic/RemoteCA/HSM/USB provider adapter
```

## Phase order

1. `phase-01-contract-and-profile.md` — add only additive profile lookup data needed by DocumentService and keep auth/permission boundary explicit.
2. `phase-02-submission-preparation.md` — validate electronic signature, merge prepared placeholders, persist file/hash/pair state, and wire `StartAsync`.
3. `phase-03-signing-note-placeholders.md` — merge per-step signing name/note placeholders while preserving all provider adapters and signing note behavior.
4. `phase-04-tests-and-review.md` — focused tests, builds, license-boundary audit, diff review and handoff.

## File ownership

| Phase | Owned paths |
|---|---|
| 01 | `services/HCS_web_free_license/services/organization/HCS.OrganizationService/Contracts/OrganizationDtos.cs`; `.../Application/IOrganizationAppService.cs`; `.../Application/OrganizationAppService.cs`; `.../Host/Controllers/OrganizationControllers.cs`; `services/HCS_web_free_license/services/document/HCS.DocumentService/Workflows/HttpWorkflowAssigneeResolver.cs` or a new sibling resolver; document `appsettings*.json` and compose env only if required. |
| 02 | `services/HCS_web_free_license/services/document/HCS.DocumentService/Workflows/WorkflowAppService.cs`; new `Workflows/WorkflowSubmissionPreparationService.cs`; new `Workflows/WordPlaceholderReplacer.cs`; `Documents/DocumentAggregate.cs`; document csproj/package lock only for required free packages. |
| 03 | `services/HCS_web_free_license/services/document/HCS.DocumentService/Signing/SigningAppService.cs`; `Signing/SigningProviders.cs` only for reusable PDF placeholder helpers; new focused signing helper/tests. |
| 04 | `services/HCS_web_free_license/services/document/HCS.DocumentService.Tests/**`, relevant client/gateway contract tests, this plan/phase reports. |

Existing unrelated worktree changes are preserved. No commit, reset, revert or destructive cleanup is authorized by this request.

## Acceptance criteria

- A workflow with a SIGN step fails before creating an in-review instance when the submitter has no valid active electronic signature; no partial workflow/history is committed.
- A valid electronic signature causes requested prepared placeholders and aliases to be replaced in the actual target DOCX/PDF blob; the file metadata hash matches stored bytes and paired files remain linked.
- A Word workflow uses the merged DOCX to generate its PDF so layout/image replacement is not lost.
- Each approval signature passes the note through the existing adapter and replaces the correct numbered `FullNameNN`/`NoteContentNN` fields; failed provider calls do not approve the task.
- No provider secret, signature image, or untrusted client value bypasses ownership/validity checks.
- `dotnet build`/targeted `dotnet test`, `git diff --check`, and `./scripts/audit-license-clean.sh` pass for the changed free tree.

## Known boundary

- PDF text replacement is overlay-based because free does not contain a PDF text-editing abstraction. If a PDF template has no detectable text glyphs for a placeholder, the helper must leave it untouched and log/return a recoverable result rather than corrupting the file.
- Workflow-specific logs/attachments remain outside this slice; existing free history/task comment behavior is retained.

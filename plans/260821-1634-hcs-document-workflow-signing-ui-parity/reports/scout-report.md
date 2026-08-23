---
title: "Scout report: HCS free document/workflow/signing UI parity"
status: complete
created: 2026-08-21
source: "Read-only comparison of HCS_web_with_license/src and HCS_web_free_license"
---

# Scout report: HCS free document/workflow/signing UI parity

## Summary

The free client already contains the core Community UI slice. A direct file copy from `HC.Blazor` is unsafe because the licensed UI is split across commercial ABP services/contracts, while the free runtime uses `HCS.Blazor.Client` → BFF → separate service APIs. The correct migration is contract-by-contract parity.

## Relevant free files

- `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/DocumentManagement.razor` — document list, filters, preview, create, send modal and workflow entry.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/DocumentDetail.razor` — create/edit, upload/delete, assignment, direct submit and workflow modal entry.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/DocumentSigning.razor` — signing queue, filters, preview/info/sign modal, signature selection, sign and approve/return/reject actions.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/WorkflowDetail.razor` — workflow wizard, step type/SLA, VIEW scopes and specific-user/submitter-unit role mode.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Components/SubmitWorkflowModal.razor` — document/workflow selection, signer candidates, template option and workflow submission.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Components/DocumentPdfPreviewModal.razor` — file/PDF preview modal and task handoff.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Components/UserSelect2.razor` and `CatalogSelect2.*` — current generic Select2 wrapper.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Documents/DocumentClient.cs` and `DocumentModels.cs` — BFF client contracts for send, workflow, signing and report calls.
- `services/HCS_web_free_license/services/document/HCS.DocumentService/Documents/DocumentAppService.cs` — create/update/assign/submit/send/revoke and list query.
- `services/HCS_web_free_license/services/document/HCS.DocumentService/Workflows/WorkflowAppService.cs` and `WorkflowModels.cs` — workflow definition, role candidate resolution, start, decide and resubmit.
- `services/HCS_web_free_license/services/document/HCS.DocumentService/Signing/SigningAppService.cs` — credential, signature, signing attempt and report adapter boundary.
- `services/HCS_web_free_license/services/platform/HCS.PlatformService/Controllers/ChatContactsController.cs` — current least-privilege contact lookup.
- `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService.Contracts/CollaborationContracts.cs` — current `ChatContactDto` shape.

## Relevant licensed reference files

- `services/HCS_web_with_license/src/HC.Blazor/Pages/Documents/DocumentDetail.razor` and `.razor.cs` — richer document detail/create/send/assignment/view behavior.
- `services/HCS_web_with_license/src/HC.Blazor/Pages/Documents/DocumentSigning.razor`, `.razor.cs` and `DocumentSigning.Rendering.cs` — signing queue/modal, signature/report flow and workflow-aware rendering.
- `services/HCS_web_with_license/src/HC.Blazor/Pages/Workflows/WorkflowDetail.razor` and `.razor.cs` — original workflow step editor and assignment UX.
- `services/HCS_web_with_license/src/HC.Blazor/Components/SubmitWorkflowModal/SubmitWorkflowModal.razor` and `.razor.cs` — original workflow submit modal and candidate handling.
- `services/HCS_web_with_license/src/HC.Blazor/Components/Select2/UserSelect2.razor` and `.razor.cs` — name/phone/avatar result rendering and selected-state handling.
- `services/HCS_web_with_license/src/HC.Blazor/wwwroot/js/hc-user-select2.js` — Select2 HTML templates, remote search and selection synchronization.
- `services/HCS_web_with_license/src/HC.Blazor/Shared/WorkflowPdfDisplayHelper.cs` — workflow-preferred PDF and server-side watermarked PDF loading behavior.
- `services/HCS_web_with_license/src/HC.Application.Contracts/DocumentPdfViewer/IDocumentPdfViewerAppService.cs` — watermarked PDF contract (not portable as-is).
- `services/HCS_web_with_license/src/HC.Application/DocumentPdfViewer/PdfStampingService.cs` — behavior reference only; may not be copied if it depends on restricted code/packages.

## Findings by requirement

### 1. Search/filter/lookup

- Free API clients trim outgoing terms but do not consistently lower them.
- Organization service search uses `Code.Contains(filter) || Name.Contains(filter)` after `Trim`.
- Document service list search normalizes with trim only.
- WorkManagement project/task filters use trim only.
- Platform contacts pass trim-only `filter` into `IIdentityUserRepository`.
- Several free pages use local `Contains(..., StringComparison.OrdinalIgnoreCase)`, while others use ad-hoc `Trim` and filters.
- `CatalogSelect2` delegates its term directly to page callbacks; this is the right central client seam for normalization, but server-side normalization is still required.

Recommended rule: normalize at each HTTP boundary and use a database-translatable case-insensitive contains strategy (`EF.Functions.ILike` where PostgreSQL is used, or lower-cased columns/values where the provider requires it). Keep internal spaces intact; “không space” is interpreted as no leading/trailing spaces, not removing spaces from names.

### 2. User Select2

- Licensed `UserSelect2` receives `LookupDto<Guid>` fields including surname, name, username and phone, then renders escaped HTML with `api/account/profile-picture-file/{id}`.
- Free `ChatContactDto` currently has only `Id`, `UserName`, `DisplayName`, `IsActive`.
- Free `UserSelect2` is only a thin wrapper around generic `CatalogSelect2`; it cannot render per-result HTML or phone/avatar today.
- No free profile-picture endpoint was found in `apps`, `gateways`, `src`, service code or tracked runtime source. Free chat currently renders initials, not images.
- Required contract work: add optional contact presentation fields and a protected avatar source or fallback policy, then add Select2 result/selection templates with HTML encoding and keyboard-safe behavior.

### 3. Workflow detail

- Free page already has `Type`, `SlaDays`, `AllowReturn`, VIEW department/user scopes and `SpecificUser` vs `RoleInSubmitterOu` switching.
- Existing switch clears `AssigneeUserId` when role mode is selected and clears `RoleId` when user mode is selected.
- The current markup places type and SLA in separate fields and renders role selection as a single `CatalogSelect2`.
- `WorkflowStepDto`, `WorkflowStepInput` and `WorkflowStep` currently model one `RoleId`. Multi-role selection is therefore a domain/API/persistence change, not only a UI change.

### 4. Document, send, sign, return/reject and watermark

- Free `DocumentClient` already exposes `SendDocumentAsync`, `RevokeDocumentAsync`, `StartWorkflowAsync`, `DecideAsync`, `ResubmitWorkflowAsync`, `SignAsync` and `GetSigningReportAsync`.
- `DocumentManagement` already has a send modal and workflow action; `DocumentDetail` has assignment and workflow action but lacks the same send modal.
- `DocumentSigning` already calls `SignAsync` before approval and has return/reject buttons, comment field, signature selection and report summary.
- Free PDF preview currently reads raw file bytes through `/api/documents/{id}/files/{fileId}/content` and creates a browser object URL.
- Licensed behavior prefers workflow output PDF and asks a server application service for watermarked bytes. Free Document service has no corresponding watermarked-PDF contract or endpoint.
- Signing is intentionally adapter-based in free code; the implementation must preserve this boundary and must not copy licensed `BnnSoftSigns` or proprietary SDK code.

## Risk and dependency notes

| Risk | Evidence | Mitigation |
|---|---|---|
| UI/API contract drift | Free and licensed DTOs differ materially | Patch free contracts and service tests together; do not copy Razor code wholesale. |
| Multiple roles ambiguity | Free model has singular `RoleId` | Confirm semantics before changing persistence; otherwise keep role mode single-select. |
| Avatar data unavailable | No free profile-picture endpoint found | Add protected endpoint/storage projection or explicitly use initials fallback; never fabricate a public URL. |
| Watermark/signing licensing | Licensed helper uses separate document viewer/signing code | Keep approved adapter/server stamping seam; document any product/legal approval. |
| Search query performance | Lowering both sides may bypass indexes | Prefer PostgreSQL `ILIKE` or functional indexes; cap page size and test query plans if volume grows. |
| Stale blob URLs | Modals create object URLs | Revoke on close/dispose and reload after mutation. |

## Scout conclusion

The acceptance goal is achievable, but “port all logic” must mean port the observable behavior through Community contracts, not copy implementation internals. Phases 1–3 are implementable from the current code. Phase 4 has two explicit decisions/gaps: approved watermarked PDF implementation and avatar source; role multiplicity is a third contract decision.

## Unresolved questions

- Confirm whether multiple roles per workflow step are required.
- Confirm approved signing/watermark provider or whether initials/raw-preview fallback is acceptable for the first free release.
- Confirm where free user profile pictures are authoritative, if true avatar parity is mandatory.

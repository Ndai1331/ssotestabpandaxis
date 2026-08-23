# Phase 4 — Document send, view, signing and decision parity

## Context

The free client already has the main calls, but the licensed reference has richer action parity and watermarked workflow-aware PDF behavior. The free service boundary must be extended only where the behavior is authorized and license-safe.

## Overview

- Priority: P1
- Status: completed
- Estimate: 5–10 days after the provider/watermark decision
- Goal: make the end-to-end document workflow usable and predictable from free UI.

## Requirements

1. Create document: validate required number/title, preserve source type, upload supported file and refresh the effective document state.
2. Send document: use the same user picker/modal from list and detail, validate receiver, show busy state, close/reload on success and keep modal open with an error on failure.
3. Submit workflow: show selected document/workflow, template option, step type/SLA and signer candidates; prevent duplicate submit using busy/idempotency state.
4. View document: select the workflow-preferred PDF when available, otherwise the best PDF file; use server-authorized content and revoke object URLs on close/dispose.
5. Sign/approve: select default signature/file, call the signing adapter exactly once per idempotency key, then decide approval; if signing fails, do not approve.
6. Return/reject: comments are trimmed, return is available only when the current step allows it, and the UI reloads the queue after a successful decision.
7. Watermark: add a Community-owned server endpoint/application contract for watermarked PDF bytes, with action (`view`/`download`), current-user/time audit context and object-level authorization. Do not port commercial helper code without approval.
8. Error UX: distinguish validation, forbidden, not-found, provider failure and transient BFF errors using existing `BffErrorKind`/notification patterns.

## Related code files

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/DocumentManagement.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/DocumentDetail.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/DocumentSigning.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Components/SubmitWorkflowModal.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Components/DocumentPdfPreviewModal.razor`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Documents/DocumentClient.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Documents/DocumentModels.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Controllers/DocumentsController.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Controllers/SigningController.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Controllers/WorkflowsController.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Documents/DocumentAppService.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Signing/SigningAppService.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Workflows/WorkflowAppService.cs`
- Existing PDF conversion/blob storage services and relevant BFF proxy route/configuration.

## Architecture

Keep the flow:

```text
Blazor Client → HCS.Bff HttpClient → WebGateway/BFF → Document Service
                                         ├─ object-level authorization
                                         ├─ signing adapter / idempotency
                                         └─ watermarked PDF bytes
```

The browser receives bytes only after BFF authorization. It must not construct storage paths, call the service host directly, or hold access tokens.

## Implementation steps

1. Build an action matrix for source type/status/permission and reuse it in list/detail/signing pages.
2. Align send modal and reload/busy/error behavior across `DocumentManagement` and `DocumentDetail`.
3. Audit `SubmitWorkflowModal` candidate selection, template/file attach, required steps and duplicate-click behavior.
4. Add workflow-preferred PDF selection to free document models/service response if it is not already represented.
5. Add the approved watermark contract/service implementation and tests for authorization, view/download action, identity/time text and invalid file input.
6. Route preview/signing/download through the new contract where applicable; revoke all object URLs.
7. Add integration tests for create/send/submit/sign/approve/return/reject and provider failure/duplicate requests.

## Todo

- [x] Define action/status/permission matrix.
- [x] Align create/send/detail/list UX.
- [x] Harden workflow submit and idempotency UX.
- [x] Implement authorized watermarked PDF endpoint with the approved Community package.
- [x] Harden sign-before-approve and decision states.
- [x] Run relevant service/client verification suites.

## Success criteria

An authorized user completes the requested journey from a fresh draft to a signed/decided workflow without raw file URLs or tokens. Unauthorized users cannot infer or mutate documents. Failed signing never transitions the workflow to approved, and retrying the same idempotency key is safe.

## Completion notes

Document detail now has permission-aware send UX, previews use authorized bytes, PDF watermarking runs server-side through `DocumentPdfWatermarkService`, and signing/decision buttons are guarded against duplicate actions. The existing free signing adapter and idempotency contract were preserved.

## Risks and security

- Watermarking must run server-side against authorized bytes and must not trust an arbitrary document/file ID without object-level checks.
- Do not use user-supplied file names or comments as unencoded HTML.
- Keep signing credentials protected at rest and never echo the secret in DTOs/logs.
- Revoke object URLs on modal close/disconnect to avoid client memory leaks.

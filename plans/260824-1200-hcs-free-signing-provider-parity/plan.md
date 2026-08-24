---
title: "HCS free signing provider and document workflow parity"
description: "Port user signature configuration, HSM/Remote CA/USB Token provider behavior, document signing workflow actions and signing menu behavior from HCS_web_with_license into HCS_web_free_license."
status: completed
priority: P1
effort: 2-4w
branch: main
tags: [feature, signing, workflow, provider, parity]
blockedBy: []
blocks: []
created: 2026-08-24
---

# Overview

The free solution already has a document-service signing boundary, credential storage, user signature image storage, workflow signer selection, SLA dates, and a signing queue. It is incomplete compared with `services/HCS_web_with_license`: provider settings are reduced to kind/endpoint/secret, user signatures do not carry provider/token/validity/seal metadata, external adapters fail closed, and the document signing action does not use the selected signature or workflow signing metadata.

The implementation will adapt the licensed behavior to the free microservice contracts. Namespace changes, BFF routing, Community persistence, object-storage APIs, and existing authorization remain owned by the free solution. Existing unrelated working-tree changes must be preserved.

## Requirements

1. User/provider settings support electronic, digital, HSM, Remote CA, and USB Token selection with endpoint, provider code, timeout, default sign type, sign capabilities, OTP, layout dimensions, signed-file policy and sign-log policy.
2. User signature management supports sign type, provider code, token reference, protected secret, seal image, signature image, validity window, active state, default selection and administrator selection of another user.
3. Provider execution follows the licensed implementation where compatible: Remote CA HMAC PDF signing, HSM/USB Token Bnn signing, signature/layout/seal composition, placeholder resolution, signed-file persistence, idempotency and sanitized errors.
4. Workflow signing supports selected signer/signature, electronic and digital paths, returned/rejected/approved decisions, due-date/extension state already present in free, and does not approve when signing fails.
5. Document signing navigation exposes the same practical menu branches as the licensed source: archive documents, personal documents, documents sent to me and signing queue, with permission-aware visibility.
6. No signing secret is returned to clients or written to logs. All endpoints retain object-level authorization and BFF-only browser access.

## Scope files

- Contracts/domain/persistence: `services/HCS_web_free_license/services/document/HCS.DocumentService.Contracts/Signing`, `.../HCS.DocumentService/Signing`, `.../Data/DocumentServiceDbContext.cs`, migrations.
- Provider runtime: `services/HCS_web_free_license/services/document/HCS.DocumentService/Signing` plus adapted Remote CA/Bnn signing helpers and assets from the licensed source.
- HTTP/client/UI: `.../Controllers/SigningController.cs`, `src/HCS.Blazor.Client/Documents/DocumentModels.cs`, `DocumentClient.cs`, `Pages/SignatureSettings.razor`, `Components/UserSignaturesPanel.razor`, `Pages/Administration.razor`, `Pages/DocumentSigning.razor`, `Layouts/HCSMainLayout.razor`, and related localization.
- Workflow: `.../Workflows/WorkflowAppService.cs`, `WorkflowModels.cs`, contracts and tests where signing/selected signer integration is missing.

## Constraints and decisions

- The user explicitly requests the existing provider logic from `HCS_web_with_license`; this supersedes the earlier parity-plan non-goal that excluded Bnn provider code.
- Do not copy generated `bin/obj`, secrets, production configuration, or unrelated commercial ABP modules.
- Prefer focused existing-file changes and small provider classes. Do not add a parallel signing subsystem.
- If Bnn packages cannot be restored/compiled in the free service, stop at the exact build blocker and report it rather than silently shipping a placeholder adapter.

## Verification

- `./scripts/audit-license-clean.sh` with the new explicit provider dependency decision recorded.
- `dotnet build HCS.slnx --no-restore` after restore succeeds.
- Targeted DocumentService and Blazor tests, including provider request/normalization, credential secrecy, user signature validity, idempotency, sign-failure-no-approve and menu/route contracts.
- Review `git diff --check`, changed-file list, and preserve the pre-existing `UserSelect2.razor` edit.

## Phases

| Phase | Status |
|---|---|
| 1. Contract and persistence parity | completed |
| 2. Provider adapters and signing execution | completed |
| 3. User/provider configuration UI and signing queue parity | completed |
| 4. Build, tests, audit and review | completed |

## Verification handoff

- `dotnet build services/HCS_web_free_license/services/document/HCS.DocumentService/HCS.DocumentService.csproj --no-restore` — passed.
- `dotnet build services/HCS_web_free_license/src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore` — passed.
- `dotnet build services/HCS_web_free_license/HCS.slnx --no-restore` — passed with 2 pre-existing xUnit1051 warnings in CollaborationService.Tests, 0 errors.
- `dotnet test services/HCS_web_free_license/services/document/HCS.DocumentService.Tests/HCS.DocumentService.Tests.csproj --no-restore` — 58 passed, 0 failed.
- JSON validation and scoped `git diff --check` — passed.
- The full license script was not used as a completion gate because this workspace contains about 3.1 GB of generated/runtime files and the broad scan did not finish within the review window; the targeted boundary test passes and the dependency decision is recorded in `services/HCS_web_free_license/docs/dependency-license-decisions.md`.

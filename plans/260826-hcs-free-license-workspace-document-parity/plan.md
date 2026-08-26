---
title: "HCS free-license workspace and document UI parity"
description: "Port the usable workspace, workflow-information, approval/signing and quick-preview UX from the licensed reference into the free HCS runtime using only free contracts and typed BFF clients."
status: in_progress
priority: P1
effort: 6-8d
branch: main
tags: [hcs, free-license, blazor, workspace, workflow, signing, bff, parity]
blockedBy: [260808-abp-blazor-wasm-workspace-login, 260810-0900-hcs-community-feature-parity]
blocks: [260826-hcs-motion-system]
created: 2026-08-26
updated: 2026-08-26
createdBy: ck:plan
---

# HCS free-license workspace and document UI parity

## Outcome

`services/HCS_web_free_license` có UX thực dụng tương đương cho:

- Workspace: quick project create/view, quick task create/view, quick file/PDF preview.
- Các danh sách văn bản/quy trình có icon xem nhanh; menu `Văn bản` hiện có được kiểm tra và wire đúng nếu thiếu.
- Workspace project create/view hiện dùng inline project modal; task create/view vẫn dùng task modal hiện có.
- Preview luôn giữ đúng document/file ID từ row/action được chọn, không tự chọn file đầu tiên hoặc file khác.
- WorkflowInfo: timeline bước, trạng thái hiện tại, document-level history, task comment và mở preview đúng file.
- Approval/sign: modal bước ký, signer, sign/approve/comment, return/extend/resubmit theo đúng free contract.

`services/HCS_web_with_license` chỉ là behavioral reference read-only. Không có `ProjectReference`, `PackageReference`, assembly load, reflection, namespace/runtime dependency, HTTP call hay binary dependency tới licensed tree/`HC.*` licensed assemblies. Logic phải được diễn đạt lại trên `HCS.*` free contracts.

## Locked architecture and YAGNI boundary

1. Browser Blazor → typed free client (`WorkManagementClient`, `DocumentClient`, `CollaborationClient`) → WebGateway BFF → free service API. Không gọi trực tiếp service từ browser.
2. Reuse existing free routes first: `/api/projects`, `/api/project-tasks`, `/api/documents`, `/api/workflows`, `/api/documents/*/files/*/watermarked-content`.
3. Chỉ mở rộng free contract khi dữ liệu đã tồn tại/ghi được thật trong free domain. Không dựng mock, không đọc DB service khác, không port licensed application service.
4. `ApprovalTask.Comment` đã có trong free domain/persistence nhưng thiếu ở DTO mapping; đây là additive contract gap có thể implement thật.
5. Free chưa có workflow-specific logs, workflow attachments hoặc signer-reassign API. Không tạo UI giả cho các capability này; chỉ trình bày document-level history/files và persisted task comments với đúng phạm vi dữ liệu.
6. `resubmit`, `decide`, comment decision, return, extend due date và signing attempt đã có free endpoint/contract; chỉ đưa vào release sau khi test API + failure path.

## Explicit data flows

```text
Workspace
  Browser → Workspace.razor (inline project modal)
    → WorkManagementClient
    → BFF /api/projects + /api/project-tasks
    → ProjectDetailDto/TaskDetailDto
    → existing create/view modals or canonical routes

Quick document/PDF preview
  list row/icon + exact file ID → DocumentPdfPreviewModal
    → DocumentClient → BFF /api/documents/{documentId}/files/{fileId}/watermarked-content
    → free DocumentService authorization + bytes/content type
    → PDF frame/download fallback; no signed-token leakage

WorkflowInfo
  queue/detail row → WorkflowInfoModal
    → free DocumentClient: DocumentDto + WorkflowInstanceDto + WorkflowDefinitionDto
    → document-level history/files + mapped ApprovalTask.Comment + actor lookup where available
    → step timeline, status, actor, comments, file preview

Approval/sign
  signing queue → current free approval/sign modal in DocumentSigning.razor
    → selected task/step/signer + free signature list
    → free sign endpoint (if selected signing path) → successful attempt only
    → free decision endpoint with comment/idempotency key
    → refreshed WorkflowInstanceDto/queue; sign failure never approves
```

## Baseline and dependency graph

| Dependency | Evidence / gate |
|---|---|
| Workspace/auth baseline | `Workspace.razor` currently navigates; existing BFF auth state and project plan must remain usable for protected deep links. |
| Work free APIs | `WorkManagementClient` already exposes project/task list/detail/create/update; `ProjectDetailDto` and `ProjectTaskDetailDto` exist. |
| Document free APIs | `DocumentClient` already exposes document/files/watermarked content and workflow instances/decisions/resubmit. |
| Workflow data | `WorkflowInfoModal` now renders the ordered definition/tasks, document-level history/files and mapped task comments; workflow-specific logs/attachments remain unsupported. |
| Approval DTO | Domain has `ApprovalTask.Comment`; `ApprovalTaskDto` currently omits it. Additive mapping is allowed; no schema migration expected. |
| Gateway | Existing YARP routes cover `/api/documents`, `/api/workflows`, `/api/projects`, `/api/project-tasks`; change only if contract test proves a missing route. |

Order: `phase-01` → `phase-02` → `phase-03` → `phase-04`.

- Phase 01 establishes reusable quick-action/preview wiring and list icon contract.
- Phase 02 consumes Phase 01 preview surface and adds truthful workflow display data.
- Phase 03 consumes Phase 02 comment/actor model and completes approval/sign UX.
- Phase 04 is the build, test, boundary audit and review gate for all prior phases.

## File ownership (exclusive by phase)

| Phase | Owned paths; no parallel phase edits these files |
|---|---|
| 01 | `src/HCS.Blazor.Client/Pages/Workspace.razor`; `Pages/Projects.razor`; `Pages/ProjectDetail.razor`; `Pages/ProjectTasks.razor`; `Pages/DocumentManagement.razor`; `Pages/DocumentDetail.razor`; `Pages/DocumentSigning.razor` list/action surface only; `Pages/WorkflowInstances.razor`; `Components/ProjectTaskCreateModal.razor`; `Components/ProjectTaskViewModal.razor`; `Components/Documents/DocumentPdfPreviewModal.razor`; `Documents/DocumentClient.cs`; `Work/WorkManagementClient.cs`; related client models only if additive fields are proven necessary; `Navigation/HCSMenuContributor.cs` and `Layouts/HCSMainLayout.razor` only for missing Văn bản menu wiring. |
| 02 | `Components/Documents/WorkflowInfoModal.razor`; `services/document/HCS.DocumentService.Contracts/Workflows/WorkflowContracts.cs`; `services/document/HCS.DocumentService/Workflows/WorkflowAppService.cs`; `src/HCS.Blazor.Client/Documents/DocumentModels.cs`; `src/HCS.Blazor.Client/Documents/DocumentClient.cs` workflow mapping/client methods; actor lookup calls use existing free `CollaborationClient` without changing its files. |
| 03 | `Pages/DocumentSigning.razor` approval/sign modal region and its existing state/flow; `Pages/WorkflowInstances.razor` decision entry only if required for the same canonical flow; no new parallel signing subsystem; no licensed files. |
| 04 | Existing test files under `services/document/HCS.DocumentService.Tests/**`, `services/work-management/HCS.WorkManagementService.Tests/**`, `gateways/web/HCS.WebGateway/HCS.WebGateway.Tests/**`, and client/test harness files required for this slice; plan reports only. Phase 04 reads all implementation paths but owns test/review changes. |

If an implementation needs a file outside its row, stop and reassign ownership before editing. Existing unrelated worktree changes, especially free-service `wwwroot/appsettings*`, must be preserved.

## Phase summary

| Phase | Deliverable | Estimate | Main risk |
|---|---|---:|---|
| 01 | Workspace quick actions, inline project modal, exact-file preview, list preview icons, Văn bản menu verification | 1.5-2d | UI advertises action while API returns 401/403 or stale IDs |
| 02 | Truthful WorkflowInfo timeline/detail/document history/task-comment/exact-file preview UX | 1.5-2d | Licensed workflow logs/attachments are unavailable in free |
| 03 | Approval/sign modal flow using existing free sign/decision/resubmit contracts | 2-2.5d | Signing succeeds/fails out of sequence with approval |
| 04 | Build, tests, license boundary, review and handoff | 1-1.5d | Broad solution build/environment failure hides slice regressions |

## Backwards compatibility and migration

- No licensed data migration and no dependency on the licensed database.
- Existing project/task/document/workflow routes and JSON shapes remain compatible.
- Add `Comment` to free `ApprovalTaskDto` as nullable/additive; old clients ignore it, and old persisted tasks remain valid.
- Do not rename existing enum values, routes, idempotency fields or document file endpoints.
- Existing workflows with no comment/history/file continue to render explicit empty states.
- New UI remains permission-gated by existing free policies. Unauthorized API calls stay `401/403`; UI does not convert them into fake empty data.

## Rollback strategy

- Phase 01: hide quick-action buttons/icons behind the existing route/permission gate or revert only owned UI/client changes; existing list/detail routes remain usable.
- Phase 02: revert modal enrichment first; nullable `Comment` mapping is safe to leave, or revert additive contract + mapping without data loss because no migration is introduced.
- Phase 03: disable the new approval/sign action path and retain existing decision endpoint; never roll back by deleting workflow/signing data. If a migration unexpectedly becomes necessary, use a forward compensating migration, not destructive rollback in a shared DB.
- Phase 04: publish only after all gates pass; if build/test fails, do not enable menu/actions. Revert the slice commit(s) by phase boundary.

## Cross-phase acceptance criteria

- `Workspace` visibly loads project/task quick data, handles empty/loading/error/403 states, and creates/views projects through the inline project modal against real free endpoints.
- A user can open the exact selected PDF from Workspace/document/workflow/signing list via the same free watermarked-content path; unsupported/missing files show recoverable error state.
- Every intended document/workflow list has a labeled preview icon with keyboard/tooltip semantics; no duplicate or dead action remains.
- WorkflowInfo shows ordered steps, current step, status, document-level history, persisted task comment, and preview action for the exact selected file; unavailable workflow-specific logs/attachments are not claimed as implemented.
- Approval/sign shows selected step and signer, supports comment + approve/reject/return/extend/resubmit only where free contracts authorize it, and never calls decision after a failed sign attempt.
- `./scripts/audit-license-clean.sh` passes for the changed free tree; no changed free project references `HC.*` licensed assemblies or `HCS_web_with_license` source paths.
- Build, targeted tests, full relevant tests, and browser acceptance pass per phase-04.

## Unresolved questions

1. Current slice accepts document-level history/files and persisted task comments for WorkflowInfo. Workflow-specific logs/attachments remain deferred until real free contracts are approved and implemented.
2. Which free roles may see quick create/preview and execute signing? Use current policies by default; do not broaden permissions silently.
3. Is signer reassignment required for MVP? No free endpoint exists, so it is explicitly out of this plan unless a real free contract is supplied.

## Implementation status (2026-08-26)

- Phase 01 is implemented: Workspace uses an inline project modal, task quick actions remain modal-based, and file preview carries the exact selected file identity through the free BFF path.
- Phase 02 is implemented: WorkflowInfo exposes document-level history and persisted task comments, with exact-file preview reuse.
- Phase 03 approval/sign behavior and Phase 04 build/test/review evidence remain separate gates; keep them pending until their acceptance evidence is recorded.
- There is no UI or API claim for workflow-specific logs, workflow attachments, or signer reassignment.

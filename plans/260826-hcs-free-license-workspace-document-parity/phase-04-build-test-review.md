---
title: "Phase 04 — Build, test, license audit and review"
description: "Validate the complete free-license parity slice across contracts, BFF routes, UI behavior, security and licensed-boundary rules."
status: pending
priority: P1
effort: 1-1.5d
branch: main
tags: [hcs, testing, e2e, security, license-boundary, review]
created: 2026-08-26
---

# Phase 04 — Build, test, license audit and review

## Depends on

- Phases 01–03 complete and their acceptance checklists green.
- Free service dependencies/restored assets available; local Docker/BFF/Keycloak runtime for browser checks.

## Test matrix

| Layer | Coverage |
|---|---|
| Unit/domain | DTO backward-compatible JSON, `ApprovalTask.Comment` mapping, timeline status/current-step join, stale/null/overdue cases, sign/decision sequencing helpers. |
| Client contract | `WorkManagementClient` and `DocumentClient` URI/query/idempotency construction; watermarked PDF vs non-PDF selection; no direct service URL. |
| Service integration | Work project/task create/detail authorization; document file authorization; workflow decision/comment/resubmit; sign failure leaves task pending; migration snapshot unchanged unless explicitly justified. |
| Gateway integration | YARP routes for `/api/projects`, `/api/project-tasks`, `/api/documents`, `/api/workflows`; authenticated propagation; anonymous 401 and forbidden 403; no redirect for API calls. |
| UI/component | Workspace loading/empty/error/403; task/project actions; preview modal; every list icon; WorkflowInfo states/comments/files; approval modal state transitions and disabled pending buttons. |
| Browser E2E | Real local Keycloak/BFF: login, deep link, Workspace quick create/view, PDF preview, document list preview, WorkflowInfo, approve/reject/return/comment, sign failure/no approve, stale task, logout/refresh. |
| Boundary/security | `audit-license-clean.sh`; changed free source/project files contain no `HC.*` assembly reference, licensed project reference/path, token/secret logging, or open direct-service browser endpoint. |

## Verification commands

Run from `services/HCS_web_free_license` after dependencies are available:

```bash
./scripts/audit-license-clean.sh
dotnet restore HCS.slnx --configfile NuGet.Config
dotnet build HCS.slnx --no-restore
dotnet test HCS.slnx --no-build
dotnet test services/document/HCS.DocumentService.Tests/HCS.DocumentService.Tests.csproj --no-restore
dotnet test services/work-management/HCS.WorkManagementService.Tests/HCS.WorkManagementService.Tests.csproj --no-restore
dotnet test gateways/web/HCS.WebGateway/HCS.WebGateway.Tests/HCS.WebGateway.Tests.csproj --no-restore
```

Also run `git diff --check`, inspect changed-file ownership, and verify no generated `bin/obj` or secrets entered the change. If full solution build is blocked by the known host-specific Blazor WASM issue, record the exact error and use the authoritative supported CI/host check; do not mask a slice failure.

## Review gates

- Contract review: additive DTO only; no undocumented endpoint or schema change.
- Security review: policy-backed UI is not the sole authorization; API negative tests pass; no secret/token exposure.
- License review: no `HC.*` licensed assembly reference; free namespace/project graph remains clean.
- UX review: keyboard-accessible icons, visible loading/empty/error states, modal close/retry behavior, hard refresh/deep-link behavior.
- Scope review: no workflow logs/attachments/reassign controls without real free contracts; no copied commercial module code/assets/binaries.

## Measurable release gate

- 0 build errors, 0 targeted test failures, 0 critical review findings.
- All acceptance scenarios in the matrix have recorded pass evidence or a named environment blocker.
- License audit passes and changed-file boundary scan returns no licensed dependency.
- No menu/action is enabled unless its real free API, policy, UI and negative test are present.

## Rollback and risk

Risk: Medium likelihood / High impact from environment/build noise. Mitigate with targeted commands first, then solution build, and separate infrastructure failures from code failures. Rollback is phase-granular: do not publish/enable any phase with a failed gate; revert only the owned slice while preserving unrelated worktree changes.

## Handoff output

Record test commands/results, known environment blockers, changed file list, deferred contract gaps and the next safe implementation command. Update only plan status/report metadata; do not commit or push unless separately requested.


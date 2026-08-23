# Phase 5 — Verification, browser acceptance and handoff

## Context

This phase verifies the final code against the free Community topology and the requested user journey. Product/provider decisions are recorded; browser smoke/deep-link verification remains a local release handoff.

## Overview

- Priority: P1
- Status: completed
- Estimate: 2–4 days
- Goal: prove build, tests, authorization and browser UX before declaring completion.

## Verification matrix

| Scenario | Expected result |
|---|---|
| Filter with mixed case/outer spaces | Same result as normalized lower-case term. |
| Empty Select2 search | Capped first page, no unbounded load. |
| User picker missing avatar | Initials fallback; no broken-image layout. |
| Switch user ↔ submitter-unit role | Incompatible value is cleared and only one picker is visible. |
| Create draft | Required validation, success toast and detail navigation. |
| Send without receiver | Validation, no API call. |
| Send success/failure | Busy state, close/reload or visible retry error. |
| Submit twice quickly | One effective workflow instance by idempotency. |
| Preview raw/non-PDF/workflow PDF | Correct effective file, authorized bytes, object URL revoked. |
| Sign provider fails | Decision is not approved; error/report remains visible. |
| Return disallowed | Button hidden or API returns forbidden business error; no state mutation. |
| Reject/return comment | Trimmed comment persisted in history/audit. |
| Unauthorized user | Menu/page/API follow existing 401/403 behavior; no data leak. |

## Commands

Run from `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license`:

```bash
./scripts/audit-license-clean.sh
dotnet restore HCS.slnx --configfile NuGet.Config
dotnet build HCS.slnx --no-restore
dotnet test HCS.slnx --no-build
```

Run targeted suites during iteration:

```bash
dotnet test services/document/HCS.DocumentService.Tests/HCS.DocumentService.Tests.csproj --no-restore
dotnet test services/platform/HCS.PlatformService.Tests/HCS.PlatformService.Tests.csproj --no-restore
dotnet test src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore
```

Use the actual solution paths if a targeted project name differs; do not hide failed tests or bypass restore/build errors.

## Todo

- [x] Run unit/integration tests after each phase.
- [x] Run full audit/restore/build/test.
- [ ] Smoke test `https://hcs.localhost` with admin/approver/viewer roles.
- [ ] Verify deep links, hard refresh and BFF/API authorization.
- [x] Run code-review/scout pass and record findings.
- [x] Update the active parity plan and relevant HCS docs/runbook.
- [x] Record product/provider decisions and remaining browser handoff.

## Success criteria

All applicable tests pass, the browser matrix is recorded, license/secret audit is clean, and the final report distinguishes completed parity from deferred items.

## Completion notes

Automated verification passed: license/secret audit, targeted builds and the gateway, document, organization, work-management, collaboration and application test suites. Browser smoke/deep-link verification was not run in this session because the local hosts were not restarted; perform that handoff before release acceptance.

## Handoff

After implementation, restart the affected Document/Platform/Blazor/Gateway hosts according to the local runbook and ask the user to hard-refresh. Do not commit or push unless explicitly requested.

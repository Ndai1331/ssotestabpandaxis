# Phase 3 — Test, review, and validate

## Overview

Validate route contracts, DTO behavior, client URI bounds, compile output, license cleanliness, and the dirty-worktree safety boundary.

## Related Code Files

Create or modify only as needed:

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/test/HCS.Application.Tests/OrganizationUnitManagementContractTests.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/test/HCS.Application.Tests/PlatformRouteContractTests.cs`

## Implementation Steps

1. Add reflection/contract tests for endpoint routes, HTTP verbs, permission, and query binding.
2. Add pure tests for flat-to-tree hierarchy and invalid target filtering if the model is extracted for testability.
3. Build contracts, application, HttpApi, Platform host, and Blazor client with `--no-restore` where assets are present.
4. Run targeted test projects, then the free-license audit script.
5. Inspect `git diff` and `git status` to ensure unrelated pre-existing changes were not reverted or reformatted.

## Success Criteria

- All targeted tests pass.
- All affected projects build successfully.
- `scripts/audit-license-clean.sh` passes.
- Plan status and phase checklists are synchronized; no commit is created unless requested.

## Todo List

- [x] Add route, client URL, and tree-builder contract tests.
- [x] Run Application tests: 7 passed.
- [x] Run Gateway tests: 141 passed; one pre-existing xUnit2031 warning remains.
- [x] Build affected projects and validate Compose runtime route (302/401 expected for unauthenticated requests).
- [x] Run bounded license/secret scan on new OU files; document the repository audit script hang.

## Risk Assessment

- Existing dirty changes may cause unrelated build/test failures; report exact failures separately.
- Browser visual verification may require the local services and authenticated session; if unavailable, report build/API evidence and the manual URL.

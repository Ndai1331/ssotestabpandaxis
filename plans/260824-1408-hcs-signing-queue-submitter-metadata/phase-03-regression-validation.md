# Phase 03 — Regression validation

Status: `pending`  
Priority: `P1`  
Depends on: Phase 02 implemented  
Owner: Existing test projects + local browser smoke

## Test matrix

| Layer | Cases | Location / evidence |
|---|---|---|
| Unit | `Created` actor wins over `FromUserId`; legacy fallback; no IDs; duplicate IDs; display/username/contact/`—` fallback | Existing `HCS.WebGateway.Tests` (client has `InternalsVisibleTo`); pure helper, no new bUnit project |
| Client contract | Repeated `userId` query encoding, max 100/chunk behavior, JSON candidate mapping, 403/error non-fatal path | Existing `BusinessClientContractTests.cs` or adjacent existing client contract test |
| API/security | signing permission accepted; missing permission 403; inactive/missing IDs omitted; empty/101 IDs bounded; exact-user endpoint auth unchanged | Authenticated local HTTP contract/manual test; no Platform test project currently exists, avoid creating one for this narrow fix |
| Integration | Queue load enriches rows, department lookup is independent, one batch call, no N+1, failed enrichment preserves rows | Local service logs/HTTP trace plus test account |
| E2E/browser | submitter outside first 50, current user, submitter OU != recipient OU, legacy row, Chat/department lookup failure; reload, both filters, CSV | `/document-signing` local HCS browser smoke; capture before/after screenshot and exported CSV |
| Build/gate | targeted projects compile/tests pass; no unintended dirty files | `dotnet build/test` per project, `git diff --check`, status/diff ownership review |

## Commands / verification

Run only after implementation and with the baseline `git status --short` saved. Target at least `HCS.DocumentService.Tests`, `HCS.WebGateway.Tests`, the Blazor client project, and the Platform/Document hosts affected by the controller/client contract. Compare post-test status to baseline so generated files or user changes are not mistaken for implementation output.

## Risk controls

- Do not use `git clean`, `git reset`, `git checkout`, broad formatter, or staging commands.
- If a test fails in an unrelated dirty file, report baseline-vs-post diff and isolate; do not “fix” unrelated worktree changes.
- Verify no bearer token, full identity list, or raw GUID is rendered/logged.
- If the department permission check fails, mark the E2E case blocked and resolve authorization as a separate smallest-scope prerequisite; do not grant admin permissions as a test workaround.

## Measurable exit criteria

All unit/client contract tests pass; API authorization and bounds are observed; E2E shows correct submitter and department for the four fixture classes; filter and CSV agree with rendered rows; no N+1; only ownership-matrix files differ beyond the pre-existing baseline. Any failed case, unverified permission assumption, or changed API semantics remains an explicit unresolved item.

## Rollback

Revert test-only changes independently if they expose a pre-existing failure. For product rollback, remove Phase 02 client enrichment first, then Phase 01 route; existing queue remains available because no stored data changed.

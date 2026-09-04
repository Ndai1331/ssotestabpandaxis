# Phase 03 — Tests, verification and docs

## Context links

- Status: Complete

- `services/collaboration/HCS.CollaborationService.Tests/ApiContractTests.cs`
- `services/collaboration/HCS.CollaborationService.Tests/DomainBehaviorTests.cs`
- `services/collaboration/HCS.CollaborationService.Tests/SecurityDurabilityTests.cs`
- `README.md`
- `docs/`

## Test scope

- Contract tests for routes, permission policy, DTO limits and media registration.
- Domain tests for visibility, non-empty post shape, reply parent preservation and media binding.
- Security tests for owner-only internal reads, public feed filtering, unauthorized media and invalid parent comments.
- Client contract tests for BFF URLs, query clamping and multipart upload behavior.
- Run the Collaboration tests, solution build, then the full repository checks if dependencies are available.

## Smoke verification

1. Start local infrastructure and HCS stack; sign in at `https://hcs.localhost`.
2. Open `/social`, create text, image and video public posts; verify newest-first order.
3. Add a top-level comment and reply; refresh and verify persisted hierarchy.
4. Create an internal post; verify it is absent from another user's feed and present at `/social/profile`.
5. Verify invalid/oversized media gets a friendly error and media download still follows authorization.
6. Test mobile width and keyboard focus for composer, comments, replies and media controls.

## Documentation

- Add the feature to the relevant README section and a concise `docs/runbooks/hcs-social.md` smoke runbook if the implementation is complete.
- Update the roadmap/changelog only if those canonical files exist in this service; do not rewrite unrelated documentation.

## Definition of done

- All targeted tests and build pass, or any external-infrastructure-only limitation is reported explicitly.
- Code review finds no critical authorization, data exposure or duplicate-submit issue.
- Plan checkboxes/status and docs match the files actually changed.

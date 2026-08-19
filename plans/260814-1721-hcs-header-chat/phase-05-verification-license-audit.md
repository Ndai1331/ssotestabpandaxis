# Phase 5 — Verification, runtime smoke and license audit

## Context links

- License audit: `services/HCS_web_free_license/scripts/audit-license-clean.sh`
- Solution: `services/HCS_web_free_license/HCS.slnx`
- Runtime compose: `services/HCS_web_free_license/docker-compose.yml`
- Existing gateway tests: `services/HCS_web_free_license/gateways/web/HCS.WebGateway.Tests/`
- Existing Collaboration tests: `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService.Tests/`

## Overview

Priority: P1. Status: in progress. Prove auth, permission, UX, runtime dependency and license boundaries before calling the increment complete.

## Test matrix

| Scenario | Expected result |
|---|---|
| Anonymous opens `/chat` | BFF challenge/NotAuthorized; no chat data or composer. |
| Authenticated user without `Collaboration.Chat` | Chat menu/action hidden or forbidden; API/hub returns 403. |
| Authorized user fresh login | `/bff/user` contains the exact allowed permission; chat list loads. |
| Logout from user menu | CSRF-protected POST succeeds; BFF cookie is gone; auth state becomes anonymous. |
| Culture switch + hard reload | Selected supported culture persists and header/chat strings change. |
| Empty contacts/conversations | Localized empty state with create/search guidance, no exception. |
| Conversation/message API failure | Localized error and retry; existing data is not silently discarded. |
| Text send from two sessions | Message persists, recipient sees realtime or REST fallback update, unread/read transitions are correct. |
| Attachment unavailable/invalid | Server rejection is shown safely; text chat remains usable. |
| Mobile widths | List/thread/info transitions work with keyboard and no horizontal overflow. |

## Implementation steps

1. Run focused permission, BFF logout, Collaboration contract/domain/security and gateway route tests.
2. Run `./scripts/audit-license-clean.sh`; verify no `HCS_web_with_license`, commercial package, Pro asset or copied DTO appears in free project references/content.
3. Restore/build/test the free solution using its approved NuGet configuration; record failures without weakening authorization or replacing tests with manual bypasses.
4. Rebuild/recreate only affected local services after code is implemented; check Collaboration, MinIO, outbox, gateway, Blazor and Caddy health.
5. Perform fresh/incognito browser checks at `https://hcs.localhost` for admin and an unprivileged role; hard-refresh after culture/logout changes.
6. Record unresolved backend/API gaps in the plan rather than marking unsupported attachment/contact/realtime behavior complete.

## Success criteria

- Focused tests and license audit pass; solution build/test status is recorded.
- Browser matrix proves cookie-only logout, exact permission behavior, culture persistence and responsive chat states.
- Runtime evidence distinguishes “API exists” from “dependency stack is healthy”.
- No paid source/package/asset is copied and no secret/token is logged or committed.

## Risk assessment

- Existing plans may still show in-progress metadata; report only this plan's evidence and avoid silently rewriting unrelated plan status.
- Full solution tests may require Docker services or generated assets; separate environment blockers from product defects.

## Security considerations

- Redact cookies, tokens, message bodies and personal contact data from logs/screenshots.
- Verify 401/403 semantics directly at gateway/service boundaries.
- Keep attachment downloads membership-guarded and filenames/content types untrusted.

## Next steps

Close or explicitly carry forward any culture/contact/permission blocker, then update this plan's status only after the acceptance matrix is evidenced.

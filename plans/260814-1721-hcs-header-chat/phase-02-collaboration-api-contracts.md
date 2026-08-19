# Phase 2 — Collaboration contracts, permissions and API gaps

## Context links

- Contracts: `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService.Contracts/CollaborationContracts.cs`
- API: `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService/Api/ChatController.cs`
- Application layer: `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService/Application/CollaborationAppService.cs`
- Hub: `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService/Hubs/ChatHub.cs`
- Central permissions: `services/HCS_web_free_license/src/HCS.Application.Contracts/Permissions/{HCSPermissions,HCSPermissionDefinitionProvider}.cs`

## Overview

Priority: P1. Status: implemented, service/runtime verification pending. Establish a license-clean, least-privilege contract before adding contact and composer UI.

## Key insights

- Conversation/message/attachment contracts are already sufficient for most client operations and should be reused rather than recreated in the Blazor project.
- Members contain IDs but no display metadata. A direct/group chat picker cannot be built safely from the admin identity list.
- Collaboration policies require claim values that the central role/permission catalog does not currently define. This is a security prerequisite, not a UI workaround.

## Requirements

- Register or align `Collaboration.Chat` and `Collaboration.Notifications` in the permission source used by local role grants and token emission; preserve exact policy names.
- Ensure admin receives only the intended initial grants and non-admin roles receive none unless explicitly approved. Test fresh sign-in, `/bff/user`, API 401/403 and hub authorization.
- Define a privacy-minimal contact DTO: stable user ID plus display name/username/avatar initials or equivalent, optional availability only if already supported. Do not expose email or admin-only fields by default.
- Support user search and group member selection with bounded query/page sizes and membership/privacy checks. Direct conversation creation must preserve the existing “exactly two distinct users” validation.
- Use existing message/attachment input DTOs. Do not add a client-side parallel model that can drift from the free API.

## Architecture/data flow

Preferred options to compare:

1. Collaboration owns `GET /api/chat/contacts` and calls a least-privilege Platform user projection/service client.
2. Platform owns a protected directory lookup endpoint consumed by the client through the BFF; Collaboration remains the authority for chat membership.

Select one after checking service boundaries, service-account/auth configuration and privacy. In either case: `Blazor typed adapter → BFF → gateway route → owning service → bounded DTO`; the browser never calls Platform/Collaboration directly or holds a token.

## Related code files

Likely modify: `HCS.Application.Contracts/Permissions/HCSPermissions.cs`, `HCSPermissionDefinitionProvider.cs`, localization permission keys, and role/claim tests.

Conditional create/modify: a free-owned contact contract/controller/application service, `CollaborationContracts.cs` or a new contracts file, service-to-service client/configuration, gateway tests/config only if the existing `/api/chat/{**}` route does not cover the chosen path.

Modify tests: Collaboration `ApiContractTests`, `DomainBehaviorTests`/`SecurityDurabilityTests`, AuthServer permission claim tests and gateway BFF/profile tests as appropriate.

Do not modify/copy: any `services/HCS_web_with_license` source, commercial project reference, commercial DTO or Pro package.

## Implementation steps

1. Trace current permission definition → role grant → AuthServer access token → gateway cookie/profile → Collaboration policy path.
2. Add the free permission definitions/localized display names and seed behavior only if the trace proves they are missing; preserve existing grants and require re-login after changes.
3. Decide contact API owner and document its threat model, page limits, fields, and permission behavior.
4. Add typed contract/client adapter and contract tests for successful lookup, empty result, unauthorized access, bounds and no cross-user data leakage.
5. Verify attachment constraints, MinIO availability, message ownership and upload-before-send binding remain enforced server-side.

## Todo

- [ ] Close permission catalog gap.
- [ ] Choose contact endpoint owner.
- [ ] Add typed free client boundary.
- [ ] Test runtime dependencies and attachment limits.

## Success criteria

- A fresh authorized session carries the exact Collaboration permission claim and can call the intended endpoints; a role without it receives 403.
- Contact search/direct/group creation has a documented, privacy-minimal contract.
- All request/response models used by the client are free-owned and typed.

## Risk assessment

- Adding a permission definition without matching token/role persistence can create a UI-visible but API-denied feature; test the complete chain.
- Service-to-service lookup can introduce a new credential or network dependency; prefer an existing authenticated internal pattern and bounded projection.
- Attachment UI can appear functional while MinIO is down; expose capability/error state and keep text chat usable.

## Security considerations

- Never use role name or UI state as an API authorization substitute.
- Do not let a contact endpoint enumerate all identity fields or permit arbitrary member IDs without server validation.
- Keep attachment membership, uploader ownership, content-type/size and message-binding checks on the server.

## Next steps

Once permission and contacts are green, expose only the supported actions to the desktop chat UI.

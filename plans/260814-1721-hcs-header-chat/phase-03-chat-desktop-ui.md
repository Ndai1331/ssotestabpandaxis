# Phase 3 — Desktop chat workspace

## Context links

- Current page: `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/ChatWorkspace.razor`
- Current styles: `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/ChatWorkspace.razor.css`
- Free API/contracts: `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService/{Api,Application,Contracts}`
- Paid behavior reference only: `services/HCS_web_with_license/src/HC.Blazor/Pages/Chat1/`

## Overview

Priority: P1. Status: implemented, browser verification pending. Rebuild the page as free-owned Blazorise components with the screenshot's three-region desktop behavior.

## Key insights

- The current page is a read-only two-column list/thread implementation with local records. Replace ad hoc records with typed free DTO/client mapping.
- The reference has a left contact/conversation list, center thread/composer and right information panel. Its rich actions must be reduced to endpoints actually available in the free service.
- Keep page/component files below the repository's practical size limit by extracting list, thread, composer and info panel components.

## Requirements

- Left panel: search contacts/conversations, user/group contact creation where API supports it, pinned/unread markers, last message/date, load-more or bounded paging, empty/error/retry states.
- Center panel: selected conversation header, avatar/title/type, paged messages, sender/time/reply/deleted/attachment states, loading/empty/error/retry states, mark-read.
- Composer: text validation (1–4000 server limit), send, optional send-on-enter behavior, attachment selection/progress/errors, clear/disable while sending. Hide attachment controls when storage capability is unavailable.
- Right panel: conversation description/type, member list/roles and supported actions (pin, rename, add/remove/leave) based on `ConversationPermissionDto`; do not fabricate paid-only search/task features.
- Permission guard: explicit `Collaboration.Chat` route policy, menu visibility, and server-enforced calls. Unauthorized page must show a localized not-authorized state, not a failing composer.

## Architecture/data flow

`ChatWorkspace → typed CollaborationClient → named HCS.Bff HttpClient → gateway /api/chat → Collaboration API`.

Use an immutable view model only for presentation mapping; retain `ConversationDto`, `PagedMessagesDto`, `ChatMessageDto`, `ConversationPermissionDto` and `UploadAttachmentResult` as transport types. Refresh the selected conversation after mutations and preserve the current search/page where possible.

## Related code files

Modify: `Pages/ChatWorkspace.razor`, `Pages/ChatWorkspace.razor.css`, client module/project reference and localization resources.

Create: `Collaboration/CollaborationClient.cs`, `Pages/Chat/ChatContactPanel.razor`, `ChatThreadPanel.razor`, `ChatComposer.razor`, `ChatInfoPanel.razor`, plus scoped CSS only where shared layout CSS is insufficient.

Conditional: contact picker and attachment preview components based on Phase 2 contracts.

## Implementation steps

1. Add the typed client with centralized status/error mapping and cancellation for search/message paging.
2. Build the left list from server DTOs; use server `type`/`pinnedOnly` filters and bounded search rather than loading all users.
3. Build the thread with newest/older paging, deleted/attachment/reply rendering and mark-read after a valid selection.
4. Build the composer over `POST /api/chat/messages`; upload attachments through the conversation endpoint first, then send attachment IDs only after upload succeeds.
5. Build the info panel from conversation/permission endpoints and hide unsupported controls.
6. Add localized loading, empty, retry, forbidden, send-failure and attachment-failure states.

## Todo

- [ ] Replace local read-only records with typed DTOs.
- [ ] Implement contact/group picker after the API is approved.
- [ ] Implement message send/read/permission-aware info actions.
- [ ] Implement attachment flow only after MinIO smoke test.

## Success criteria

- Desktop matches the intended three-region hierarchy and remains usable with no conversation, no messages, long messages and API failure.
- Message send and attachment flow never bypass server validation and update the view only after a successful response.
- Right-panel controls match the server permission DTO, including disabled/hidden behavior.

## Risk assessment

- A single large Razor component can regress maintainability and mobile behavior; keep panels separate and share only small view models/helpers.
- Attachment previews/downloads may need safe content-disposition handling; do not inject untrusted HTML or filenames into markup.

## Security considerations

- Encode/render message text as text; never treat user message content as markup.
- Do not expose attachment URLs without the BFF/API membership check.
- Keep server permission and membership validation authoritative for every mutation.

## Next steps

Wire realtime events and mobile transitions only after REST list/thread/composer behavior is stable.

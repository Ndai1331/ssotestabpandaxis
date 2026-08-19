# Phase 4 — Realtime, unread state and responsive behavior

## Context links

- Client connection: `services/HCS_web_free_license/src/HCS.Blazor.Client/Collaboration/ChatRealtimeConnection.cs`
- Hub: `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService/Hubs/ChatHub.cs`
- Gateway route: `services/HCS_web_free_license/gateways/web/HCS.WebGateway/appsettings.json`
- Current responsive styles: `Pages/ChatWorkspace.razor.css`, `Layouts/HCSMainLayout.razor.css`

## Overview

Priority: P1. Status: implemented, runtime/browser verification pending. Add resilient realtime updates and a deliberate desktop/mobile state machine.

## Key insights

- The free connection has auto-reconnect and receives user-targeted message/deletion events, but the page does not currently expose connection status or implement full subscription/lifecycle behavior.
- Realtime is an enhancement, not the source of truth. REST reload/retry must recover missed events and work when SignalR or Redis is unavailable.
- The paid reference uses a mobile mode that shows one of list/thread/info at a time. Reproduce that interaction with free-owned CSS/components, not copied markup/assets.

## Requirements

- Connection states: connecting, connected, reconnecting, disconnected/error with localized status and retry/reload action.
- On new message/deletion: update selected thread when safe; otherwise refresh conversation list/unread counts. Avoid duplicate messages using server IDs/client IDs.
- Mark-read when the selected conversation is viewed and after a successful message refresh; do not clear unread state optimistically on API failure.
- Desktop: three columns with bounded scroll regions. Mobile: list → thread → info transitions with back buttons, preserved selection and no horizontal overflow.
- Handle tab visibility/connection loss without unbounded timers, event-handler leaks or repeated full-page reloads.

## Architecture/data flow

`ChatRealtimeConnection event → page-scoped state coordinator → targeted REST refresh → component state update`.

Keep the singleton connection lifecycle owned by the client module, but subscribe/unsubscribe page handlers deterministically. Use the BFF SignalR route and cookie credentials already configured; do not add browser token transport.

## Related code files

Modify: `Collaboration/ChatRealtimeConnection.cs`, `Pages/ChatWorkspace.razor`, panel components and `ChatWorkspace.razor.css`.

Create if needed: a small `ChatWorkspaceState`/coordinator and responsive breakpoint tests or browser-test helpers. Avoid a second realtime abstraction for notifications in this plan.

## Implementation steps

1. Verify hub authorization and `ReceiveMessage`/`MessageDeleted` payload mapping against free DTOs.
2. Add connection state and deterministic event subscription/disposal; invoke hub subscription only if the server contract's membership semantics are correct.
3. Add REST fallback on reconnect and explicit retry; dedupe by message ID/client message ID.
4. Add unread badge/list refresh and mark-read behavior tied to successful API responses.
5. Implement CSS/grid desktop and mobile state transitions at repository breakpoints; test keyboard focus/back navigation.

## Todo

- [ ] Validate hub payloads and subscription semantics.
- [ ] Add reconnect/fallback state.
- [ ] Add mobile list/thread/info state transitions.
- [ ] Test duplicate/missed event recovery.

## Success criteria

- A message sent from another session appears after a realtime event; after forced SignalR failure, a manual/reconnect REST refresh recovers it.
- Unread count is not lost on failed read/send operations.
- Desktop and mobile screenshots/flows show no clipped composer, hidden back action or horizontal overflow.

## Risk assessment

- The current hub `Subscribe(Guid)` behavior should be verified before the client relies on conversation groups; user-targeted notifications may be sufficient for the first increment.
- Singleton SignalR handlers can leak if page subscriptions are not removed; add disposal/duplicate-event tests.

## Security considerations

- Keep hub `[Authorize(Policy = CollaborationPermissions.Chat)]` and membership validation intact.
- Treat realtime payloads as untrusted DTO data and render text safely.

## Next steps

Run the verification matrix with two authenticated users and a disconnected backend scenario.

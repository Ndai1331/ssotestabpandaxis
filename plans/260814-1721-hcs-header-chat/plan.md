---
title: "HCS header, localization and collaboration chat"
description: "Add BFF-safe logout, a culture selector, message navigation, and license-clean collaboration chat parity to the free HCS client."
status: in-progress
priority: P1
effort: 3-5d
branch: main
tags: [feature, auth, localization, collaboration, blazorise, responsive]
blockedBy: [260815-1550-hcs-chat-audit]
blocks: []
created: "2026-08-14"
createdBy: "Codex"
---

# HCS header, localization and collaboration chat

## Scope

Extend `services/HCS_web_free_license` with an authenticated header action set (logout, culture selector, message shortcut) and a responsive, Blazorise-based chat workspace. The paid service is a behavior/layout reference only; no paid source, package, asset, DTO or `HC.HttpApi` dependency may enter the free solution.

The implementation is split so the header/logout work can proceed immediately, while chat contact lookup and culture switching are treated as explicit API decisions rather than guessed browser behavior.

## Evidence and current state

- `HCSMainLayout.razor` already has an authenticated user dropdown and a static notification button, but no logout, culture selector or header message action.
- `BffEndpoints` already exposes protected `POST /bff/logout`; `BffHttpMessageHandler` already obtains and attaches the antiforgery token for non-GET calls.
- `/chat` and `/hubs/chat` are already proxied by the gateway. Free Collaboration has conversation, message, read/unread and attachment endpoints, but the client is read-only and has no contact/group directory UI.
- Free `Collaboration.Chat` is present in the current central permission catalog and its role-to-claim source path exists. The follow-up audit must still fix/verify the Blazor host dynamic-policy provider and authenticated BFF claim before runtime acceptance.
- The free client has no culture selector contract. AuthServer has an MVC language-switch endpoint, but its cookie/host behavior and language-list visibility are not a Blazor client contract.

## Related plans and dependencies

- Follow-up audit: `260815-1550-hcs-chat-audit` (host policy provider, destructive confirm modal, gateway/claim/runtime evidence and license gate). Runtime acceptance in this plan is blocked until that audit is closed.

- Related: `260813-1200-hcs-free-feature-parity` (permission claim propagation and feature exposure); verify its open authorization work before chat smoke tests.
- Related: `260814-1000-hcs-blazorise-localization` (shared localization primitives); reuse its decisions, but this plan owns header/chat strings needed for the requested increment.
- Baseline: `260812-0926-hcs-admin-navigation-catalogs` (current HCS layout, Blazorise 2.3.0 and admin permission UI).
- No paid service project may become a project/package/runtime dependency.

## API and dependency gaps

1. **Permission catalog/policy provider:** add/align `Collaboration.Chat` and `Collaboration.Notifications` with the central HCS permission definition, access-token resolver and Blazor dynamic-policy provider, or prove an equivalent claim source. Do not bypass the policy in UI or API.
2. **Contacts:** free chat stores member `Guid`s and has no non-admin user lookup endpoint. A least-privilege contact lookup/projection is required for user search, direct-chat creation and member names. `/api/identity/users` is not an acceptable substitute for ordinary users.
3. **Culture:** choose and test a client-safe switch that sets the request culture and persists it across reloads. The admin-only language-management API cannot be the selector's data source.
4. **Realtime/storage:** text send and attachment upload depend on Collaboration, its database/outbox, SignalR and MinIO being healthy. The UI must retain REST refresh/retry behavior when SignalR is unavailable.

## Phases

1. Header/auth/localization contract — logout and message action now; culture selector contract and permission-aware visibility.
2. Collaboration contracts/API — typed free DTO/client boundary, permissions, contacts/groups, attachment/realtime capability checks.
3. Desktop chat workspace — contacts/conversations, thread, composer, attachment state and information panel.
4. Realtime/responsive behavior — SignalR lifecycle, unread/read synchronization and mobile one-column navigation.
5. Verification and license audit — focused tests, build/runtime smoke matrix, responsive/accessibility checks and `audit-license-clean.sh`.

## Implementation status

- Implemented: central Collaboration permission definitions and client policy mapping; BFF logout action; header culture selector/message shortcut; request-culture cookie; typed Collaboration client; least-privilege Platform contact lookup; gateway route; responsive chat list/thread/info UI; direct/group creation; text/attachment send; pin/rename/leave; unread/read; SignalR status and REST fallback states.
- Verified: localization JSON parsing; Platform contact controller, Collaboration service, Blazor host and Blazor client C# compilation; Collaboration test suite (19/19); scoped implementation license/secret audit; gateway production compilation.
- Pending: full Platform/Blazor WebAssembly packaging, gateway test assembly packaging, Docker dependency health and authenticated browser smoke tests. The repository-wide audit script was not allowed to finish because it scans generated/vendor trees; the equivalent source implementation scan passed.
- Current environment issue: full MSBuild/static-assets runs intermittently hang after dependency compilation; this is kept separate from source diagnostics and must be rerun before marking complete.

## Acceptance criteria

- An authenticated user sees a message icon that navigates to `/chat`, a language selector beside the notification action, and a user menu with a CSRF-protected BFF logout action. Logout refreshes auth state, clears the BFF session and returns to login/home without exposing tokens.
- The selector displays only supported cultures, persists the choice after reload, and localizes all new header/chat labels and states in Vietnamese and English. If the culture contract is not available, the plan reports the exact blocker and does not ship a misleading control.
- `/chat` is guarded by `Collaboration.Chat` at route, menu and API levels. Desktop shows contacts/conversations, messages/composer and chat information; mobile provides an explicit list/thread/info navigation state.
- Text send, mark-read, pin/leave/member operations and attachment upload/download are exposed only when the corresponding free endpoint and permission are available. Unsupported actions are hidden or disabled with a localized explanation.
- Loading, empty, error/retry, reconnecting and permission-denied states are covered. REST refresh remains usable when SignalR disconnects.
- Focused tests, full free-solution build/test where feasible, Docker health checks and the license audit pass. No commercial code/package/asset is copied.

## Blockers and unresolved questions

- Resolved: contact lookup is a least-privilege, authenticated Platform endpoint (`GET /api/chat/contacts`) backed by `IIdentityUserRepository`; it returns only active user id/name/status and excludes the caller.
- Resolved: culture switching writes the supported `en`/`vi` ASP.NET Core culture cookie on the BFF host and forces a reload; the server now advertises the same supported cultures.
- Confirm the current local runtime has Collaboration DB, MinIO, outbox and SignalR healthy before promising attachments/realtime in acceptance evidence.
- Confirm the admin/test roles that should receive Collaboration chat and notification permissions; non-admin defaults must remain least privilege.

---
title: "Scout report — HCS header, localization and collaboration chat"
type: scout-report
status: complete
created: "2026-08-14"
---

# Scout report

## Summary

The free HCS client has enough BFF, gateway and Collaboration primitives for logout, a message shortcut, conversation read/list behavior, text send and attachment workflows. It does not yet have the client UI or typed client boundary for those workflows. Two backend contracts are not safe to infer: ordinary-user contact lookup and culture switching. A third cross-service concern—Collaboration permission claims—is not registered in the central HCS permission catalog.

## Header/auth findings

- `services/HCS_web_free_license/src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor` renders the header actions and authenticated user dropdown. The dropdown has identity/account links but no logout action. A top navigation item already points to `/chat` but is labeled `Saas` with a globe icon, while the main menu contributor uses the intended `Trao đổi`/comments icon.
- `services/HCS_web_free_license/gateways/web/HCS.WebGateway/Bff/BffEndpoints.cs` provides `POST /bff/logout` and validates antiforgery before signing out the gateway cookie.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Http/BffHttpMessageHandler.cs` attaches the BFF antiforgery token for non-GET requests, so logout should use the existing named `HCS.Bff` client and not manually manipulate cookies or tokens.
- `Routes.razor` uses `CascadingAuthenticationState`, `BffAuthenticationGate` and `AuthorizeRouteView`; the chat page should add an explicit permission policy in addition to menu visibility.

## Localization findings

- `HCS.Blazor` configures request localization with English default and ABP request-localization middleware. `HCSResource` has `vi.json` and `en.json`, but many custom client strings are hard-coded Vietnamese.
- AuthServer's login view uses `ILanguageProvider` and `/Abp/Languages/Switch?culture=...&uiCulture=...&returnUrl=...`. No free Blazor client component currently consumes that endpoint, and the language-management API is permission-protected for administrators.
- The selector therefore needs a verified UI/BFF culture contract or a deliberately constrained supported-culture list; it must not assume an admin API or a cross-host cookie will work.

## Collaboration findings

- Free contracts in `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService.Contracts/CollaborationContracts.cs` already model conversation types, members, permissions, send input, attachments, messages and paging.
- `services/HCS_web_free_license/services/collaboration/HCS.CollaborationService/Api/ChatController.cs` already exposes conversation CRUD/member operations, send/delete/pin/forward, read/unread, message search/context and attachment upload/download/delete.
- `CollaborationAppService` validates membership, attachment ownership/binding, message length, reply references and permission capabilities. The existing API is suitable for a typed client, subject to runtime/permission verification.
- `ChatHub` and `ChatRealtimeConnection` provide a SignalR path and recipient notification events. The client connection does not currently implement the full subscribe/lifecycle UI; REST refresh is still required as a fallback.
- `ChatWorkspace.razor` currently loads conversation rows and read-only messages. It has no contact directory, group creation flow, composer, attachment input, information panel, mark-read action or explicit chat permission guard.
- There is no free ordinary-user contact lookup endpoint. Conversation member DTOs carry only user IDs and roles, so the client cannot safely render names or discover recipients for direct/group conversations.

## Permission/license findings

- Collaboration controllers/hub require `Collaboration.Chat` and notifications require `Collaboration.Notifications` via a `permission` claim.
- `HCSRoleDataSeedContributor` seeds every enabled permission registered by the central `HCS.Permissions` definition. The central `HCSPermissionDefinitionProvider` currently defines HCS, organization, work-management and document permissions, not Collaboration permissions. This must be aligned before relying on role grants.
- `PermissionAuthorizationPolicyProvider` currently recognizes `HCS.`, `AbpIdentity.`, `FeatureManagement.` and `SettingManagement.` prefixes, not `Collaboration.`. A page-level `[Authorize(Policy = Collaboration.Chat)]` therefore needs an explicit provider/policy mapping decision in addition to token claim emission.
- `PermissionClaimResolver`, gateway OIDC token handling and `/bff/user` already have a permission-claim path; the work is to register/seed the exact free Collaboration names and test fresh-login propagation.
- `services/HCS_web_with_license` was inspected only for behavior/layout. Its commercial `HC.HttpApi`, Chat UI and Pro modules are excluded from implementation dependencies.

## Expected technical boundary

The client should reference the free Collaboration Contracts project directly or through a new free client adapter, use the existing `HCS.Bff` named client, and keep DTO mapping in free-owned code. The gateway already proxies `/api/chat`, `/api/notifications` and `/hubs/chat`, so route changes are conditional on any newly introduced contact endpoint.

## Scout unresolved items

1. Select the owner and shape of a privacy-minimal user lookup contract.
2. Select the culture-switch mechanism that survives the Blazor/BFF/AuthServer host boundary.
3. Verify the local Collaboration dependencies and fresh-login permission claims before committing to attachment/realtime evidence.

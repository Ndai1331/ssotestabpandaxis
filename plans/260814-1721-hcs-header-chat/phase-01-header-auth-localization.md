# Phase 1 — Header, auth and localization contract

## Context links

- Main layout: `services/HCS_web_free_license/src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor`
- Layout styles: `services/HCS_web_free_license/src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor.css`
- BFF endpoints: `services/HCS_web_free_license/gateways/web/HCS.WebGateway/Bff/BffEndpoints.cs`
- BFF client/auth: `services/HCS_web_free_license/src/HCS.Blazor.Client/Http/BffHttpMessageHandler.cs`, `Authentication/BffAuthenticationStateProvider.cs`
- Related localization plan: `plans/260814-1000-hcs-blazorise-localization/`

## Overview

Priority: P1. Status: implemented, browser verification pending. Deliver the requested header affordances without weakening BFF security or inventing a culture API.

## Key insights

- Logout is an existing server capability, not a new AuthServer feature. Invoke `POST /bff/logout` through the configured named client so the existing antiforgery handler is reused.
- The top-right chat shortcut should be a real link to `/chat`, use the message icon already used by `HCSMenuContributor`, and be hidden/disabled consistently with the chat permission once claims are available.
- The notification bell is outside the requested notification-center implementation. Preserve its placement and add only the selector/action adjacency needed by this task.

## Requirements

- Authorized user: display message shortcut, culture selector, notification action and user dropdown with logout.
- Logout: show in-progress/failed state, prevent duplicate calls, refresh `BffAuthenticationStateProvider`, then navigate to a safe anonymous route. Do not call AuthServer logout directly or store access/refresh tokens in the browser.
- Culture: define supported cultures, current-culture display, persistence and reload semantics before UI wiring. All labels/error states added by this task must use `HCSResource` keys in `vi.json` and `en.json`.
- Anonymous/forbidden users: do not render a misleading authenticated action; deep links remain protected by `AuthorizeRouteView` and the page policy.

## Architecture/data flow

`Header action → HCS.Bff HttpClient → BffHttpMessageHandler (antiforgery) → POST /bff/logout → gateway cookie sign-out → auth-state refresh → safe navigation`.

For culture, choose one verified path: a BFF/UI endpoint that sets the request-culture cookie, or an AuthServer switch endpoint whose redirect/cookie behavior is proven for the UI host. The selector must not use the admin-only language-management API.

## Related code files

Modify: `HCSMainLayout.razor`, `HCSMainLayout.razor.css`, `HCS.Blazor.Client/Authentication/BffAuthenticationStateProvider.cs` only if refresh visibility requires an API change, and `HCS.Domain.Shared/Localization/HCS/{vi,en}.json`.

Create if justified by component-size rules: `Layouts/HeaderActions.razor`, `Layouts/CultureSelector.razor`, and a small logout action component/service.

Conditional backend files: `gateways/web/HCS.WebGateway/Bff/BffEndpoints.cs` or the UI host's culture endpoint. Do not modify until the cookie/host contract is selected.

## Implementation steps

1. Confirm the existing BFF named client and authentication-state refresh behavior with focused tests.
2. Replace the misleading top-header chat/SaaS action with an accessible message link to `/chat` and preserve the existing main navigation link.
3. Add a reusable logout action with busy/error handling and a safe post-logout navigation target.
4. Add the culture contract first, then the selector; add localized labels, aria text, empty/error text and confirmation text.
5. Validate desktop, 860px and 480px header layouts without obscuring the user menu or selector.

## Todo

- [ ] Decide culture endpoint/list contract.
- [ ] Add header/logout/culture tests.
- [ ] Verify fresh login → `/bff/user` → permission-aware header.

## Success criteria

- Logout clears the BFF session through the existing endpoint and never sends a browser-held token.
- Message navigation reaches `/chat` and retains permission protection.
- Culture choice survives reload and all new strings resolve in both supported resource files.

## Risk assessment

- Cross-host culture cookies may appear to work in one local URL and fail after Caddy/HTTPS; require a browser reload test on `https://hcs.localhost`.
- A static bell badge without a notification contract could mislead users; do not add unread behavior unless the existing notification API is wired and verified.

## Security considerations

- Keep BFF cookie-only architecture and antiforgery validation.
- Do not expose OIDC tokens, refresh tokens, account secrets or raw claims in the header.
- Treat client permission visibility as UX only; gateway/service policies remain authoritative.

## Next steps

Resolve the culture contract and complete the permission-catalog decision before implementing the chat page guard.

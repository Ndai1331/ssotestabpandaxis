# Phase 1 — Create a single BFF-backed auth state

## Overview

Priority: P1. Replace the split gate/cascade decision with an `AuthenticationStateProvider` whose principal is built from the Gateway BFF response.

## Related code files

- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Authentication/BffAuthenticationGate.razor` — await the provider and redirect only for the anonymous state; delete temporary debug POST/logging.
- Create `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Authentication/BffAuthenticationStateProvider.cs` — internal DTOs and provider implementation.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/HCSBlazorClientModule.cs` — register the concrete provider and `AuthenticationStateProvider` as the same scoped instance after `HCS.Bff` is configured.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Routes.razor` only if required to inject/use the registered cascade; retain exactly one `CascadingAuthenticationState`.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs` — replace the temporary `context.Menu.Items.Clear()` state with the minimal authenticated **Trao đổi** (`/chat`) sidebar item.

## Design

`BffAuthenticationStateProvider.GetAuthenticationStateAsync()` calls relative `bff/user` through the existing named `HCS.Bff` client (which preserves BFF cookies). On a successful payload where `isAuthenticated` is true, create a `ClaimsIdentity` with a fixed local authentication type and all returned claim type/value pairs. Otherwise return an empty `ClaimsPrincipal`.

The provider owns caching/refresh semantics for the initial application render and exposes a refresh method used by the gate. That method atomically replaces its state and calls `NotifyAuthenticationStateChanged`; it must not expose raw response bodies or tokens. The gate renders a loading state while refresh is pending, then uses the provider’s resulting principal, not an independent HTTP status flag.

## Steps

1. Define private DTOs matching the current Gateway JSON (`isAuthenticated`, `name`, `claims[].type`, `claims[].value`) with case-insensitive deserialization.
2. Implement the provider with cancellation-safe `HttpClient` use, an unauthenticated fallback for 401, non-success, invalid JSON, and network errors, and no background debug/telemetry call.
3. Preserve `name` when Gateway returns no name claim by adding it as the identity name claim only when non-empty; do not manufacture roles/permissions.
4. Register one scoped provider instance for both the concrete type and `AuthenticationStateProvider` abstraction. Do not use singleton state in WASM.
5. Refactor the gate to call provider refresh once during initialization; remove its direct `/bff/user` parsing and temporary agent log block. Ensure redirect target remains the existing safe BFF login component.
6. Verify `AuthorizeView`, ABP/Lepton navigation, and header consume the cascade supplied by the provider; do not add a nested cascade or parallel OIDC provider.
7. Restore the `Trao đổi` main-menu item with `.RequireAuthenticated()` so the authenticated admin has a visible sidebar destination. Do not weaken Collaboration Service's `Collaboration.Chat` API/hub policy; if its permission claim is absent, `/chat` continues to show its existing authorization/error behavior and is tracked separately from this UI recovery.

## Success criteria

- A 200 BFF payload with `role: admin` produces an authenticated principal with `admin` role.
- A 401/non-200/malformed response produces anonymous UI without an unhandled exception.
- The gate and all consuming UI components receive the exact same `AuthenticationState` instance/update.
- Authenticated admin sees one left-menu item, **Trao đổi**; anonymous users do not.

## Security

Only claims already allowlisted by `BffEndpoints.IsPublicClaim` enter the client principal. Keep tokens in the HTTP-only BFF cookie and remove the diagnostics code that could emit profile payloads externally.

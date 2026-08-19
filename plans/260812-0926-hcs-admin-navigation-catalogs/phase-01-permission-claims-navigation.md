# Phase 1 — Unify permission claims and admin navigation

## Overview

Priority: P1. Make the existing local-role grants effective in the AuthServer-issued access token, the BFF public profile, service policies, and the ABP navigation authorization checks.

## Evidence

- `AbpPermissionGrants` grants `admin` Identity and HCS Organization permissions, but `KeycloakClaimsProcessor` adds only role/email claims.
- Organization, Collaboration, and Work Management APIs enforce `RequireClaim("permission", permission)`.
- `BffEndpoints.IsPublicClaim` currently filters permission claims, so the Blazor authorization cascade cannot authorize permission-bound menus.
- `HCSMenuContributor` currently has only authenticated **Trao đổi**; the route is recovered but unrelated service permissions remain absent.

## Related code files

- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/apps/auth-server/HCS.AuthServer/Authentication/**` — add a focused token-claim contributor/handler that resolves effective local-role grants during OpenIddict token issuance.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/apps/auth-server/HCS.AuthServer/HCSAuthServerModule.cs` — register the contributor/handler using the existing OpenIddict pipeline.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/gateways/web/HCS.WebGateway/Bff/BffEndpoints.cs` — whitelist the public `permission` claim in the sanitized BFF profile; do not return tokens.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs` — build the groups/items in the matrix and apply `RequirePermissions`/`RequireAuthenticated` consistently.
- Modify or add focused tests under `apps/auth-server/HCS.AuthServer.Tests`, `gateways/web/HCS.WebGateway/HCS.WebGateway.Tests`, and `src/HCS.Blazor.Client/**Tests**` using existing test-project conventions.

## Architecture

```text
Keycloak groups → local HCS role provisioning → ABP role grants
   → AuthServer access-token permission claims → Gateway BFF cookie/proxy
   → sanitized /bff/user permission claims → BFF auth provider
   → ABP menu authorization + service API policy
```

The server resolves permissions from the local role grants. The browser receives only permission-name claims already required for menu presentation; API authorization continues to verify the signed access token independently.

## Implementation steps

1. Identify the OpenIddict sign-in/token event where the external user principal and assigned local roles are available. Add one internal resolver that obtains the effective allow-set through ABP permission APIs/repositories, correctly deduplicates role grants, and assigns `permission` claims only to the access token destination.
2. Preserve the existing Keycloak app gate, role mapping, first-login provisioning, issuer/audience, and token encryption decision. A user without `bd-app-hcs` receives no token; an unmapped-but-entitled user remains `nhanvien` with no admin/catalog claim.
3. Do not derive permissions in Blazor from `role == admin`, and do not trust the UI. Ensure all Organization service policies remain unchanged and become satisfiable solely by the signed claim.
4. Add `permission` to the Gateway's public-profile allowlist so `BffAuthenticationStateProvider` receives it. Keep raw JWTs, refresh tokens, authorization codes, role-grant IDs, and any non-allowlisted claims out of `/bff/user`.
5. Replace the one-item sidebar with the exact hierarchy in the plan matrix. Use stable menu names, localized Vietnamese labels, Font Awesome icons, ordering, and ABP `RequirePermissions`. Retain a small authenticated workspace entry so a successful login cannot result in an empty sidebar.
6. Verify both the Blazor host contributor (SSR) and WASM client contributor render the same hierarchy from the same claim names. Do not clear/rebuild the menu in a component lifecycle method.
7. Add tests for admin claim emission; unprivileged role non-emission; BFF filtering; root/group menu shape; and route/menu concealment when each permission is absent.

## Success criteria

- A newly issued `admin` access token contains each effective admin grant once, including `AbpIdentity.Roles.ManagePermissions` and the four exposed Organization/catalog grants.
- `/bff/user` exposes only allowlisted identity, role, and permission name/value pairs.
- Permission removal followed by a fresh login causes both sidebar concealment and API `403`.

## Security considerations

- Permission names are authorization metadata, not an authority by themselves: the service validates the signed bearer token.
- Bounds-check any claim count/length and use the database/ABP effective-grant mechanism rather than accepting a Keycloak-provided `permission` claim.


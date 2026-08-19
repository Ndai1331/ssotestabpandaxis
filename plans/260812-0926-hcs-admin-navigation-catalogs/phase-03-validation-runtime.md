# Phase 3 — Verify authorization, Docker runtime, and documentation

## Overview

Priority: P1. Prove the complete browser/API authorization chain and record local operating instructions. No migration or seed data import occurs in this phase.

Implementation status: In progress as of 2026-08-14. Organization and Blazor Docker images were rebuilt and restarted; focused source/test checks pass. Full solution/license audit and complete browser lifecycle evidence remain open.

## Test matrix

| Scenario | Expected result |
|---|---|
| Fresh incognito `admin` login | Header authenticated; workspace + administration/catalog sidebar visible; no Login link. |
| `nhanvien`, `bacsi`, `lanhdao` fresh login | No admin/catalog menu; direct Organization API receives 403. |
| Admin opens role permissions | Built-in ABP role page and permission dialog load through gateway; no CORS/token leak. |
| Admin grants/revokes a catalog permission to a test role | Change persists in `AbpPermissionGrants`; after affected user signs out/in, menu and API behavior change together. |
| Catalog lifecycle | Empty state → create → filter/page → edit → delete; values persist in `hcs_organization.MasterDataItems`/entity tables. |
| Direct deep link and API | Authorized deep link loads; anonymous UI redirects through BFF; anonymous API is 401 and unauthorized API is 403. |

## Commands

Run from `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license`:

```bash
./scripts/audit-license-clean.sh
dotnet restore HCS.slnx --configfile NuGet.Config
dotnet build HCS.slnx --no-restore
dotnet test HCS.slnx --no-build
docker compose build auth-server web-gateway blazor platform organization
docker compose up -d --force-recreate auth-server web-gateway blazor platform organization caddy
docker compose ps
```

## Implementation steps

1. Run target test projects before the full suite: AuthServer claim tests, WebGateway BFF/profile tests, Blazor navigation/component tests, Organization service authorization/contract tests.
2. Build and recreate only changed containers plus Caddy; inspect logs for token validation, route-proxy, JSON/serialization, and authorization errors without emitting credentials or tokens.
3. Perform the test matrix in a new incognito window at `https://hcs.localhost`; hard-refresh after deployment. Capture status codes and redacted route/claim names only.
4. Query DB read-only to confirm grants and catalog counts. Do not seed production-like catalog values automatically; an administrator creates/imports approved data later.
5. Update the HCS README/runbook with: Keycloak vs HCS role boundary, permission-change re-login requirement, admin UI route/menu, restart order, URLs, and troubleshooting for missing sidebar/403.

## Success criteria

- All tests/build/audit pass and Docker services are healthy.
- Browser evidence matches the matrix with no browser-held token and no direct service bypass.
- Documentation makes it clear which data can be managed in HCS and which identity attributes remain in Keycloak.

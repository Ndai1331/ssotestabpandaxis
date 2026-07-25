# Phase 05 — Elsa Studio (Blazor WASM standalone) + OpenIddict client

**Goal:** Tạo app Elsa Studio WASM standalone chạy `:44396`, trỏ backend về WorkflowService `:44395`, đăng nhập qua AuthServer (OpenIddict Code+PKCE, SSO đồng bộ Keycloak). Đây là UI Option A — app riêng, mở ở tab mới.

**Depends on:** phase-02 (host `/elsa/api`), phase-03 (scope + CORS/redirect).
**Owns files:** `services/abp-blazor/apps/elsa-studio/**` (mới) + client entry trong `OpenIddictDataSeeder.cs` + `IdentityService appsettings.json`.
**Docs:** https://abp.io/docs/latest/modules/elsa-pro (mục Elsa Studio) + https://v3.elsaworkflows.io (Elsa Studio WASM host).

## A. Tạo project Elsa Studio WASM
- [ ] Tạo `apps/elsa-studio/HanhChinhSo.ElsaStudio/HanhChinhSo.ElsaStudio.csproj` (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`, net10.0).
- [ ] Dùng template/host chuẩn Elsa Studio WASM: package `Elsa.Studio*` (Core, Dashboard, Login, Workflows.Designer) hoặc biến thể ABP-flavored theo docs Elsa Pro. Pin version khớp Elsa host (unresolved Q4).
- [ ] `Program.cs`: cấu hình Studio backend:
  - `RemoteBackendOptions.Url = "http://localhost:44395/elsa/api"` (hoặc `:44398/elsa/api` nếu chọn qua gateway ở phase-04C).
  - Auth: OIDC/OpenIddict Authorization Code + PKCE:
    - Authority `http://localhost:44372`
    - ClientId `ElsaStudio`
    - Scope: `openid profile email roles WorkflowService`
    - RedirectUri `http://localhost:44396/authentication/login-callback`
    - PostLogoutRedirect `http://localhost:44396/authentication/logout-callback`
  - `RequireHttpsMetadata=false` (lab).
- [ ] `wwwroot/appsettings.json`: expose các URL trên để đổi không cần rebuild.
- [ ] `Properties/launchSettings.json`: `applicationUrl=http://localhost:44396`, env `Development`.

> Nếu bản Elsa Studio dùng **token endpoint / password (`UseAbpIdentity`)** thay vì OIDC redirect, fallback: cấu hình Studio login form gọi `/connect/token` password grant. Plan mặc định Code+PKCE để đúng SSO — chốt ở unresolved Q2.

## B. OpenIddict client `ElsaStudio` (public, Code+PKCE)
File: `services/identity/hanhchinhso.IdentityService/Data/OpenIddictDataSeeder.cs` → `CreateClientsAsync()`
- [ ] Đọc root url: `Configuration["OpenIddict:Applications:ElsaStudio:RootUrl"].EnsureEndsWith('/')`.
- [ ] `CreateOrUpdateApplicationAsync(...)`:
  - `applicationType = Web`, `type = Public` (WASM → public, PKCE), `consentType = Implicit`.
  - `name = "ElsaStudio"`, `displayName = "Elsa Studio"`, `secret = null`.
  - `grantTypes = [ AuthorizationCode ]` (PKCE tự áp cho public client).
  - `scopes = commonScopes ∪ { "WorkflowService" }`.
  - `redirectUris = [ "{root}authentication/login-callback" ]`.
  - `postLogoutRedirectUris = [ "{root}authentication/logout-callback" ]`.
  - `clientUri = root`.
- [ ] Bật `requirePkce`/`RequireProofKeyForCodeExchange` nếu API seeder hỗ trợ tham số (public SPA client).

## C. RootUrl config (IdentityService appsettings.json)
- [ ] `OpenIddict:Applications` thêm:
```json
"ElsaStudio": { "RootUrl": "http://localhost:44396/" }
```
- [ ] (CORS/redirect `:44396` đã thêm ở AuthServer phase-03; double-check còn đủ.)

## D. Solution wiring (optional cho Studio)
- [ ] (Optional) thêm `HanhChinhSo.ElsaStudio` vào `Default.abprun.json` applications (`folder: apps`, `launchUrl :44396`) để chạy chung profile. Không bắt buộc — có thể `dotnet run` riêng.

## Verify
- [ ] Restart IdentityService (seed client) → `OpenIddictApplications` có `ElsaStudio` (public, redirect `:44396/authentication/login-callback`).
- [ ] `dotnet run` Studio `:44396` → trang login redirect sang AuthServer `:44372`; đăng nhập admin (qua Keycloak/AuthServer) → callback về Studio, vào dashboard.
- [ ] Studio gọi `:44395/elsa/api` kèm bearer token → list workflows 200 (không CORS error trong console).

## Rollback
- Xóa `apps/elsa-studio/`, block `ElsaStudio` trong seeder + `OpenIddict:Applications.ElsaStudio` appsettings, và entry run-profile (nếu thêm).

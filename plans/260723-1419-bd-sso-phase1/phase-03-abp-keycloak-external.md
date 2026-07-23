---
phase: 3
title: "ABP AuthServer Keycloak external"
status: pending
effort: 4h
dependsOn: [1]
---

# Phase 03 — ABP AuthServer Keycloak external (Approach A)

## Goal

AuthServer (`:44372`) thêm external OpenID Connect **Keycloak**. Blazor (`:44306`) vẫn authenticate qua AuthServer. Login Keycloak → tạo/link `IdentityUser` + gán 4 roles.

## Prerequisites

- Phase 1: client `abp-auth` + secret + groups claim  
- ABP infra up (`services/abp-blazor/etc/docker`)  
- AuthServer + Blazor chạy theo README (pfx, `abp install-libs`)

## Steps

### 1. Confirm baseline ABP chạy

- AuthServer http://localhost:44372  
- Blazor http://localhost:44306  
- Login local ABP admin OK (trước khi gắn KC)

### 2. Add OpenIdConnect package (nếu chưa reference trực tiếp)

AuthServer đã có transitive `Microsoft.AspNetCore.Authentication.OpenIdConnect` qua ABP Account. Nếu compile thiếu → thêm package reference explicit.

### 3. Configure external provider

File: `services/abp-blazor/apps/auth-server/abptestwithsso.AuthServer/abptestwithssoAuthServerModule.cs`  
Method: `ConfigureExternalProviders`

Thêm (pattern cạnh Google):

```csharp
.AddOpenIdConnect("Keycloak", "Keycloak", options =>
{
    options.Authority = configuration["Keycloak:Authority"]; // http://localhost:5110/realms/bd
    options.ClientId = configuration["Keycloak:ClientId"];   // abp-auth
    options.ClientSecret = configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.CallbackPath = "/signin-oidc"; // must match KC Valid redirect URIs
    options.ClaimActions.MapJsonKey("groups", "groups");
    // Map email → AbpClaimTypes.Email / Identity username as needed
})
```

Config trong `appsettings.Development.json` hoặc User Secrets:

```json
"Keycloak": {
  "Authority": "http://localhost:5110/realms/bd",
  "ClientId": "abp-auth",
  "ClientSecret": "<secret>"
}
```

Align KC redirect URI nếu CallbackPath khác.

### 4. Auto-provision + role mapping

- Dùng ABP Account external login flow (built-in) để tạo `IdentityUser` khi lần đầu  
- Sau external login success: đọc claim `groups` → assign roles `admin|bacsi|lanhdao|nhanvien`  
- Seed 4 roles trong Identity DB nếu chưa có (data seed contributor hoặc one-time SQL/UI)  
- Priority multi-group: admin > lanhdao > bacsi > nhanvien  

Implementation options (chọn đơn giản nhất khi code):

1. `IExternalLoginInfo` / `RegisterAsync` hook trong Account module  
2. Custom `OpenIdConnectEvents.OnTokenValidated` map roles  

Ưu tiên event hook gọn, không fork Account Pro nếu tránh được.

### 5. Blazor login UX

- Trang login AuthServer hiện nút **Keycloak**  
- Blazor challenge → AuthServer → Keycloak (2 hop — chấp nhận Approach A)

### 6. Verify redirect chain

1. Mở Blazor → Login  
2. Chọn Keycloak  
3. Login `admin@benhvien.vn`  
4. Về Blazor authenticated  
5. Identity → Users: có user + role `admin`  

Lặp 3 user còn lại.

## Files likely touched

| File | Change |
|------|--------|
| `abptestwithssoAuthServerModule.cs` | AddOpenIdConnect |
| `appsettings.Development.json` / user-secrets | Keycloak section |
| New: role mapper helper / event | Map groups → roles |
| Identity seed (optional) | 4 roles |

## Success criteria

- [ ] Nút Keycloak trên AuthServer login  
- [ ] 4 users provision + đúng role  
- [ ] Local ABP login vẫn hoạt động  
- [ ] Không commit client secret  

## Risks

| Risk | Mitigation |
|------|------------|
| Redirect URI mismatch | Sync KC client ↔ CallbackPath |
| Claims không có `groups` | Check mapper phase 1; Add to userinfo |
| Correlation cookie / HTTPS | Local HTTP — check `Cookie.SameSite` / `CorrelationCookie` nếu fail |
| Role name case | Chuẩn hóa lowercase `admin`… như bảng plan |

## Next

Phase 04 — E2E SSO + runbook.

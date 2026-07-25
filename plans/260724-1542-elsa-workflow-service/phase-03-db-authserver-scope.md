# Phase 03 — DB connection + AuthServer scope/audience

**Goal:** WorkflowService có connection string đúng, và AuthServer (OpenIddict) cấp token cho scope/resource `WorkflowService` để Studio + Swagger + Blazor gọi API hợp lệ.

**Depends on:** phase-01 (service tồn tại), phase-02 (Elsa dùng cùng DB).
**Owns files:**
- `services/abp-blazor/services/workflow-service/hanhchinhso.WorkflowService/appsettings.json`
- `services/abp-blazor/services/identity/hanhchinhso.IdentityService/Data/OpenIddictDataSeeder.cs`
- `services/abp-blazor/services/identity/hanhchinhso.IdentityService/appsettings.json`
- `services/abp-blazor/apps/auth-server/hanhchinhso.AuthServer/appsettings.json` + `appsettings.Development.json`

## Connection string
- [ ] WorkflowService `appsettings.json` → `ConnectionStrings.WorkflowService = "Host=localhost;Port=5432;Database=hanhchinhso_Workflow;User ID=postgres;Password=myPassword;"`.
- [ ] Giữ mapped connections `AdministrationService` (Permission/Feature/Setting) + `AuditLoggingService` như LanguageService (module đã map trong `ConfigureDatabase`) để Permission Management hoạt động.

## API scope + resource (OpenIddict seeder — chạy ở IdentityService)
File: `services/identity/hanhchinhso.IdentityService/Data/OpenIddictDataSeeder.cs`
- [ ] `CreateApiScopesAsync()`: thêm `await CreateScopesAsync("WorkflowService");`.
- [ ] `CreateSwaggerClientsAsync()` → mảng scopes của `SwaggerTestUI`: thêm `"WorkflowService"`.
- [ ] `CreateClientsAsync()` → Blazor `BlazorWebApp` scopes: thêm `"WorkflowService"` (để Blazor/host có thể gọi nếu cần; Studio dùng client riêng ở phase-05).
- [ ] (Nếu thêm redirect Swagger cho WorkflowService) thêm `workflowServiceRootUrl` đọc từ `OpenIddict:Resources:WorkflowService:RootUrl` và append `/swagger/oauth2-redirect.html` vào `redirectUris` của Swagger client.

## OpenIddict RootUrl (IdentityService appsettings.json)
- [ ] Trong `OpenIddict:Resources` thêm:
```json
"WorkflowService": { "RootUrl": "http://localhost:44395" }
```
- [ ] (phase-05 sẽ thêm `OpenIddict:Applications:ElsaStudio.RootUrl`.)

## AuthServer CORS / redirect allowed
File: `apps/auth-server/hanhchinhso.AuthServer/appsettings.json` (+ `.Development.json`)
- [ ] Thêm `http://localhost:44395` (WorkflowService) và `http://localhost:44396` (Studio) vào `App:CorsOrigins`.
- [ ] Thêm `http://localhost:44396` vào `App:RedirectAllowedUrls` (Studio login redirect). *(WorkflowService là resource server, không cần redirect.)*

## Audience verify (WorkflowService)
- [ ] WorkflowService `appsettings.json` `AuthServer:Audience="WorkflowService"` (đã set phase-01) — khớp scope resource vừa tạo.

## Verify
- [ ] Restart IdentityService (chạy seeder) → bảng `OpenIddictScopes` có `WorkflowService`; `SwaggerTestUI` + `BlazorWebApp` có scope mới (kiểm psql `OpenIddictApplications`/permissions).
- [ ] Lấy token qua Swagger `SwaggerTestUI` chọn scope `WorkflowService` → decode JWT thấy `aud`/`scope` chứa `WorkflowService`.
- [ ] Gọi `:44395/elsa/api/...` kèm token → **200** (thay vì 401).

## Rollback
- Xóa các dòng `WorkflowService` đã thêm trong seeder + appsettings; restart IdentityService (seeder idempotent, scope thừa có thể để lại vô hại hoặc xóa thủ công trong DB).

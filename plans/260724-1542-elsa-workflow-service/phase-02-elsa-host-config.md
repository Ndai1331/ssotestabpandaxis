# Phase 02 — Cài & cấu hình ABP Elsa Pro trên host

**Goal:** Biến `hanhchinhso.WorkflowService` thành Elsa Pro host: cài package Elsa, `ConfigureElsa(...)`, expose `/elsa/api`, để Elsa tự tạo schema trong `hanhchinhso_Workflow`.

**Blockers (GATE):** Phải có **ABP Team+ license** active (`abp login` + quyền `Volo.Abp.Elsa.*`). Nếu `dotnet restore` không thấy được các package `Volo.Abp.Elsa.*` trên `nuget.abp.io` → DỪNG và báo user. Không workaround.
**Depends on:** phase-01.
**Owns files:** `services/abp-blazor/services/workflow-service/hanhchinhso.WorkflowService/**` (csproj + module).
**Docs:** https://abp.io/docs/latest/modules/elsa-pro

## Packages (thêm vào host csproj, version 10.5.0)
- [ ] `Volo.Abp.Elsa.Application`
- [ ] `Volo.Abp.Elsa.Application.Contracts` (có thể để trong Contracts project nếu Studio/Blazor cần proxy — lab để ở host là đủ)
- [ ] `Volo.Abp.Elsa.AspNetCore`
- [ ] `Volo.Abp.Elsa.Identity`
- [ ] `Volo.Abp.Elsa.Domain` + `Volo.Abp.Elsa.EntityFrameworkCore` *(nếu docs yêu cầu store ABP-side; nếu Elsa dùng store riêng thì bỏ)*

> Xác nhận danh sách chính xác theo trang docs Elsa Pro tại thời điểm cài (module list có thể gộp). Giữ đúng version dòng 10.5.0.

## Module wiring (`hanhchinhsoWorkflowServiceModule.cs`)
- [ ] Thêm vào `[DependsOn(...)]`: `AbpElsaApplicationModule`, `AbpElsaAspNetCoreModule`, `AbpElsaIdentityModule` (+ `AbpElsaEntityFrameworkCoreModule` nếu dùng).
- [ ] Trong `ConfigureServices` thêm `ConfigureElsa(context, configuration)`:
```csharp
private void ConfigureElsa(ServiceConfigurationContext context, IConfiguration configuration)
{
    context.Services.AddAbpElsa(elsa =>
    {
        elsa.UseAbpIdentity();

        elsa.UseWorkflowManagement(mgmt =>
            mgmt.UseEntityFrameworkCore(ef => ef.UsePostgreSql(
                configuration.GetConnectionString("WorkflowService"))));

        elsa.UseWorkflowRuntime(rt =>
            rt.UseEntityFrameworkCore(ef => ef.UsePostgreSql(
                configuration.GetConnectionString("WorkflowService"))));

        elsa.UseScheduling();
        elsa.UseJavaScript();
        elsa.UseLiquid();
        elsa.UseCSharp();
        elsa.UseHttp();          // HTTP activities + endpoints
        elsa.UseWorkflowsApi();  // /elsa/api cho Studio
    });
}
```
> Tên API (`AddAbpElsa` / `ConfigureElsa`) theo docs Elsa Pro — điều chỉnh cho khớp signature thực tế của package 10.5.0.
- [ ] `OnApplicationInitialization`: đảm bảo `app.UseAuthentication()`/`UseAuthorization()` đã có (từ phase-01) TRƯỚC khi map Elsa API; nếu docs yêu cầu middleware riêng (`app.UseWorkflowsApi()` / Elsa http) thì thêm đúng thứ tự sau `UseRouting`.
- [ ] Bật Elsa **auto-migration**: cấu hình để Elsa tự `AutoRunMigrations`/apply schema khi khởi động (theo option của `UseEntityFrameworkCore`). ABP `WorkflowServiceRuntimeDatabaseMigrator` (phase-01) chỉ lo ABP infra (inbox/outbox); Elsa store tách riêng.

## CORS cho Studio
- [ ] `ConfigureCors` (đã có từ template) đọc `App:CorsOrigins`; thêm `http://localhost:44396` vào `appsettings.json` của WorkflowService để Studio WASM gọi được `/elsa/api`.

## Verify
- [ ] `dotnet restore` kéo được `Volo.Abp.Elsa.*` (chứng minh license OK).
- [ ] `dotnet build` OK.
- [ ] Run `:44395` → khởi động không lỗi; kết nối `hanhchinhso_Workflow` và tạo bảng Elsa (kiểm psql: có bảng `Elsa*` / workflow definitions/instances).
- [ ] `GET http://localhost:44395/elsa/api/...` (endpoint list workflows) trả **401** khi không token (chứng tỏ API mounted + JwtBearer active).

## Rollback
- Gỡ block `ConfigureElsa` + Elsa entries khỏi `[DependsOn]` và các `PackageReference` Elsa. DB `hanhchinhso_Workflow` có thể drop.

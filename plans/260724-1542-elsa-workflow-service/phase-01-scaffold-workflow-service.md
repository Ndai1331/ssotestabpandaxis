# Phase 01 — Scaffold `hanhchinhso.WorkflowService`

**Goal:** Tạo microservice mới flat (`service-nolayer`) copy từ LanguageService, chạy được `:44395` với ABP infra (Redis/RabbitMQ/JWT/health/metrics) NHƯNG chưa có Elsa. Đây là khung sạch để phase-02 nhét Elsa vào.

**Blockers:** none (không cần license cho phase này).
**Owns files:** `services/abp-blazor/services/workflow-service/**`

## Target structure (mirror `services/language/`)
```
services/abp-blazor/services/workflow-service/
├── common.props                                  (copy nguyên từ services/language/common.props)
├── hanhchinhso.WorkflowService.abpmdl
├── hanhchinhso.WorkflowService.slnx
├── hanhchinhso.WorkflowService/
│   ├── hanhchinhso.WorkflowService.csproj
│   ├── Program.cs
│   ├── hanhchinhsoWorkflowServiceModule.cs
│   ├── hanhchinhsoMetrics.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json (nếu cần)
│   ├── Properties/launchSettings.json
│   ├── Controllers/HomeController.cs
│   ├── Data/WorkflowServiceDbContext.cs
│   ├── Data/WorkflowServiceDbContextFactory.cs
│   ├── Data/WorkflowServiceDataSeeder.cs
│   ├── Data/WorkflowServiceRuntimeDatabaseMigrator.cs
│   ├── HealthChecks/HealthChecksBuilderExtensions.cs
│   ├── HealthChecks/WorkflowServiceDatabaseCheck.cs
│   └── Dockerfile (copy, optional lab)
├── hanhchinhso.WorkflowService.Contracts/
│   ├── hanhchinhso.WorkflowService.Contracts.csproj
│   ├── hanhchinhsoWorkflowServiceContractsModule.cs
│   └── Localization/WorkflowServiceResource.cs (+ Localization/WorkflowService/en.json, vi.json)
└── hanhchinhso.WorkflowService.Tests/
    ├── hanhchinhso.WorkflowService.Tests.csproj
    ├── WorkflowServiceTestsModule.cs
    ├── WorkflowServiceIntegrationTestBase.cs
    └── TestProgram.cs
```

## Rename rules (khi copy từ LanguageService)
- Token `LanguageService` → `WorkflowService` (namespace, class, csproj, connection string name).
- Token `language-management` (RemoteServiceName/RootPath) → `workflow` / `WorkflowService`.
- Bỏ toàn bộ ref module `LanguageManagement*` (sẽ KHÔNG có ở service này) — DbContext chỉ giữ event inbox/outbox.
- Prefix migrations history: `__WorkflowService_Migrations`.
- Xóa `obj/`, `bin/`, `Logs/`, `Migrations/` copy dư; migrations Elsa/ABP sẽ tạo sau.

## Tasks
- [ ] Tạo thư mục `services/abp-blazor/services/workflow-service/` và copy `common.props` từ `services/language/common.props` (giữ `<AbpProjectType>service-nolayer</AbpProjectType>`).
- [ ] Tạo `hanhchinhso.WorkflowService/hanhchinhso.WorkflowService.csproj` dựa trên LanguageService csproj nhưng **loại bỏ** các `Volo.Abp.LanguageManagement.*` package; giữ: `Volo.Abp.EntityFrameworkCore.PostgreSql`, `AspNetCore.Mvc`, `Autofac`, `AspNetCore.Serilog`, `Swashbuckle`, `EventBus.RabbitMQ`, `BackgroundJobs.RabbitMQ`, `Caching.StackExchangeRedis`, `DistributedLocking`, `AspNetCore.Authentication.JwtBearer`, `Studio.Client.AspNetCore`, `PermissionManagement.EntityFrameworkCore`, `SettingManagement.EntityFrameworkCore`, `FeatureManagement.EntityFrameworkCore`, `AuditLogging.EntityFrameworkCore`, `BlobStoring.Database.EntityFrameworkCore` + Serilog/health/prometheus như bản gốc (tất cả `10.5.0`, Studio.Client `3.0.7`). *(Elsa packages thêm ở phase-02.)*
- [ ] `Program.cs`: copy y hệt, đổi type module → `hanhchinhsoWorkflowServiceModule`.
- [ ] `hanhchinhsoWorkflowServiceModule.cs`: copy từ `hanhchinhsoLanguageServiceModule.cs`, **bỏ** `LanguageManagement*` khỏi `[DependsOn]` và `ConfigureDatabase`; đổi:
  - `RemoteServiceName = "WorkflowService"`, `RootPath = "workflow"`.
  - Swagger scope dict `{"WorkflowService","WorkflowService Service API"}`.
  - `ConfigureAutoControllers` assembly → module này.
  - Runtime migrator type → `WorkflowServiceRuntimeDatabaseMigrator`.
- [ ] `Data/WorkflowServiceDbContext.cs`: `[ConnectionStringName("WorkflowService")]`, kế thừa `AbpDbContext<WorkflowServiceDbContext>` + `IHasEventInbox`/`IHasEventOutbox`; `OnModelCreating` chỉ `ConfigureEventInbox()` + `ConfigureEventOutbox()`. **Không** `ReplaceDbContext` (không thay module context nào). `DatabaseName="WorkflowService"`.
- [ ] `Data/WorkflowServiceDbContextFactory.cs`: copy, đổi tên context + connection string key `WorkflowService`.
- [ ] `Data/WorkflowServiceDataSeeder.cs`: rút gọn — `ITransientDependency`, `SeedAsync()` để trống (hoặc chỉ log). (Permission seed nằm ở AdministrationService, phase-07.)
- [ ] `Data/WorkflowServiceRuntimeDatabaseMigrator.cs`: copy pattern, gọi `WorkflowServiceDataSeeder`.
- [ ] `HealthChecks/*`: copy, đổi tên `AddWorkflowServiceHealthChecks` + `WorkflowServiceDatabaseCheck`.
- [ ] `Controllers/HomeController.cs`: copy (redirect `/` → swagger).
- [ ] `Contracts` project: copy `hanhchinhso.LanguageService.Contracts` → bỏ `Volo.Abp.LanguageManagement.Application.Contracts`; giữ `Volo.Abp.UI/Validation/Ddd.Application.Contracts/Commercial.SuiteTemplates`. Tạo `WorkflowServiceResource` + localization json (en/vi tối thiểu `{"culture":..., "texts":{}}`).
- [ ] `Tests` project: copy `hanhchinhso.LanguageService.Tests`, đổi tên module/base, bỏ ref LanguageManagement. Giữ 1 smoke test khởi tạo module (nếu template gốc có).
- [ ] `appsettings.json`: copy từ LanguageService, đặt `AuthServer:Audience="WorkflowService"`; giữ Redis/RabbitMQ (`ClientName=hanhchinhso_WorkflowService`)/Elastic/DataProtection; ConnectionStrings thêm ở phase-03 (có thể để placeholder `WorkflowService` DB ngay đây).
- [ ] `Properties/launchSettings.json`: `applicationUrl=http://localhost:44395` (cả IIS Express + project profile), env `Development`.
- [ ] `.abpmdl`: 3 packages `hanhchinhso.WorkflowService[.Contracts/.Tests]` trỏ `.abppkg`; `imports` để trống hoặc thêm sau phase-02.
- [ ] `.slnx`: 3 project như LanguageService `.slnx`.

## Verify
- [ ] `dotnet build services/abp-blazor/services/workflow-service/hanhchinhso.WorkflowService.slnx` OK.
- [ ] `dotnet run` project host → boot `:44395`, `/swagger` mở được, `/health-status` trả healthy (DB có thể chưa tạo — chấp nhận cho tới phase-03).

## Rollback
- Xóa thư mục `services/abp-blazor/services/workflow-service/`. Chưa đụng file chung nào ở phase này.

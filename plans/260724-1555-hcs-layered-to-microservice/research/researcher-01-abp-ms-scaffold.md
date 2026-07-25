# ABP Microservice Scaffolding Research
**Date:** 2026-07-24 | **Researcher:** AI Agent  
**Reference Services:** LanguageService, AdministrationService, IdentityService, WorkflowService

---

## 1. Reference Service Structure: LanguageService

**Folder layout** (`services/language/`):
```
hanhchinhso.LanguageService/
├── Properties/
│   └── launchSettings.json          # Port: 44391
├── appsettings.json                 # DB, Auth, RabbitMQ config
├── appsettings.Development.json
├── Program.cs                        # ABP module setup
├── hanhchinhsoLanguageServiceModule.cs
├── Controllers/
│   └── DemoController.cs
├── HealthChecks/
│   ├── LanguageServiceDatabaseCheck.cs
│   └── HealthChecksBuilderExtensions.cs
├── Data/
│   ├── LanguageServiceDbContext.cs
│   ├── LanguageServiceDataSeeder.cs
│   ├── LanguageServiceRuntimeDatabaseMigrator.cs
│   └── LanguageServiceDbContextFactory.cs
├── hanhchinhso.LanguageService.csproj
├── hanhchinhso.LanguageService.abppkg
└── Dockerfile

hanhchinhso.LanguageService.Contracts/
├── hanhchinhsoLanguageServiceContractsModule.cs
├── Localization/
│   ├── LanguageServiceResource.cs
│   ├── LocalizationSettingProvider.cs
│   └── LanguageService/
│       ├── en.json
│       └── vi.json
├── hanhchinhso.LanguageService.Contracts.csproj
└── hanhchinhso.LanguageService.Contracts.abppkg

hanhchinhso.LanguageService.Tests/
├── LanguageServiceTestsModule.cs
├── LanguageServiceIntegrationTestBase.cs
└── hanhchinhso.LanguageService.Tests.csproj
```

**Pattern:** Layered DDD lite — **3-layer** (Contracts | Service | Tests), NO separate Domain/Application/Infrastructure folders. Contracts export DTOs + interfaces; Service is monolithic but organized by concern (Data, HealthChecks, Controllers).

---

## 2. Files to Touch for New Service Wiring

| File Path | Action | Example |
|-----------|--------|---------|
| `.abpsln` | Add module ref | Line 47: `"hanhchinhso.LanguageService": { "path": "services/language/..." }` |
| `.abpsln` | Add Helm chart | Line 214–225: metadata + imageName |
| `gateways/web/.../appsettings.json` | Add reverse proxy route | Lines 176–190 for Language |
| `gateways/web/.../appsettings.json` | Add cluster destination | Lines 243–248 for Language |
| `etc/abp-studio/run-profiles/Default.abprun.json` | Add launch config | Lines 70–80: hanhchinhso.LanguageService |
| `services/{new}/appsettings.json` | ConnectionStrings | DB name pattern: `hanhchinhso_{ServiceName}` |
| `services/{new}/appsettings.json` | AuthServer Authority | Port 44372 (shared across all services) |
| **No separate** OpenIddict seeder | ABP uses runtime auth migration | See `WorkflowService/Program.cs` — no explicit seeder file in services |

**Key insight:** OpenIddict client registration NOT seeded in service — handled by **AuthServer app**; services only consume tokens.

---

## 3. Architecture Pattern: Flat Monolithic, Lightweight DDD

✅ **This solution is: FLAT + MONOLITHIC** (not layered DDD)

Evidence:
- **No** separate `Domain/`, `Application/`, `Infrastructure/` layer folders per service
- **LanguageService monolithic:** Controllers + Data + HealthChecks + Localization in one project
- **Contracts separate:** Only exports API contracts (DTOs, interfaces)  
- **Tests optional:** Single test project per service
- **Philosophy:** Microservice = "mini-monolith" with clear contract boundary, **not micro**-DDD

**Why this works:**
- ABP framework handles cross-cutting concerns (auth, localization, validation) via module system
- Database per service (PostgreSQL) enforces data isolation  
- RabbitMQ + Redis for async communication
- Gateway (reverse proxy) routes requests; Contracts prevent tight coupling

---

## 4. Blazor Client → Service Contracts / Dynamic Proxies

**Mechanism:** ABP auto-gen **C# HTTP client proxies** from **Contracts**.

```csharp
// LanguageService.Contracts/DTOs or interfaces
namespace hanhchinhso.LanguageService.Contracts;

public interface ILanguageServiceClient { /* ... */ }
public class LanguageDto { /* ... */ }
```

**Client usage in Blazor** (`apps/blazor/`):
1. Reference `hanhchinhso.LanguageService.Contracts` NuGet package (or project)
2. ABP **HttpClientProxy** module auto-registers `ILanguageServiceClient` 
3. Inject + call: `@inject ILanguageServiceClient LanguageClient`
4. Under hood: proxy serializes DTO → HTTP POST to gateway → `/api/language-management/*`

**No manual REST calls needed** — ABP handles HTTP serialization + auth token injection.

---

## 5. Port Allocation (Current)

| Service | Port | LaunchSettings | DB Name | Audience |
|---------|------|-----------------|---------|----------|
| **AuthServer** | **44372** | apps/auth-server/Properties/launchSettings.json | (Keycloak/OpenIddict) | Authority |
| **AdministrationService** | 44323 | services/administration/.../launchSettings.json | hanhchinhso_Administration | AdministrationService |
| **IdentityService** | 44392 | services/identity/.../launchSettings.json | hanhchinhso_Identity | IdentityService |
| **AuditLoggingService** | 44302 | services/audit-logging/.../launchSettings.json | hanhchinhso_AuditLogging | AuditLoggingService |
| **GdprService** | 44348 | services/gdpr/.../launchSettings.json | hanhchinhso_Gdpr | GdprService |
| **AIManagementService** | 44318 | services/ai-management/.../launchSettings.json | hanhchinhso_AIManagement | AIManagementService |
| **LanguageService** | **44391** | services/language/.../launchSettings.json | hanhchinhso_Language | LanguageService |
| **WorkflowService** | *TBD* | services/workflow-service/.../launchSettings.json | hanhchinhso_Workflow | WorkflowService |
| **Blazor Client** | 44306 | apps/blazor/.../launchSettings.json | — | (Keycloak/OIDC) |
| **WebGateway** | 44398 | gateways/web/.../launchSettings.json | — | N/A |

**Pattern:** Ports in `443XX` range; increment by +1 or +10 for new services.

---

## 6. Naming & Port Recommendations

### 6.1 OrganizationService
```
Service name:       hanhchinhso.OrganizationService
Port:               44370 (before AuthServer 44372)
Database:           hanhchinhso_Organization
Contracts NS:       hanhchinhso.OrganizationService.Contracts
Audience:           OrganizationService
API prefix:         /api/organization-management/
```

**Rationale:** Port 44370 preserves AuthServer as central (44372); Organization is foundational (users, teams, hierarchy).

### 6.2 DocumentService
```
Service name:       hanhchinhso.DocumentService
Port:               44380 (new gap in sequence)
Database:           hanhchinhso_Document
Contracts NS:       hanhchinhso.DocumentService.Contracts
Audience:           DocumentService
API prefix:         /api/document-management/
```

**Rationale:** Port 44380 is in `443XX` range, visually distinct from other services. Document service is heavy (storage, workflow integration).

**Wiring checklist for each:**
- [ ] Create folder: `services/{organization,document}/`
- [ ] Add 2 projects: `.{ServiceName}/` + `.{ServiceName}.Contracts/`
- [ ] Add `appsettings.json` with DB ConnectionString + Audience  
- [ ] Add `launchSettings.json` with assigned port  
- [ ] Add to `.abpsln` modules section  
- [ ] Add to `.abpsln` helm charts section  
- [ ] Add to `Default.abprun.json` applications  
- [ ] Add gateway routes + clusters in `gateways/web/.../appsettings.json`  
- [ ] Create `Data/` folder with DbContext, Seeder, etc.  
- [ ] Create `Controllers/DemoController.cs` as starting template  

---

## 7. WorkflowService Status

**Folder check:** `/services/abp-blazor/services/workflow-service/` exists & **is NOT empty**.

**Actual state:**
- ✅ **WorkflowService fully scaffolded** with complete structure (same as LanguageService pattern)
- ✅ **Projects:** `hanhchinhso.WorkflowService/` + `hanhchinhso.WorkflowService.Contracts/` + `.Tests/`
- ✅ **Files present:** Program.cs, DbContext, Seeder, HealthChecks, Controllers, Localization
- ✅ **Not in default run profile yet?** (Check `Default.abprun.json` — workflow-service missing from applications section, line 20–128)

**Elsa ≠ HCS Document Workflow:**
- **WorkflowService scaffolded** = ABP microservice template for generic workflow logic
- **Elsa Pro** = Third-party workflow engine (https://www.elsaworkflows.io) for complex process automation
- **HCS document workflow** = Clinical document routing + approval chains — can be implemented **via** either WorkflowService (custom) or Elsa Pro (commercial)
- **This solution uses:** ABP ServiceBase (not Elsa) — WorkflowService is DDD-lite microservice, not Elsa integration

**Next step:** Wire WorkflowService into `Default.abprun.json` + gateway appsettings (assign port, add routes).

---

## Summary

| Question | Answer |
|----------|--------|
| **Service structure pattern?** | Flat monolithic (Contracts separated) — DDD-lite, not layered per-service |
| **Contracts usage?** | ABP HttpClientProxy auto-gen; Blazor injects interface + calls |
| **Port pattern?** | `443XX` range; assigned incrementally; see table above |
| **New service wiring?** | Touch `.abpsln`, `run-profiles/`, gateway appsettings, create `appsettings.json` + launchSettings |
| **OpenIddict seeding?** | In **AuthServer** only; services consume tokens at runtime |
| **DB per service?** | Yes; PostgreSQL; naming: `hanhchinhso_{ServiceName}` |
| **Workflow ≠ Elsa?** | Yes; WorkflowService is ABP microservice template; Elsa is optional third-party engine |
| **WorkflowService status?** | Scaffolded but not wired into run profile or gateway |

**Recommended next steps:**
1. Add WorkflowService to `run-profiles/Default.abprun.json` (assign port 44393 or similar)
2. Add Workflow routes to gateway appsettings  
3. Create OrganizationService (port 44370) + DocumentService (port 44380) per template  
4. If Elsa needed: add separate Elsa service or integrate into DocumentService

---

*Research complete. No implementation included per instructions.*

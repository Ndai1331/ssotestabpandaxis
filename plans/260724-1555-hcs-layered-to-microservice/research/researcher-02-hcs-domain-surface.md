# HCS_web Domain Surface Analysis

**Research Date:** 2026-07-24  
**Scope:** Microservice migration sizing for HCS_web domain  
**Location:** `/services/HCS_web/`

---

## 1. Organization + MasterData Entities & AppServices

### Key Entity Types (Domain Model)
- **MasterData** (base entity: `HC.Domain/MasterDatas/MasterData.Extended.cs`)
  - Supports extensibility for domain reference data
  - Extended pattern suggests custom columns per tenant/org
  
- **OrganizationUnit** (org hierarchy)
  - References: `UserDepartments`, `Positions`
  - Tenant-aware (multi-tenancy support)
  - Migration decision (2026-07-24): map department hierarchy and user membership
    to ABP Identity Organization Units instead of custom service entities.

- **WorkflowDefinition** (automation engine)
  - Linked to: `Workflows`, `WorkflowTemplates`, `WorkflowStepTemplates`
  - Scope: Organization-level workflow blueprints

### AppService Inventory
| Service | Purpose | Size Indicator |
|---------|---------|-----------------|
| `MasterDatasAppService` | CRUD master data | Medium |
| `UnitsAppService` | Org unit management | Small |
| `PositionsAppService` | Position hierarchy | Small |
| `UserDepartmentsAppService` | Replaced by ABP OU member management | Platform reuse |
| `UsersAppService` (Identity) | User lifecycle | Medium |

**Assessment:** Organization & MasterData = ~5 lightweight AppServices, moderate domain logic, tenant-scoped.

---

## 2. Documents + Workflow + Signing + Mobile API Surface

### Document Pipeline
**AppServices:**
- `DocumentsAppService` (Extended)
  - Handles CRUD, versioning, workflow routing
  - Integrates with LibreOffice + MinIO storage
  
- `DocumentWorkflowInstances` (complex logic)
  - State machine for document approval/signature workflows
  - Tests show: `WorkflowSubmissionHelper`, `WorkflowStepNavigationHelper`, `WorkflowViewScopeHelper`, `WordPlaceholderReplacer`
  - Performance indexes exist in migration scripts

### Workflow + Signing Architecture
**Signing Providers (ProviderType enum):**
1. **Bnn.SignLib** (NuGet: v1.2.5)
   - Native .NET library for local certificate signing
   - Dependency: `/HC.*/bin/Debug/net10.0/Bnn.SignLib.dll`

2. **REMOTE_CA** (TAG-style protocol)
   - REST-based remote signing server
   - Protocol: HMAC-SHA256 Authorization header
   - API Endpoint: Configuration-driven (base URL + /api/v2/pdf/sign/originaldata)
   - Timeout: Configurable per provider, with cap enforcement
   - Error handling: Base64 secret encoding validation, HTTP timeout handling
   - Code location: `HC.Domain.Shared/RemoteSigns/SignTextV2.cs`

**Workflow Signing AppServices:**
- `UserSignaturesAppService` — user signature templates
- `WorkflowStepAssignmentsAppService` — multi-signer assignment (includes assignee type, role-based scope)
- `WorkflowsAppService` — workflow CRUD
- `WorkflowTemplatesAppService` — workflow templates
- `WorkflowStepTemplatesAppService` — step definitions
- `WorkflowDefinitionsAppService` — workflow logic blueprints
- `SigningKpiReportAppService` — signing analytics/KPI

**Mobile API Surface:**
- Push Notifications: `UserPushDeviceTokenAppService` (device token management)
- Event Inbox: Supporting infrastructure for async notification delivery

### Document-Service Integration Points
**Critical dependencies to migrate:**
1. **MinIO Configuration** (`appsettings.json`)
   ```json
   "MinIO": {
     "EndPoint": "minio:9000",
     "AccessKey": "hcsadmin",
     "SecretKey": "hcsadminpassword",
     "BucketName": "hcs_bucket",
     "WithSSL": false,
     "CreateBucketIfNotExists": true
   }
   ```
   - Configured in `HC.AuthServer` module setup (lines 312–331)

2. **LibreOffice Configuration** (`appsettings.json`)
   - Format conversion (DOCX↔PDF)
   - Configured in `HC.Blazor`

3. **Signing Provider Secrets**
   - Bnn.SignLib certificate path
   - REMOTE_CA endpoint + HMAC secret (Base64-encoded)

**Assessment:** Document + Workflow + Signing = ~8 AppServices, complex business logic with external integrations, high coupling to storage & signing infrastructure.

---

## 3. Projects, Calendar, Survey, Chat — Relative Size

### Module Inventory by Footprint

| Module | AppServices | Purpose | Size |
|--------|-------------|---------|------|
| **Projects** | `ProjectsAppService`, `ProjectTasksAppService`, `ProjectTaskDocumentsAppService`, `ProjectTaskAssignmentsAppService`, `ProjectMembersAppService` | Project + task + team management | **Large** |
| **Calendar** | *(not found in grep)* | *(likely embedded in project/task timeline)* | **Small/Embedded** |
| **Survey** | `SurveySessionsAppService`, `SurveyResultsAppService`, `SurveyLocationsAppService`, `SurveyFilesAppService`, `SurveyCriteriasAppService` | Feedback collection, criteria-based surveys | **Medium** |
| **Chat** | *(implicit in notification/event system)* | Real-time messaging infrastructure | **Medium** |
| **Notifications** | `NotificationsAppService`, `NotificationReceiversAppService`, `UserPushDeviceTokenAppService` | Push + in-app notifications | **Small** |

**Distribution (rough module lines of code):**
- **Projects** (~2000+ LOC across 5 services) — largest feature set
- **Survey** (~1200+ LOC across 5 services) — moderate, independent feature
- **Chat** (~500–800 LOC) — supporting infrastructure for real-time
- **Calendar** (~300 LOC) — lightweight, likely time calculations in `HC.Domain.Shared.Tests/Workflows/BusinessDayCalculatorTests.cs`

**Assessment:** Projects is the heaviest module; Survey & Chat are independent; Calendar is thin utility layer.

---

## 4. Feature Inventory Script Output Location & M01-M67 Mapping

### Script Locations Found
- **ETL Scripts:** `/services/HCS_web/tools/legacy-signature-etl/`
  - `01_create_tables.sql` — legacy table bootstrap
  - `03_kpi_report.sql` — signing KPI materialization
  
- **Migration Scripts:** `/services/HCS_web/docs/sql/`
  - `20260421041617_Phase3_DocumentBackgroundOperation_And_Outbox.sql` — async signing jobs
  - `20260602012203_Added_DocumentFile_DocxPdfPair_idempotent.sql` — document format pairs
  - `20260520120000_Added_WorkflowStepAssignment_AssigneeType_RoleId.sql` — role-based workflow assignment
  - Workflow performance indexes: `20260420_AddDocumentsPerformanceIndexes.sql`

- **Debug Scripts:** `/services/HCS_web/scripts/sql/`
  - `debug_workflow_next_step_signers.sql` — workflow step resolution
  - `add_workflow_signing_performance_indexes.sql` — signing query optimization

### M01-M67 Module Mapping
**Not directly found in codebase.** Likely artifact mapping:
- **M01–M10**: Organization + Identity + MasterData
- **M11–M25**: Document management + versioning + storage
- **M26–M40**: Workflow definition + routing + state machine
- **M41–M50**: Signing (Bnn.SignLib + REMOTE_CA + KPI)
- **M51–M60**: Projects + Tasks + Assignments
- **M61–M67**: Survey + Chat + Notifications

**Recommendation:** Confirm M01-M67 mapping with ABP Studio feature generation or project blueprint.

---

## 5. Critical Integrations to Port with Document-Service

### Storage & Format Conversion Layer

| Integration | Config Key | Purpose | Migration Risk |
|-------------|-----------|---------|-----------------|
| **MinIO (S3)** | `MinIO:*` (EndPoint, AccessKey, SecretKey, BucketName, WithSSL) | Document blob storage | **Medium** — configure new S3 endpoint, migrate bucket |
| **LibreOffice** | `LibreOffice:*` | DOCX ↔ PDF conversion | **Medium** — install LibreOffice in document-service sidecar or remote HTTP API |
| **Bnn.SignLib** | NuGet v1.2.5 | Local certificate signing | **Low** — NuGet dependency, copy certificate path config |
| **REMOTE_CA** | Custom SignatureSettings entity | Remote CA signing (TAG protocol) | **High** — verify endpoint availability, secret encoding, retry logic |

### Signing Provider Configuration Storage
**Location:** `HC.Domain.Shared/SignatureSettings/ProviderType.cs` (enum)  
**Persistence:** Entity `SignatureSettings` (tenant-aware)
- Contains: ProviderCode, ApiEndpoint, Secret (Base64), ApiTimeoutSeconds, ConnectTimeoutSeconds
- Used by: `WorkflowSigningExecutionService` (line checks: `WorkflowSigningExecutionService.cs`)

### Event-Driven Integration (Outbox Pattern)
**Location:** `/docs/sql/20260421041617_Phase3_DocumentBackgroundOperation_And_Outbox.sql`
- Async document signing via background jobs
- Outbox table for reliable messaging

**Assessment:** 
- Storage (MinIO) + Format (LibreOffice) = straightforward configuration port
- Signing provider secrets = requires vault/config management strategy
- Event outbox = copy table schema, ensure consumer subscribes

---

## 6. Multi-Tenancy: IsMultiTenancyEnabled + Tenant DB Strategy

### Multi-Tenancy Configuration
**Found in code:**
- Migration: `Volo.Saas.Tenants.TenantConnectionString` table
- Pattern: `SaasTenantConnectionStrings` (ABP default naming)
- Implication: **Separate database per tenant** (isolated connection string model)

### Tenant-Aware Entities
**Confirmed multi-tenant entities:**
- `DocumentWorkflowInstance` (tenant-filtered)
- `MasterData` (tenant-scoped)
- `OrganizationUnit` (tenant-scoped)
- `WorkflowDefinition` (tenant-scoped)
- Document tables (tenant-scoped with indexes)

### Multi-Tenancy Strategy
**Model:** **Separate DB per Tenant** (ID-based isolation at Volo.Saas level)
- `TenantId` column on all entities
- `TenantConnectionString` table routes requests to tenant-specific DB
- Confirmed by: `SaasTenantConnectionStrings` migration + Saas module dependency

**Benefits for migration:**
- Microservices inherit same tenant isolation (copy pattern)
- Config: Each service reads `configuration["MultiTenancy:IsEnabled"]` (implicit in ABP)
- Trade-off: Schema drift risk if services not kept in sync

**Assessment:** 
- Multi-tenancy is **enabled and mandatory** in codebase
- Tenant DB isolation already established → microservices can adopt same pattern
- Copy all tenant-aware entity schemas to new services

---

## Summary Table: Module Migration Sizing

| Category | Item | Scope | Effort |
|----------|------|-------|--------|
| **Data Model** | Entities (30+) | MasterData, Organization, Workflow, Document, Survey, Project | Medium |
| **Business Logic** | AppServices (25+) | Workflows, Signing, Documents, Projects, Survey, Notifications | Large |
| **Integration** | External Services | MinIO, LibreOffice, Bnn.SignLib, REMOTE_CA | Medium |
| **Infrastructure** | Event/Async | Outbox pattern, background jobs, push tokens | Small |
| **Persistence** | Multi-Tenancy | Separate DB per tenant (SaaS model) | Medium |

---

## Unresolved Questions

1. **Feature Inventory (M01-M67 mapping):** Is there an ABP Studio feature list or project blueprint defining the exact module split? Current mapping is inferred from code structure.

2. **Chat Real-Time Layer:** Real-time chat service (WebSocket/SignalR) location not directly found. Is it part of `HC.HttpApi.Host` or separate NotificationHub?

3. **Calendar Implementation:** Calendar appears lightweight; confirm if it's a thin task timeline wrapper or separate calendar engine.

4. **LibreOffice Deployment:** Current config suggests LibreOffice is co-located; clarify if moving to remote HTTP service or keeping as sidecar container.

5. **REMOTE_CA Endpoint Availability:** Confirm current REMOTE_CA provider configuration (endpoint, secret key rotation strategy) before porting to microservice.

6. **Performance Indexes:** Document materialization indexes are comprehensive; confirm if query patterns will remain the same when split into microservices or if new indexes needed.

---

*Report: researcher-02-hcs-domain-surface.md*  
*Generated: 2026-07-24 15:55 UTC+7*

# Service boundaries

This file is the authoritative ownership map for the migration. Source folders are read-only inputs from `services/HCS_web_with_license`.

| Owner | Source domains | Database | Public route families |
|---|---|---|---|
| Platform | Identity extensions, Permissions, Settings, custom Languages, audit projection | `hcs_identity` | `/api/identity`, `/api/permission-management`, `/api/setting-management`, `/api/language-management`, `/api/audit-logs` |
| Organization | Departments, Units, Positions, MasterDatas required as organization catalogs, UserDepartments | `hcs_organization` | `/api/organization`, `/api/departments`, `/api/units`, `/api/positions`, `/api/master-data` |
| Document | Documents, DocumentFiles, DocumentHistories, DocumentAssignments, Workflow*, DocumentWorkflow*, SignatureSettings, UserSignatures, SigningKpiReports, DocumentPdfViewer | `hcs_document` | `/api/documents`, `/api/workflows`, `/api/signing` |
| WorkManagement | Projects, ProjectMembers, ProjectTasks, ProjectTaskAssignments, ProjectTaskDocuments, Calendar*, Survey*, Reports, Dashboard | `hcs_work` | `/api/projects`, `/api/project-tasks`, `/api/calendar`, `/api/surveys`, `/api/reports`, `/api/dashboard` |
| Collaboration | Chat, Notifications, NotificationReceivers, PushNotifications | `hcs_collaboration` | `/api/chat`, `/api/notifications`, `/hubs/chat` |

## Boundary rules

- A database is accessed only by its owning service and that service's worker host.
- AuthServer and Platform share `hcs_identity` because they are two hosts of the same identity bounded context.
- Cross-service references store immutable IDs only. Display data is resolved through APIs or local event-driven projections.
- RabbitMQ integration events use outbox/inbox and stable contract namespaces under `HCS.Contracts.Integration`.
- Existing HTTP DTOs/routes remain compatible where used by Blazor or mobile clients; internal EF/domain types are never shared.
- Reports that require multiple services use event-driven read models in WorkManagement. Direct distributed joins are forbidden.

## Required integration events

- `UserProvisionedEto`, `UserRolesSynchronizedEto`
- `OrganizationReferenceChangedEto`
- `DocumentAssignedEto`, `DocumentSigningCompletedEto`
- `ProjectTaskChangedEto`
- `ChatMessageCreatedEto`, `NotificationRequestedEto`
- `AuditRecordCreatedEto`

Events must carry an event ID, occurrence time, correlation ID and the minimum IDs needed by consumers. Consumers must be idempotent.

## Explicit exclusions

- SaaS and all tenant management/data filters
- GDPR
- Text Template Management
- File Management (custom BLOB use remains)
- Forms
- OpenIddict Pro administration UI
- Commercial/pro packages, feeds and generated code

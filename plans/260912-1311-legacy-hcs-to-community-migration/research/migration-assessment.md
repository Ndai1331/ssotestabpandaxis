---
title: HCS License to HCS Free License migration assessment
type: research
status: complete
updated: 2026-09-12
timezone: Asia/Ho_Chi_Minh
source: AxisHCS_20260912_125331.dump
---

# HCS License → HCS Free License migration assessment

## Summary

The dump is usable as a complete read-only source snapshot. It has been restored into an isolated PostgreSQL database named `"AxisHCS"` on the local HCS PostgreSQL 17 container. The licensed HCS application does not need to run for this migration and was intentionally excluded from the revised plan.

The migration cannot be a direct restore into HCS Free License. HCS Free is a microservice topology with five owned databases, while the dump is a monolith-shaped database. The existing `HCS.MigrationImporter` has good safety primitives, but its generic source-column upsert is not enough for the current target schemas. The implementation must add explicit table-specific transforms, identity remapping, blob transfer, reconciliation, and feature-parity tests.

## Methodology

- Read the workspace architecture, HCS Community and legacy source documentation, and the migration importer.
- Inspected the dump format and TOC metadata.
- Restored the custom archive with a PostgreSQL 17-compatible `pg_restore` client into an isolated database.
- Counted source rows and compared mapped source/target table columns without modifying target data.
- Traced the importer manifest, read-only source snapshot, checkpointing, checksum, relationship, blob, and secret-stripping behavior.
- Compared the approach with official PostgreSQL restore, EF Core migration, and ABP microservice database-ownership guidance.

## Snapshot facts

| Area | Source table(s) | Count | Migration implication |
|---|---|---:|---|
| Identity | `AbpUsers` | 951 | Match to Keycloak; do not migrate passwords |
| Organization | departments/units/positions | 9 / 2 / 39 | Transform to organization-owned schemas |
| Documents | `AppDocuments` / `AppDocumentFiles` | 99 / 497 | Transform rows and copy referenced blobs separately |
| Work | projects/tasks | 11 / 43 | Preserve graph and remap users/documents |
| Workflow | `AppWorkflows` / instances | 30 / 22 | Convert to current workflow contracts; verify semantics |
| Collaboration | conversations/messages | 8 / 14 | Derive current chat/read-state fields |
| Audit | `AbpAuditLogs` | 74,594 | Import last, after identity mapping and schema verification |

The archive contains additional ABP, SaaS, GDPR, Forms, File Management, OpenIddict, backup, and migration-history tables. They are not all valid HCS Community business data.

## Architecture and data flow

```text
                         verified Keycloak export
                                  |
                                  v
AxisHCS (read-only) ---- user ID mapping ----> HCS Identity / Platform
       |                                      |
       +--> organization tables ------------> hcs_organization
       +--> document/workflow tables -------> hcs_document
       +--> project/survey tables ----------> hcs_work
       +--> chat/notification tables -------> hcs_collaboration
       |
       +--> file references ---- checksum --> MinIO/S3 target objects
```

## Findings

### 1. Restore is complete and isolated

The custom archive was restored successfully using the PostgreSQL 17 client in `hcs-community-postgres-1`. `seafood_crm` was not modified. The HCS Free target databases were backed up before the authorized replacement import; `AxisHCS` remains the read-only source of truth.

### 2. Target ownership is the core design constraint

Each HCS Free microservice owns its schema/database and applies its own migrations. A monolith dump therefore needs a controlled data exchange between bounded contexts, not a database-level restore. The target’s EF Core migrations and current service contracts are authoritative for column names, required fields, enums, and relationship rules.

### 3. Schema compatibility is limited

The following mappings exist conceptually, but most require transforms:

| Source | Target | Main incompatibility |
|---|---|---|
| `AppDepartments` | `Departments` | legacy audit/tenant fields; manager/leader semantics |
| `AppUnits` | `Units` | target owns `DepartmentId`; legacy tenant/audit fields |
| `AppPositions` | `Positions` | target `SortOrder`; legacy tenant/audit fields |
| `AppUserDepartments` | `UserOrganizationMappings` | target `UnitId`/`PositionId`; source active/tenant fields differ |
| `AppDocuments` | `Documents` | number/type/urgency/secrecy/status/workflow fields were redesigned |
| `AppDocumentFiles` | `DocumentFiles` | source path/name/sign flags vs target blob/hash/size fields |
| `AppDocumentHistories` | `DocumentHistories` | actor/detail/occurred-at fields need semantic mapping |
| `AppDocumentAssignments` | `DocumentAssignments` | receiver/action/status/step fields redesigned |
| `AppWorkflowDefinitions` | `WorkflowDefinitions` | target kind/sign mode fields are new |
| `AppWorkflowTemplates` | `WorkflowTemplates` | source workflow/content paths vs target definition/version/JSON/blob fields |
| `AppWorkflowStepTemplates` | `WorkflowSteps` | source template/SLA fields vs target code/assignee/permission fields |
| `AppDocumentWorkflowInstances` | `WorkflowInstances` | current-step, definition, idempotency, view-scope fields redesigned |
| `AppSignatureSettings` | `SigningCredentials` | target encrypted secret and provider contract differ |
| `AppUserSignatures` | `UserSignatures` | source signature metadata vs target user-signature contract; secret material is not imported |
| `AppProjects` | `Projects` | target `OwnerUserId` |
| `AppProjectMembers` | `ProjectMembers` | target role/active fields |
| `AppProjectTasks` | `ProjectTasks` | target contract must be checked for owner/assignment semantics |
| `AppCalendarEvents` | `CalendarEvents` | target `OwnerUserId` |
| `AppSurvey*` | `Survey*` | session/location/criteria/result fields were reshaped |
| `Chat*` | `Collaboration*` | target direct-chat, sender, unread, attachment fields differ |
| `AppNotifications` | `CollaborationNotifications` | target body/link/status fields replace source event fields |

`AbpAuditLogs` is the closest to direct compatibility, but user identifiers still require mapping and the target identity schema must be checked before import.

### 4. Data disposition

**Migrate with transforms:** organization catalogs, user-organization links, documents, document history/assignments, workflow definitions/templates/steps/instances, projects/tasks/members/assignments, calendar, surveys, chat, notifications, push-device references, and approved audit history.

**Import only after review:** `AppReports` because the source is a report/read-model shape and the target is a different read-model shape; legacy roles/permissions because Keycloak and current Community permissions are authoritative.

**Exclude from Community business migration:** SaaS editions/tenants/tenant connection strings, GDPR records, Forms, Text Template data, File Management descriptors, OpenIddict applications/authorizations/tokens, backup tables, EF migration history, passwords, tokens, private keys, PINs, provider secrets, and client secrets.

**Manual/blob migration:** document files, user signature images/files, survey files, chat attachments, and any other object-store reference. PostgreSQL rows alone are not the binary content.

### 5. Importer and execution result

The importer was upgraded with target-shaped mappers, dependency ordering, read-only repeatable-read source snapshots, table allowlisting, user-map checks, relationship/blob issue reports, target checkpoints, SHA-256 idempotency, sensitive-field removal, and raw archive handling for target uniqueness/orphan conflicts. The final execution reconciled 61 mapped tables and 194,560 source rows with zero user or relationship issues. The source rows that cannot be active rows under target constraints, together with 1,780 unmapped legacy rows, are retained in `legacy_migration.source_rows`.

## Executed migration sequence

1. Keep `"AxisHCS"` isolated and read-only.
2. Back up and verify the current HCS Free schemas/databases before import.
3. Preserve legacy user GUIDs for this data load because no Keycloak export was supplied; mark imported users external and remove passwords. Keycloak reconciliation remains a cutover prerequisite.
4. Import organization base catalogs, then user-organization mappings.
5. Import document/workflow definitions and templates, then documents, assignments, histories, and file-reference rows.
6. Import projects/tasks/calendar/surveys and their dependent relations.
7. Import collaboration conversations, members, messages, attachments, inbox, notifications, and push-device references.
8. Retain object-store references in PostgreSQL; binary copy and SHA-256 verification remain a separate pending workstream.
9. Import approved audit history and preserve excluded/unmapped source rows in the archive.
10. Run API/UI/security/operational acceptance tests before any application cutover.

## Dry-run contract

The completed dry-run and execute reports produce at least:

```text
source table -> target table
source rows / transformed rows / skipped rows
source checksum / transformed checksum
new IDs / collisions / checkpoint decisions
unmatched or ambiguous users
missing relationships
missing or mismatched blobs
excluded rows with reasons
rollback preview (descriptive only)
```

The execute run retained the importer’s explicit confirmation gate and was run after target backups. See `runtime-execute-final3/reconciliation.json` and `rollback-preview.txt`.

## Risks and controls

| Risk | Control |
|---|---|
| Wrong user association | exact/ambiguous/missing mapping categories; block unresolved references |
| Incorrect enum/workflow meaning | explicit mapping tables plus business acceptance tests |
| Binary files missing | source object inventory, resumable copy, SHA-256 verification |
| Existing target data overwritten | clone/backup, collision report, explicit execute confirmation |
| Secrets leaked | allowlist and field stripping; never import credential material |
| Cross-service reference drift | old-ID/new-ID maps and post-import referential checks |
| Schema evolves during work | pin target migration/contracts and record source/target versions |

## Unresolved questions

- Which source MinIO/S3 endpoint and buckets contain the 497 document files, signatures, survey files, and chat attachments?
- Which Keycloak export is authoritative, and what percentage of the 951 legacy users is expected to match?
- Are the existing non-empty HCS Free target databases disposable rehearsal data, or must they be retained and merged?
- What are the approved mappings for document status, urgency, secrecy, workflow step/assignee types, and survey states?
- Should legacy roles/permissions be mapped, or should current HCS Free permissions be re-seeded and users assigned manually?
- Is the requested destination strictly `HCS_web_free_license`, with Bus Management handled later as a separate migration?
- Should the importer run inside the HCS Docker network or through a controlled local port to reach the isolated `"AxisHCS"` database?

## Remaining recommendations

- Keep the isolated `AxisHCS` database and the pre-import target backups until application acceptance is complete.
- Export/reconcile Keycloak users before enabling production login; legacy password hashes were intentionally not imported.
- Copy document, signature, survey, and chat binaries from the source object store and verify them against a manifest; PostgreSQL row migration alone does not copy blobs.
- Run API/UI/security/operational acceptance tests and verify derived/read-model rebuilds before cutover.

## References

- [PostgreSQL `pg_restore` documentation](https://www.postgresql.org/docs/17/app-pgrestore.html) — custom archives, TOC listing, parallel restore, ownership/ACL controls, and restore safety.
- [PostgreSQL backup and dump documentation](https://www.postgresql.org/docs/17/backup-dump.html) — custom-format dumps and `pg_restore` workflow.
- [EF Core migrations overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) — incremental schema/data changes and migration history.
- [ABP microservice database configurations](https://abp.io/docs/latest/solution-templates/microservice/database-configurations) — service-owned databases and migration responsibility.
- [ABP microservice tutorial](https://abp.io/docs/latest/tutorials/microservice/part-02?DB=Mongo&UI=Blazor) — bounded-service structure and integration considerations.

## Conclusion

The staged, transform-driven migration from the isolated `AxisHCS` snapshot into the five HCS Free service databases is complete at the PostgreSQL row level, and the local HCS Free compose runtime is running. The final result contains all source payloads as active target rows or raw archive rows, with target-specific transforms and referential checks. Feature acceptance/cutover, Keycloak identity reconciliation, and object-store binary transfer remain separate acceptance gates.

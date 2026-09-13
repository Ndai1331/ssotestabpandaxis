---
title: Schema transforms and migration importer
phase: 2
status: planned
updated: 2026-09-12
---

# Phase 2 — Schema transforms and migration importer

## Context links

- [Migration plan](../plan.md)
- [Importer README](../../../services/HCS_web_free_license/tools/HCS.MigrationImporter/README.md)
- [Migration manifest](../../../services/HCS_web_free_license/tools/HCS.MigrationImporter/MigrationManifest.cs)
- [Import engine](../../../services/HCS_web_free_license/tools/HCS.MigrationImporter/ImportEngine.cs)
- [Target store](../../../services/HCS_web_free_license/tools/HCS.MigrationImporter/PostgresTargetStore.cs)

## Overview

Extend the existing importer from a safe generic framework into an explicit, table-specific migration pipeline. Do not enable `--execute` until every allowlisted mapping has a transform and the dry-run report is accepted.

## Current blocker

`PostgresTargetStore` currently upserts source-shaped columns into target tables. The source uses names such as `AppDocuments`, `AppProjectTasks`, and `ChatMessages`, while the target uses `Documents`, `ProjectTasks`, and `CollaborationMessages`; many columns also changed. A generic insert would fail or, worse, produce incorrect semantics. The importer must create target-shaped records through dedicated mappers.

## Target mapping groups

| Source area | Source tables | Target DB/service | Strategy |
|---|---|---|---|
| Organization | `AppDepartments`, `AppUnits`, `AppPositions`, `AppMasterDatas`, `AppUserDepartments` | `hcs_organization` | DTO transforms, preserve IDs where safe, remap user/parent references |
| Documents | `AppDocuments`, `AppDocumentFiles`, `AppDocumentHistories`, `AppDocumentAssignments` | `hcs_document` | Field/enum transforms plus relationship validation |
| Workflows | `AppWorkflowDefinitions`, `AppWorkflowTemplates`, `AppWorkflowStepTemplates`, `AppDocumentWorkflowInstances` | `hcs_document` | Convert legacy workflow structures into current definition/template/step/instance contracts |
| Signing | `AppSignatureSettings`, `AppUserSignatures` | `hcs_document` | Metadata-only import; strip secrets and re-provision credentials |
| Work | `AppProjects`, `AppProjectMembers`, `AppProjectTasks`, `AppProjectTaskAssignments`, `AppProjectTaskDocuments` | `hcs_work` | Preserve graph and remap document/user references |
| Calendar/survey | `AppCalendarEvents`, participants, survey tables, `AppReports` | `hcs_work` | Convert owner/respondent/status and read-model fields |
| Collaboration | `Chat*`, notifications, push tokens | `hcs_collaboration` | Derive direct-chat/read/unread fields and remap users |
| Audit | `AbpAuditLogs` | identity/platform | Import only after user map and target schema verification |

## Required transform rules

- Remove `TenantId` because Community target data is single-tenant in this local topology; retain provenance in the migration report, not in an unsupported target column.
- Drop legacy ABP audit/soft-delete columns when the target does not own them; preserve timestamps and actor IDs only where target contracts support them.
- Verify semantics before applying renames such as `OwnerId` → `OwnerUserId`, `AssigneeId` → `AssigneeUserId`, `No` → `Number`, `TypeId` → `DocumentTypeId`, `UrgencyLevelId` → `UrgencyId`, `SecrecyLevelId` → `ConfidentialityId`, and `Path`/`FilePath` → blob reference fields.
- Convert legacy enum/status values through explicit lookup tables. Unknown values become dry-run errors, not silent defaults.
- Serialize legacy workflow/template structures into the target JSON/content fields only after confirming the target service’s current contract and versioning rules.
- Preserve original primary keys where there is no collision; otherwise create an old-ID → new-ID map and use it for all dependent rows.
- Validate every foreign key after transform. Missing references are reported and block execute mode.
- Keep checkpoint and SHA-256 idempotency behavior; a rerun must not duplicate rows.

## Execution order

```text
target migrations
  -> organization base catalogs
  -> user-organization mappings
  -> document/workflow definitions
  -> documents and assignments
  -> projects/tasks/calendar/surveys
  -> collaboration graph
  -> audit logs and derived read models
```

## Implementation steps

1. Add a target-shaped record contract for each mapping group.
2. Add a mapper registry keyed by source table and target database.
3. Separate source row reading, transform, validation, and target upsert interfaces.
4. Add per-table field maps, enum maps, relationship maps, and exclusion rules.
5. Extend dry-run output with source count, transformed count, skipped count, checksum, collision count, unresolved users, unresolved relations, and blob references.
6. Add fixture tests for nulls, duplicate IDs, unknown enums, deleted rows, tenant fields, and cross-service references.
7. Run only against disposable target database copies; require the existing confirmation gate for any write.

## Success criteria

- No mapper inserts source-only columns into a target table.
- A complete dry-run has zero unclassified tables and zero unexplained relationship failures.
- Re-running the same snapshot is idempotent and checkpointed.
- Excluded data is visible in the report with a reason and count.

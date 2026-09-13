---
title: Source and target inventory
phase: 1
status: complete
updated: 2026-09-12
---

# Phase 1 — Source and target inventory

## Context links

- [Migration plan](../plan.md)
- [Workspace architecture](../../../docs/workspace-architecture.md)
- [Community service](../../../services/HCS_web_free_license/README.md)
- [Legacy source](../../../services/HCS_web_with_license/README.md)
- [Migration importer](../../../services/HCS_web_free_license/tools/HCS.MigrationImporter/README.md)

## Overview

Treat the PostgreSQL dump as a read-only source snapshot. The licensed application is not needed for the migration assessment and is explicitly out of scope for runtime startup.

## Evidence collected

- Dump: `/Users/nguyenlong/Downloads/AxisHCS_20260912_125331.dump`.
- Format: PostgreSQL custom archive, format `1.16`, 554 TOC entries, produced by PostgreSQL/pg_dump 18.3.
- Restore: completed into the isolated exact-case database `"AxisHCS"` on `hcs-community-postgres-1` using the PostgreSQL 17 client in the container.
- Existing target databases and `seafood_crm` were not overwritten.
- Core source volumes: `AbpUsers` 951, `AppDocuments` 99, `AppDocumentFiles` 497, `AppProjects` 11, `AppWorkflows` 30, `ChatConversations` 8, `AbpAuditLogs` 74,594.

## Target boundary

The destination is `services/HCS_web_free_license`, whose data ownership is split across:

```text
Keycloak / HCS Identity
          |
          +--> hcs_organization   organization catalogs and user relations
          +--> hcs_document       documents, workflows, files, signing metadata
          +--> hcs_work           projects, tasks, calendar, surveys, reports
          +--> hcs_collaboration  chat, notifications, social activity
```

`services/HCS_bus_management` is a separate bounded context. The dump has no Bus Management tables, so it is not a target for this migration.

## Classification rules

1. **Transform-required:** business tables whose source and target names or columns differ.
2. **Low-transform:** tables that are structurally compatible after identity remapping and safe metadata normalization.
3. **Excluded:** SaaS/tenant administration, OpenIddict, GDPR, forms, text templates, File Management, credentials, tokens, and commercial-only configuration.
4. **Manual/blob:** rows that reference object storage or signing material whose binary content is not inside the PostgreSQL dump.

## Completed steps

- Inspected workspace architecture and service boundaries.
- Restored the complete dump to an isolated source database.
- Compared source tables to the existing five target schemas.
- Confirmed that direct database restore and generic source-column upsert are unsafe because most mapped tables have incompatible columns.

## Remaining TODO

- Export a verified Keycloak user list and measure mapping coverage.
- Inventory the source object-storage endpoint, buckets, and referenced paths.
- Confirm whether current non-empty target databases are rehearsal data or must be preserved.
- Freeze the target contract/version before writing table mappers.

## Success criteria

- Source remains queryable and read-only.
- No source or unrelated database is modified.
- Every source table has an explicit migration disposition before implementation begins.

## Risks and controls

- **Risk:** using the legacy monolith schema as the target schema. **Control:** target migrations and service contracts are authoritative.
- **Risk:** ambiguous users after identity migration. **Control:** stop unresolved references in dry-run; never guess a user ID.
- **Risk:** existing target data is overwritten. **Control:** disposable target clone, backup, primary-key collision report, and explicit confirmation before execution.

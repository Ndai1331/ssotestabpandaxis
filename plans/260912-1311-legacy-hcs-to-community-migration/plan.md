---
title: Legacy HCS snapshot restore and Community migration
created: 2026-09-12
status: complete
---

# Legacy HCS snapshot restore and Community migration

## Overview

Use `AxisHCS_20260912_125331.dump` as a read-only legacy source and move its business data into `services/HCS_web_free_license`, preserving the HCS License feature set while adapting the data to the five microservice-owned databases. The licensed HCS application is not a runtime dependency for this plan and will not be started again.

The dump was restored successfully as an isolated PostgreSQL database named `"AxisHCS"` inside `hcs-community-postgres-1`. The HCS Free target schemas were preserved, backed up, and then populated by a table-specific importer. All source rows are represented either in active microservice tables or in the controlled `legacy_migration.source_rows` archive.

The destination is `services/HCS_web_free_license`; `services/HCS_bus_management` is a separate bounded context and is outside this migration because the dump contains no Bus Management tables.

## Phases

- [x] Phase 1 — inspect workspace, dump metadata, runtime topology
- [x] Phase 2 — restore full dump into isolated PostgreSQL database
- [x] Phase 3 — compare source schema, target schemas, and feature boundaries
- [x] Phase 4 — freeze target contracts and implement table-specific transforms
- [x] Phase 5 — establish the available identity policy and record the blob boundary
- [x] Phase 6 — run dry-run reconciliation against the target schemas
- [x] Phase 7 — execute staged import with backups, checkpoints, and archive handling
- [ ] Phase 8 — validate feature parity, cut over, and retain rollback evidence

## Dependencies

- PostgreSQL 17 client/server compatibility for dump format 1.16; the source dump was produced by PostgreSQL 18.3.
- Existing HCS Community migrations and contracts in `services/HCS_web_free_license`.
- No Keycloak export was supplied for this run; legacy user GUIDs were preserved and imported as external users with passwords removed. A Keycloak mapping is still required before login cutover.
- A source object-storage inventory is required for document, signature, and survey files; PostgreSQL rows do not contain the binary objects.
- Target databases remain separate: `hcs_identity`, `hcs_organization`, `hcs_document`, `hcs_work`, `hcs_collaboration`.

## Safety constraints

- Never modify `services/HCS_web_with_license` as migration input.
- Do not modify `seafood_crm`. Existing HCS target business rows may be replaced only after an explicit backup and confirmation; the pre-import backups are in `backups-20260912/`.
- Do not start the licensed HCS runtime; the isolated dump database is the only source of truth for this migration assessment.
- Do not directly restore the monolith database into a microservice database.
- Do not import commercial/SaaS/GDPR/OpenIddict/File Management data into Community.
- Do not migrate passwords, signing private keys, tokens, or client secrets.
- Target writes were executed only after the dry-run completed and database backups were created. The explicit confirmation gate remains in the importer.
- Preserve existing dirty worktree changes; commit only after the user explicitly requests it.

## Success criteria

- Full dump is available in isolated `"AxisHCS"` source database with verified table counts.
- Every source table is classified as automatic, transform-required, excluded, or manual/blob migration.
- Each target write has an explicit table mapper, dependency order, checkpoint, and idempotency rule.
- Dry-run produces counts, checksums, user/relationship/blob issues, and a rollback preview without changing target data.
- The final execute report reconciles 61 mapped tables and 194,560 source rows with zero user, relationship, or reported blob-reference issues.
- Every source row from mapped tables is active or archived; 1,780 unmapped legacy rows and 257 target-conflict/orphan rows are retained in `legacy_migration.source_rows`.
- Feature-parity QA, object-storage transfer, Keycloak reconciliation, application startup, and cutover remain follow-up work.

## Current evidence

- Source database: 951 users, 99 documents, 497 document files, 11 projects, 30 workflow instances, 8 conversations, and 74,594 audit logs.
- `HCS.MigrationImporter` now provides read-only source snapshots, target-shaped mappers, dependency ordering, checkpoints, checksum/idempotency handling, secret stripping, conflict archiving, and reconciliation reports.
- Final report: `runtime-execute-final3/reconciliation.json` (61 tables, 194,560 source rows, 194,560 processed rows, zero user/relationship/blob issues).
- Target database backups: `backups-20260912/`.
- Active target rows that cannot be represented because of target uniqueness/orphan constraints are preserved as raw source rows in `legacy_migration.source_rows`; binary object-store content was not copied.
- The HCS Community runtime is up from the local compose stack; `db-migrator` completed successfully, `https://hcs.localhost/` returned HTTP 200, and the OIDC discovery endpoint returned HTTP 200.

## Deliverables

- `research/migration-assessment.md` — source inventory, schema mismatch evidence, feature parity matrix, risks, and decisions.
- `phases/phase-01-source-and-target-inventory.md` — completed evidence and classification method.
- `phases/phase-02-schema-transform-and-importer.md` — required mapper and importer work.
- `phases/phase-03-identity-and-blob-migration.md` — Keycloak and object-storage migration plan.
- `phases/phase-04-feature-parity-and-cutover.md` — test gates, rollout, and rollback.
- `runtime-execute-final3/reconciliation.json` — final execution/reconciliation result.
- `runtime-execute-final3/reconciliation.csv` — per-table reconciliation result.
- `runtime-execute-final3/rollback-preview.txt` — rollback preview.
- `backups-20260912/` — pre-import dumps of the five HCS Free target databases.

---
title: Feature parity and cutover
phase: 4
status: planned
updated: 2026-09-12
---

# Phase 4 — Feature parity and cutover

## Context links

- [Migration plan](../plan.md)
- [Migration assessment](../research/migration-assessment.md)
- [Community service](../../../services/HCS_web_free_license/README.md)

## Parity objective

The goal is functional parity with the legacy HCS License screens and workflows, not schema parity. Each feature is validated through the new service API and, where applicable, the Blazor UI.

| Legacy capability | Community microservice | Acceptance focus |
|---|---|---|
| Departments, units, positions, master data, user assignments | Organization | catalog reads/writes, hierarchy, user visibility |
| Documents, files, history, assignments | Document | metadata, file links, permissions, audit trail |
| Workflow definitions/templates/steps/instances | Document | submit, assign, approve/reject, overdue/finish state |
| Signature settings and signature history | Document | metadata restored, secrets re-provisioned, signing smoke test |
| Projects, members, tasks, task documents | Work | project graph, assignment, document linkage, status |
| Calendar and participants | Work | owner/participants, time zones, notifications |
| Surveys and reports | Work | session/catalog/results and read-model correctness |
| Chat, inbox, notifications, push devices | Collaboration | conversation graph, unread state, attachments, notification delivery |
| Users, roles, permissions | Keycloak + Platform | login, role mapping, authorization matrix |

## Validation gates

1. **Schema gate:** all target migrations are applied and the importer targets the intended service DB.
2. **Data gate:** source/transformed/target counts and checksums reconcile; all blocking relationship and identity errors are resolved or explicitly accepted.
3. **Security gate:** login through Keycloak works; authorization tests cover administrator, manager, staff, and read-only cases; secrets are absent from imported data.
4. **API gate:** CRUD/read flows and negative authorization tests pass per service.
5. **UI gate:** representative legacy journeys work in the Community Blazor client: organization lookup, document submission, workflow approval, project task, survey, chat, and notification.
6. **Blob gate:** files download/render and hashes match the approved transfer manifest.
7. **Operational gate:** backups, checkpoint resume, logs, health checks, and rollback procedure are rehearsed.

## Proposed rollout

```text
freeze source snapshot
  -> backup target databases
  -> import organization + identity references
  -> import document/workflow graph
  -> import work graph
  -> import collaboration graph
  -> transfer/verify blobs
  -> refresh derived data
  -> run parity checklist
  -> enable users and monitor
```

Use a rehearsal first. For the final cutover, stop legacy writes if the legacy system is still producing changes; otherwise record the dump timestamp as the source cutoff and communicate the data boundary.

## Rollback

- Stop the importer and application writes.
- Restore the pre-cutover backup of each affected target database.
- Revert object-store changes using the transfer manifest or an isolated target bucket.
- Keep the source `"AxisHCS"` database untouched for forensic comparison.
- Record the failed gate and rerun only after the mapper/report is corrected.

## Exit criteria

- All agreed legacy functions have a passing Community acceptance test or a signed gap decision.
- No unresolved identity, relationship, or blob blocker remains.
- The user has reviewed reconciliation, backup, and rollback evidence before production-like target writes.

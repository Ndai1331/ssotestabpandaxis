---
title: "Community quality gate"
date: 2026-08-04
scope: "HCS_web_free_license"
---

# Test Report — 2026-08-04 — Community quality gate

## Test Results Overview

- Total: 154
- Passed: 154 | Failed: 0 | Skipped: 0
- Suites: AuthServer 11, WebGateway 33, Document 24, Organization 18, Work Management 23, Collaboration 19, Platform EF 7, Domain 5, Application 3, Migration Importer 11.
- `HCS.TestBase` contains no discoverable tests; it did not fail the run.

## Build and dependency status

- `dotnet build HCS.slnx --no-restore --disable-build-servers -v minimal`: PASS, 0 warnings / 0 errors.
- `dotnet test HCS.slnx --no-restore --no-build --disable-build-servers -v minimal`: PASS.
- Platform `dotnet ef migrations has-pending-model-changes`: PASS. Migration `20260804095716_AddPlatformAuditProjectionAndEvents` includes audit projection and ABP inbox/outbox persistence.
- `./scripts/audit-license-clean.sh`: PASS.
- NuGet vulnerability query completed with no vulnerable package reported.
- Coverage metrics: not collected; no project coverage threshold is configured.

## Verified hardening

- BFF refresh-token rotation uses distributed Redis coordination, distinguishes terminal from transient refresh errors, caps replayable request bodies, validates production HTTPS origins, and uses the BFF/XSRF handler for the SignalR client.
- Inbox claims are transactional and only suppress the exact unique-marker duplicate; durable outbox leases protect terminal rows.
- Collaboration keeps deletion tombstones and prevents duplicate direct conversations; survey management has its own authorization policy.
- Audit exception values are sanitized. Mutating HTTP responses are deferred until the database mutation and audit-outbox commit succeeds; GET downloads retain streaming.

## Remaining release work

- Stage MinIO uploads before opening the database audit transaction; otherwise slow 50 MiB uploads can pin DB connections.
- Perform live Keycloak, PostgreSQL, Redis, RabbitMQ and MinIO E2E; no runtime infrastructure or source migration has been executed in this pass.
- Perform importer dry run against the licensed source database and validate reconciliation/blob reports. Do not modify source data.
- Replace generic Blazor route panels with full feature-parity screens and validate menu/permission flows in a browser.
- Resolve the documented Blazorise organization-license decision and Bnn signing redistribution/real signing-adapter release gate.

## Unresolved questions

- Which runtime credential bundle and Keycloak realm export should be used for the first isolated local E2E run?
- Is the organization licensed for Blazorise, or should the client switch to an approved OSS component stack?

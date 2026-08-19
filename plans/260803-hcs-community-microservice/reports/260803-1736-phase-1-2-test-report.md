---
title: Phase 1-2 QA Report
date: 2026-08-03 17:36 +07:00
scope: services/HCS_web_free_license
status: conditional-pass
---

# Phase 1-2 Test Report

## Summary

- Community/license audit: PASS.
- Full solution build: PASS, 19 projects, 0 warnings, 0 errors.
- Full test suite: PASS, 16/16 distinct tests.
- Targeted stability rerun: PASS, 14/14 executions.
- Phase 1 baseline verified. Phase 2 not acceptance-complete due missing end-to-end security/integration coverage and incomplete required flows noted below.

## Scope Selection

Diff-aware mode auto-escalated to full suite. Git sees `services/HCS_web_free_license/` as one untracked solution tree (257 non-generated files, 113 C# files), so file-to-test mapping is not reliable. Solution/config/host additions also require full-suite validation.

## Test Results Overview

| Run | Passed | Failed | Skipped | Duration |
|---|---:|---:|---:|---:|
| `dotnet test HCS.slnx --no-restore --disable-build-servers` | 16 | 0 | 0 | 21.23 s wall |
| AuthServer targeted rerun | 6 | 0 | 0 | 2.52 s wall; 64 ms tests |
| Gateway targeted rerun | 2 | 0 | 0 | 2.61 s wall; 190 ms tests |
| Platform EF targeted rerun | 6 | 0 | 0 | 10.87 s wall; 8 s tests |
| Total executions | 30 | 0 | 0 | 37.23 s wall |

Distinct full-suite breakdown:

- `HCS.AuthServer.Tests`: 6 passed.
- `HCS.WebGateway.Tests`: 2 passed.
- `HCS.Domain.Tests`: 2 passed.
- `HCS.EntityFrameworkCore.Tests`: 6 passed.
- `HCS.Application.Tests`: 0 discovered; tests are abstract base scenarios realized through EF tests.
- `HCS.TestBase`: 0 discovered; support assembly, not an executable suite.

No flaky failure observed across one full run plus one targeted rerun. Repetition depth insufficient to prove absence of flakiness.

## Coverage Metrics

| Metric | Value | Threshold | Status |
|---|---:|---:|---|
| Lines | unavailable | 80% | NOT MEASURED |
| Branches | unavailable | 70% | NOT MEASURED |
| Functions | unavailable | 80% | NOT MEASURED |

`Microsoft.CodeCoverage` is restored transitively, but solution has no checked-in coverage collection/report/threshold command. Current tests cannot demonstrate the requested 80% quality gate.

## Build and Dependency Status

- `./scripts/audit-license-clean.sh`: PASS before and after build/test.
- `dotnet build HCS.slnx --no-restore --disable-build-servers -v minimal`: PASS in 19.61 s.
- Build warnings: 0. Build errors: 0.
- All projects resolved from existing restore; this run intentionally used `--no-restore`.

## Critical Issues

1. **Phase 2 blocker — Keycloak provisioning/role persistence not verified and no dedicated implementation found.** Tests cover group gate and in-memory claim mapping only. No test proves idempotent first-login user link/provision by verified email/username or removal/addition of persisted ABP roles on each login.
2. **Phase 2 blocker — centralized audit projection absent/unverified.** Current viewer reads local `IAuditLogRepository`; no audit integration event, outbox consumer, cross-service projection, retry, or idempotency test found.
3. **Acceptance gap — no live Keycloak/OpenIddict E2E.** Missing authorization-code redirect/callback, `prompt=login`, denied group, client/scope seed, certificate, token issuance, and service-to-service client-credentials tests.
4. **Acceptance gap — gateway tests are config-only.** No running-host test for auth enforcement, bearer/token forwarding, WebSocket `/hubs/chat`, CORS, health, upstream failure, or route contract behavior.
5. **Platform coverage gap.** Language tests cover one-default behavior and text update/cache read. Missing duplicate culture/text, unknown culture, delete/update default rejection, permission enforcement, paging/sorting/filtering, distributed cache invalidation across instances, controllers/routes, and concurrency.
6. **Audit viewer coverage gap.** Only combined list filter tested. Missing detail lookup, not-found, independent filter branches, action/entity/error payload mapping, paging/sorting, permission enforcement, and sensitive-data handling.

## Performance

- Slowest observed suite: Platform EF, about 8 s test duration.
- Slowest observed test in full run: initial admin seed check, about 4 s.
- No benchmark/load/memory-leak suite exists. Current timings acceptable for local integration tests; no performance requirement validated.

## Recommendations

1. P0: implement and integration-test Keycloak first-login provisioning/linking plus authoritative role reconciliation.
2. P0: implement audit event/outbox/inbox projection and retry/idempotency tests.
3. P0: add local-stack E2E for Keycloak, OpenIddict, Gateway, PostgreSQL, Redis, RabbitMQ, and MinIO before marking Phase 2 complete.
4. P1: add `dotnet test` coverage collection, Cobertura report, and enforced line/branch thresholds.
5. P1: add WebApplicationFactory gateway tests with fake upstream and WebSocket/token-forwarding assertions.
6. P1: extend language/audit tests for errors, authorization, concurrency, and distributed invalidation.

## Unresolved Questions

- Should Phase 2 use ABP external-login auto-provisioning as-is, or require a custom Keycloak provisioning service for deterministic linking and persisted role reconciliation?
- What audit retention/detail-redaction rules must the centralized projection enforce?

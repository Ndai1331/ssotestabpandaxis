---
title: Red team — HCS to microservice plan
date: 2026-07-24
verdict: pass-with-fixes
---

# Red team review

Hostile findings → mitigations applied in plan.

| # | Attack | Severity | Response in plan |
|---|--------|----------|------------------|
| 1 | `workflow-service` = Elsa vs HCS Workflow naming collision | High | Locked: DocumentService mới; Elsa orthogonal; ADR Phase 1 |
| 2 | Phase 3 = 40% plan — “one phase” cook sẽ fail | High | Slices 3a–3h; cook per slice |
| 3 | User shared-DB vs HCS Saas DB-per-tenant capability | Med | Target = shared DB explicit; don't copy tenant connection-string complexity |
| 4 | Dual codebase drift | Med | Feature freeze HCS |
| 5 | Reporting cross-DB joins | Med | Phase 7 ETL/events only |
| 6 | OpenIddict seeder edit races (Elsa + Org + Doc) | Med | Serialize seeder; note in Phase 2/5 |
| 7 | Mobile parity “ngay” vs Org-first roadmap | Med | Accepted: Org 1–2w then Doc; not day-0 |
| 8 | Underestimated Chat/SignalR+YARP | Low | Phase 6 risk + early WS test |
| 9 | plans/plan.md Task9 Ahrefs noise | Low | Ignore; unrelated pending junk |

**Verdict:** Plan viable. Largest residual risk = Phase 3 delivery discipline.

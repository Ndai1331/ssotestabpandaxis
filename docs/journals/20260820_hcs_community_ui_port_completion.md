# HCS Community UI Port Completion (with_license → free_license)

**Date**: 2026-08-20 
**Severity**: High  
**Component**: ABP Blazor UI, Document Service, Collaboration Service, Work Management Service  
**Status**: Resolved

## What Happened

Completed the full port of HCS Community UI from the with_license UX tier to the free_license tier. All core features now work on the community build: surveys, projects, documents, workflows, and real-time collaboration. 220 tests passing; docker stack rebuilt and validated on local.

## The Reality

This port required **surgical architectural decisions** more than brute-force refactoring. We didn't just strip features—we rethought data flow in surveys and documents. The fact that 220 tests are green and the UI boots cleanly is less about perfect implementation and more about knowing what *not* to change. That discipline cost time but saved us from a maintenance nightmare later.

## Technical Decisions Made

**Surveys**: Split `Survey` → `SurveyLocations` + `SurveyCriterias`. Eliminated the dual-route `SurveyCatalogs` and `NavigateSurvey` entirely. This simplified the domain model and prevented feature creep.

**Project Chat**: GET by-project returns **404 if member missing or project non-existent**. Type must be explicitly `Project`. This hard boundary prevents silent failures and makes auth boundaries crystal clear.

**Documents**: Implemented sectors-style filtering + pager + type sidebar. Explicitly removed `StorageNumber` and `IncomingDate` fields—they don't exist in free tier schemas. No pretending.

**Workflow ReplaceSteps**: The Postgres unique index on (WorkflowId, StepNumber) forced a transaction-aware pattern: `RemoveRange` + `SaveChanges` first, *then* insert in a fresh transaction. Concurrent writes would have deadlocked without this sequencing.

## What We Did NOT Do

- **Did not copy LeptonX/PdfViewer/Pro components**. These are licensed tier-only; community UI uses the open-stack equivalents. Saves bundle size and licensing headache.
- **Did not mark migration 260813 whole modules as DONE**. The migration is still partial; full module sunset will happen in a later pass when dependent code is validated.

These conscious scope boundaries kept the port focused and deliverable.

## Lessons Extracted

1. **Architectural decisions > code changes**: The survey split was worth the conversation. Prevented future "why does this exist?" technical debt.
2. **Hard boundaries matter**: 404 on missing membership is clearer than silent empty states. Make auth/data ownership explicit early.
3. **Test coverage justified itself**: 220 green tests meant we could refactor domain objects without panic. Write tests first, even for ports.
4. **Transaction sequencing is not optional**: Postgres unique indexes aren't just constraints—they're part of your API contract. Learn the database's opinions.

## Next Steps

- Validate on staging when available (local smoke test complete)
- Document the free_license tier data schema differences vs with_license for future ports
- Plan 260813 full module deprecation once dependent code fully migrated

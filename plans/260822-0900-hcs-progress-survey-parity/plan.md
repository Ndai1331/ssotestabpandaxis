---
status: completed
---

# Progress/status sync and Survey parity

## Goal

Port the paid Survey/SurveyResult behavior to the free service while preserving the
free service boundaries, and enforce bidirectional task progress/status synchronization.

## Phases

1. [phase-01-progress-status.md](phase-01-progress-status.md) — domain invariant,
   API/UI synchronization and regression tests.
2. [phase-02-survey-public-api.md](phase-02-survey-public-api.md) — public survey
   contracts, persistence, Work API and anonymous BFF routing.
3. [phase-03-survey-ui-results.md](phase-03-survey-ui-results.md) — public
   collection UX and SurveyResult statistics/detail parity.
4. [phase-04-verification.md](phase-04-verification.md) — compile, unit/security
   tests and final diff review.

## Constraints

- Work only in services/HCS_web_free_license plus this plan.
- Preserve unrelated dirty changes in the workspace.
- Keep public endpoints limited to the survey collection flow; management and
  result data remain protected.
- Do not copy commercial ABP/LeptonX dependencies from the paid source.

## Definition of done

- Task progress 100 and status Completed always converge in domain/API/UI.
- Anonymous users can open /survey-collections/{locationId} and submit the
  paid-source-equivalent survey flow through the BFF.
- Protected SurveyResult page exposes location-filtered statistics and detail data.
- Relevant tests and builds pass, with failures investigated rather than ignored.

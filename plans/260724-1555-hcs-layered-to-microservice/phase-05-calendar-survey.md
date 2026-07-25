---
phase: 5
title: Calendar and Survey
status: pending
effort: 2-3w
dependsOn: [2]
---

# Phase 05 — Calendar + Survey

## Goal

Hai service độc lập (parallelizable sau Phase 2):

| Service | Port | DB |
|---------|------|-----|
| CalendarService | 44382 | `hanhchinhso_Calendar` |
| SurveyService | 44383 | `hanhchinhso_Survey` |

Soft depends Phase 3 only nếu calendar event gắn document — mặc định **dependsOn Phase 2** (org users).

## Source (HCS)

- Calendar: `CalendarEvents`, `CalendarEventParticipants`
- Survey: `SurveyLocations`, `SurveyCriterias`, `SurveySessions`, `SurveyFiles`, `SurveyResults`

## Steps

1. Scaffold cả 2 services + wire
2. Port domain + Mud UI
3. Survey files → MinIO (reuse blob pattern Document/Admin)
4. Parity checklist

## Success criteria

- [ ] Calendar CRUD + participants
- [ ] Survey session → submit result E2E
- [ ] Both qua Gateway

## Parallel

Calendar ∥ Survey OK nếu 2 cook agents không sửa cùng OpenIddict seeder — batch scope adds một PR/session.

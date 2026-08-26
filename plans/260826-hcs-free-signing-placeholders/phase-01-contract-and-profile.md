# Phase 01 — contract and profile lookup

## Goal

Make current-user display name, department name and position name available to the free DocumentService through authorized service APIs, without a database reference between services.

## Tasks

- Extend the existing organization lookup response additively with `PositionName` (and any minimal position identifier needed for diagnostics).
- Resolve the current user mapping with primary mapping precedence and join department/position names in OrganizationService.
- Ensure the lookup is allowed for the existing signing/workflow permission boundary, while administrative mapping CRUD permissions remain unchanged.
- Add a small HTTP resolver in DocumentService that forwards the incoming bearer token and fails closed/with an explicit empty metadata result when lookup is unavailable.

## Gate

Organization and DocumentService compile; existing organization API tests remain green; no direct project/database reference is introduced.


# Phase 2 — Blazorise CRUD vertical slices

## Order

1. Organization/document catalogs: replace bespoke card/table/form markup with `DataGrid`, `Modal`, `Button`, `Alert`, validation and confirmation components.
2. Documents/signing/workflow: add localized file/assignment/audit workflows using components already compatible with BFF multipart requests.
3. Projects/tasks/calendar/surveys/reports: use responsive grid/filter/pager patterns and localized empty/error states.

## Rules

- Migrate one domain slice at a time with browser CRUD evidence before the next slice.
- Retain existing API contracts and authorization checks; UI change must not weaken a server policy.
- Use paid source only to reproduce behavior (wizard tabs, confirmation, fields, role-aware actions), translating it to free-service contracts.

## Acceptance

- Every migrated slice has no duplicate hard-coded action/status text in vi/en.
- Destructive actions require an accessible confirmation and prevent duplicate requests.

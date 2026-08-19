# Phase 3 — Shell, accessibility and verification

## Work

- Localize top navigation, dropdown labels, user menu, admin screen and AuthServer login theme.
- Validate desktop/tablet/mobile widths, focus order, contrast, empty/loading/error states and keyboard dropdown/modal behavior.
- Remove replaced page-local CSS and ensure Blazorise Bootstrap styles do not conflict with `HCSMainLayout`.
- Run unit/resource tests, authenticated browser smoke tests for every top-menu route, and visual snapshots in vi/en.

## Completion evidence

- Browser proves culture switch + fresh login + admin permissions + CRUD route availability in both languages.
- Build/test output is clean; no paid package, license key or secret is introduced.

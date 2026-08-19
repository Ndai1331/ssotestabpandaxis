# Phase 1 — Localization and UI foundation

## Scope

- Inventory literal user-facing text in `HCS.Blazor.Client/Pages`, `Layouts`, `Components` and the AuthServer login theme.
- Add grouped `HCSResource` keys to `vi.json` and `en.json`; inject `IStringLocalizer<HCSResource>` through `HCSComponentBase` or page base classes.
- Create shared Blazorise wrappers: page header/action bar, feedback alerts/toasts, empty state, destructive-action confirmation and table shell.
- Wire a culture selector to ABP localization/current-culture behavior; preserve `vi` default and persist the chosen culture through normal ABP conventions.

## Guardrails

- Keep raw backend error messages out of the UI.
- Localize labels, validation and status text; never localize API routes, permission names, enum storage values or date serialization.
- Add tests for resource-key existence and culture fallback.

## Acceptance

- Switching vi/en changes shared shell and one representative CRUD page without a reload loop or auth loss.
- Shared components render keyboard-accessible actions and semantic feedback.

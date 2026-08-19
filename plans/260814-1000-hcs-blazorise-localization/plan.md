---
status: planned
blockedBy: [260813-1200-hcs-free-feature-parity]
---

# HCS Blazorise UI and localization modernization

## Overview

Modernize the free HCS client after feature parity is verified. Use the existing Blazorise Bootstrap 5 + FontAwesome stack; do not add MudBlazor or ABP Pro UI packages. The paid source is a behavior/layout reference only: its commercial DTOs, `HC.HttpApi`, and Pro widgets are not portable.

## Evidence

- Free client already references Blazorise 2.2.1, Bootstrap5 and FontAwesome in `HCS.Blazor.Client.csproj` and configures them in `HCSBlazorClientModule`.
- `HCSResource` already has Vietnamese/English JSON resources, but free custom pages mostly hard-code Vietnamese strings.
- Paid source consistently uses `IStringLocalizer<HCResource>`, Blazorise `Modal`, `DataGrid`, `Alert`, `Button`, and localized confirmation/validation messages.

## Decision

Adopt Blazorise, not MudBlazor. A second component/theme system would increase bundle size, duplicate Bootstrap behavior, and make the current Lepton/ABP integration less coherent.

## Phases

1. Foundation — localization inventory, stable UI primitives, culture selector and message/confirmation abstraction.
2. CRUD modernization — migrate Organization, Documents, Workflow, Work, Calendar, Surveys and Signing in vertical slices to localized Blazorise components.
3. Navigation/admin/auth polish — top-menu language switching, responsive/accessibility validation and admin/profile consistency.
4. Regression, browser matrix and removal of superseded CSS/markup.

## Success criteria

- Vietnamese and English cover every string introduced by custom free pages; no language-dependent permission/API identifiers.
- Each migrated CRUD page uses the shared Blazorise primitives for loading/empty/error/success, confirmation and responsive table/form behavior.
- No MudBlazor/ABP Pro dependency or paid source code is copied.
- Desktop and mobile authenticated paths preserve BFF/permission behavior.

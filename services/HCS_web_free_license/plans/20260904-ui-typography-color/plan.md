---
title: "HCS shared title typography and primary link color"
status: completed
progress: 100%
created: 2026-09-04
tags: [frontend, ui, css, accessibility]
---

# HCS shared title typography and primary link color

## Objective

Reduce oversized titles across the shared HCS shell and make ordinary links and title treatments consume the existing HCS primary color tokens instead of Bootstrap's default blue or ad-hoc navy values.

## Scope

Modify only:

- `src/HCS.Blazor.Client/wwwroot/hcs-tokens.css`
- `src/HCS.Blazor.Client/wwwroot/hcs-components.css`
- `src/HCS.Blazor.Client/wwwroot/main.css`
- `src/HCS.Blazor.Client/Components/GatewayDataPanel.razor.css`
- `src/HCS.Blazor.Client/Components/NotificationToast.razor.css`
- `src/HCS.Blazor.Client/Components/UserSignaturesPanel.razor.css`
- `src/HCS.Blazor.Client/Pages/Index.razor.css`
- `src/HCS.Blazor.Client/Pages/AccountManagement.razor.css`
- `src/HCS.Blazor.Client/Pages/Administration.razor.css`
- `src/HCS.Blazor.Client/Pages/AdministrationRoles.razor.css`
- `src/HCS.Blazor.Client/Pages/AuditLogs.razor.css`
- `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor.css`
- `src/HCS.Blazor.Client/Pages/SurveyCollections.razor.css`
- `src/HCS.Blazor.Client/Pages/SurveyResults.razor.css`

Do not modify page markup, page-scoped CSS, routes, auth, APIs, backend services, packages, or JavaScript. Preserve button, navigation-on-teal, status, error, and disabled-state color semantics.

## Implementation

1. Add shared semantic heading scale and link RGB tokens to the existing HCS token layer.
2. Complete the scoped Bootstrap bridge with `--bs-link-color-rgb`, hover RGB, heading color, and body RGB values so Bootstrap anchors resolve to HCS teal.
3. Add a final shared HCS heading/link rule for common content titles and ordinary anchors, excluding branded/icon/menu/button/chat-preview controls where their existing interaction styling is intentional.
4. Reduce legacy catalog/modal heading sizes and replace remaining title/link default-blue literals in `main.css` with semantic tokens.
5. Update page/component-scoped title selectors that otherwise override the shared scale, while retaining semantic status/error colors and chat/menu surface contrast.

## Acceptance checks

- Main page titles are `clamp(1.25rem, 1.6vw, 1.6rem)` and modal/section titles stay compact.
- Ordinary HCS links use `var(--hcs-color-primary)` and hover/focus use `var(--hcs-color-primary-strong)`.
- No HCS application stylesheet retains `#0d6efd` or hard-coded title navy values for shared title/link rules.
- Buttons, teal navigation, danger/error, warning, success, and info status treatments remain unchanged.
- `git diff --check`, static CSS assertions, build, and relevant tests pass.

## Plan review verdict

Approved for implementation as a bounded presentation-layer follow-up. The existing HCS teal token system and Be Vietnam Pro typography remain the source of truth; no new palette or component framework is introduced.

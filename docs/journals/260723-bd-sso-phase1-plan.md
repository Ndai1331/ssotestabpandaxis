# Journal — 2026-07-23

## BD SSO Phase 1 plan created

- Source: brainstorm `plans/reports/brainstorm-260723-1415-bd-sso-login-flow.md` (approved Approach A).
- Plan dir: `plans/260723-1419-bd-sso-phase1/`
- Phases: Keycloak → Directus OpenID → ABP AuthServer external OIDC → E2E/runbook.
- Ports locked: KC 5110, Directus 8055/8080, AuthServer 44372, Blazor 44306.
- Next: `/cook .../plan.md --auto` when user requests implement.

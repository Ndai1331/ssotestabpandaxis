# Journal — 2026-07-23 cook SSO Phase 1

## Done
- Keycloak `:5110` up; realm `bd` bootstrapped (script idempotent).
- ABP AuthServer: OpenIdConnect Keycloak + group→role claims; Identity seed bacsi/lanhdao/nhanvien.
- Docs: runbook + Directus `.env.sso.example`.

## Blocked on user runtime
- Directus not fully started in this session — ROLE_MAPPING UUIDs pending Studio roles.
- Full E2E SSO needs Directus + ABP infra/apps running together.

## Next for user
1. Follow `docs/runbooks/local-sso-lab.md` §2–4.
2. Confirm SSO Directus→ABP without re-password.

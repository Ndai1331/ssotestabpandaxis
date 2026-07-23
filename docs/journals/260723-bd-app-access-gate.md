# Journal — 2026-07-23 App access gate

Implemented Keycloak app entitlement for BD lab:

- Groups `bd-app-directus` / `bd-app-abp` in bootstrap; lab users get both + role group
- ABP AuthServer fails OIDC if missing `bd-app-abp`; default `nhanvien` only after gate
- Directus hook extension rejects Keycloak login without `bd-app-directus`
- Docs: runbook, handoff, wiki hot/log

Reload: re-bootstrap KC (done), recreate Directus (done), restart AuthServer when testing ABP.

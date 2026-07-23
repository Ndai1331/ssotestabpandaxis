# Journal — 2026-07-23 Axis rebrand

White-label source in `services/directus-main`:

- Table/collection prefix `directus_*` → `axis_*` (system-data, seeds, migrations, api/app/sdk/tests)
- Display `Directus` → `Axis` (en-US i18n + UI/API fallbacks; vi-VN had no Directus strings)
- Kept: `@directus/*`, env `DIRECTUS_*`, docker paths, SDK type names, directus.com URLs
- Lab: wiped PG volumes (`bd_axis_*`), restarted compose, re-bootstrapped Keycloak realm `bd`

**Gap:** compose still runs upstream image `directus/directus:11.9.2`, so live DB tables remain `directus_*` until a local image build from this fork.

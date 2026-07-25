# ARCHIVE — Directus v12 (not lab SoT)

> **Do not use this tree for BD SSO lab.**

Lab Directus SoT is **`../directus-main-v11`** (11.13.4):

```bash
cd ../directus-main-v11
docker compose -f docker-compose.bd-lab.yml up -d
```

## Why archived

- This tree is Directus **12.x** (MSCL) with **runtime license SSO gate**.
- Lab previously needed `BD_LAB_ALLOW_SSO=true` bypass.
- v11 has OpenID SSO without that gate; compose + `bd-app-axis` gate live under `directus-main-v11`.

## Keep for

- Comparing MSCL / license entitlement behavior
- Historical Axis rebrand / plan references

## Do not

- Run `docker-compose.bd-lab.yml` here while v11 lab is up (ports `:5110` / `:8055` / `:5120` / `:5121` conflict)
- Point runbooks or new agent work at this path for SSO

See: `docs/runbooks/local-sso-lab.md`, plan `plans/260725-1726-directus-v11-sso-lab/`.

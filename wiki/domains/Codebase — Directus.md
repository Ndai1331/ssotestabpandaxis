---
type: domain
title: "Codebase — Directus"
updated: 2026-07-23
---

# Codebase — Directus

Path: `services/directus-main/`

## Role in BD
OIDC client của Keycloak — Clinical Data Management.

## Local infra
`docker-compose.yml` (debug):
- Keycloak **:5110** (admin/secret)
- Postgres :5100, Redis :5105, …

## Next for SSO
Cấu hình Directus Auth OpenID → issuer Keycloak realm; map roles → policies.

Upstream readme: `services/directus-main/readme.md`

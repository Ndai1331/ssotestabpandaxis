# Docker Compose production — hanhchinhso

Templates for Ubuntu 24.04+ single-VM (or staging) deploy with **host Nginx** TLS.

| File | Role |
|------|------|
| `docker-compose.prod.yml` | Infra + core apps (+ `--profile full`) |
| `.env.example` | Copy → `.env` (secrets; gitignored) |
| `postgres/init-databases.sql` | Create ABP DBs on first postgres volume |
| `nginx/hcs.conf.example` | Reverse proxy → `127.0.0.1:8080/8081/8082` |
| `certs/` | Place `openiddict.pfx` here (not committed) |

Full runbook: [`docs/runbooks/deploy-abp-production.md`](../../../../docs/runbooks/deploy-abp-production.md)

## Prerequisites

- Docker Engine + Compose plugin on Ubuntu 24.04+
- Images built/pushed to `${IMAGE_REGISTRY}` (see `etc/helm/build-image.ps1` or `dotnet publish` + `docker build`)
- DNS for `app` / `auth` / `gateway`
- Nginx + Certbot on host

### Image name mapping

| Compose service | Expected image | Dockerfile path |
|-----------------|----------------|-----------------|
| authserver | `{registry}/authserver:{tag}` | `apps/auth-server/hanhchinhso.AuthServer` |
| blazor | `{registry}/blazorwebapp:{tag}` | `apps/blazor/hanhchinhso.Blazor` |
| webgateway | `{registry}/webgateway:{tag}` | `gateways/web/hanhchinhso.WebGateway` |
| identity | `{registry}/identity:{tag}` | `services/identity/...` |
| administration | `{registry}/administration:{tag}` | `services/administration/...` |
| language | `{registry}/language:{tag}` | `services/language/...` |
| organization | `{registry}/organization:{tag}` | `services/organization/...` (profile `full`) |
| workflow | `{registry}/workflow:{tag}` | `services/workflow-service/...` (profile `full`) |
| auditlogging | `{registry}/auditlogging:{tag}` | `services/audit-logging/...` |
| gdpr | `{registry}/gdpr:{tag}` | `services/gdpr/...` |
| aimanagement | `{registry}/aimanagement:{tag}` | `services/ai-management/...` |

Align tags with whatever you push from CI / `build-all-images`.

## Quick start

```bash
cd services/abp-blazor/etc/docker-prod
cp .env.example .env
chmod 600 .env
# edit .env — passwords, PUBLIC_*_URL, IMAGE_REGISTRY, PFX passphrase

mkdir -p certs data
# copy production openiddict.pfx → certs/openiddict.pfx

docker compose -f docker-compose.prod.yml --env-file .env up -d
# optional extras:
docker compose -f docker-compose.prod.yml --env-file .env --profile full up -d
```

Loopback ports for Nginx:

| Port | Service |
|------|---------|
| 8080 | Blazor |
| 8081 | AuthServer |
| 8082 | WebGateway |
| 8085 | Workflow API (profile `full` only) |
| 15672 | RabbitMQ management (localhost only) |
| 9000/9001 | MinIO (profile `full`, localhost only) |

```bash
sudo cp nginx/hcs.conf.example /etc/nginx/sites-available/hcs.conf
# edit server_name + enable site
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d app.YOUR.DOMAIN -d auth.YOUR.DOMAIN -d gateway.YOUR.DOMAIN
```

## Keycloak

Set `KEYCLOAK_*` in `.env`. Empty `KEYCLOAK_AUTHORITY` / `KEYCLOAK_CLIENT_ID` → AuthServer skips external OIDC (see AuthServer module). Keycloak itself is **not** in this compose — run separately (e.g. Directus lab compose or dedicated stack).

## After first boot

1. Confirm migrations / seed (Identity OpenIddict clients must use production redirect URLs).
2. Change initial admin password.
3. `curl -I https://app.…` / `auth.…` / `gateway.…`
4. Hard-refresh browser after OIDC URL changes.

## Notes / gaps

- Elsa Studio has **no Dockerfile** yet — Studio host not in this compose; Workflow API is on `:8085` for a separately hosted Studio.
- OpenIddict client secrets / redirect URIs are seeded by Identity — update seed or DB for production domains (lab defaults use `localhost`).
- `postgres/init-databases.sql` runs **only** on empty data volume.
- Do not expose Postgres/Redis/RabbitMQ/MinIO on `0.0.0.0`.

# Kubernetes Package QA — 2026-08-08

## Summary

Added a single-command Kubernetes deployment source: generic Docker image builder, Helm chart, `scripts/k8s-up.sh`, runtime-secret template and first/later-run guide in the HCS README.

## Verified

- `bash -n scripts/k8s-up.sh`: pass.
- `dotnet build HCS.slnx --no-restore --disable-build-servers -v minimal`: pass, 0 warnings/errors.
- `dotnet test HCS.slnx --no-restore --no-build --disable-build-servers -v minimal`: 154 passed, 0 failed.
- `./scripts/audit-license-clean.sh`: pass.

## Not executed

- Helm lint/template: Helm is not installed on this workstation.
- Image build or Kind deployment: Docker CLI exists but its daemon is unavailable; Kind is not installed.
- Live Keycloak/MinIO/RabbitMQ/PostgreSQL smoke test: blocked until the cluster, TLS, PFX files, runtime credentials and Keycloak client are supplied.

## Operational prerequisites

- Production requires an ingress controller, two HTTPS DNS hosts, a TLS secret, non-guest RabbitMQ credentials, OpenIddict/DataProtection PFX files and a Keycloak `hcs-free-auth` client.
- A remote cluster also requires `HCS_IMAGE_REGISTRY`; `k8s-up.sh` builds and pushes the versioned deployment images there. `--kind` loads local images instead.

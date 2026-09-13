#!/usr/bin/env bash
# Load .env and export host-process settings for local UI (localhost:44401-44415).
# Usage: source scripts/dev-local-env.sh

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
  echo "Source this file:  source scripts/dev-local-env.sh" >&2
  exit 1
fi

_hcs_root=$(pwd)
if [[ ! -f "$_hcs_root/.env" ]]; then
  _hcs_this="${BASH_SOURCE[0]:-$0}"
  _hcs_root=$(cd "$(dirname "$_hcs_this")/.." && pwd)
fi
if [[ ! -f "$_hcs_root/.env" ]]; then
  echo "Missing $_hcs_root/.env (copy from .env.example)." >&2
  return 1
fi

set -a
# shellcheck disable=SC1091
source "$_hcs_root/.env"
set +a

_port_in_use() {
  lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1
}

if [[ -z "${HCS_POSTGRES_PORT:-}" ]]; then
  if _port_in_use 5432; then
    export HCS_POSTGRES_PORT=5433
  else
    export HCS_POSTGRES_PORT=5432
  fi
fi

_pg="Host=127.0.0.1;Port=${HCS_POSTGRES_PORT};Username=hcs;Password=${HCS_POSTGRES_PASSWORD}"
export ConnectionStrings__Default="${_pg};Database=hcs_identity"
export ConnectionStrings__Organization="${_pg};Database=hcs_organization"
export ConnectionStrings__DocumentService="${_pg};Database=hcs_document"
export ConnectionStrings__WorkManagement="${_pg};Database=hcs_work"
export ConnectionStrings__Collaboration="${_pg};Database=hcs_collaboration"

export DataProtection__Redis="localhost:6379"
export Redis__Configuration="localhost:6379"
export Authentication__Authority="https://localhost:44401"
export Authentication__ClientId="HCS_App"
export Authentication__ClientSecret="${HCS_GATEWAY_CLIENT_SECRET}"
export Authentication__BearerAudience="HCS"
export Authentication__RequireHttpsMetadata="true"
export Authentication__AllowUntrustedBackchannelCertificate="true"
export Authentication__Keycloak__Enabled="false"
export AuthServer__Authority="https://localhost:44401"
export AuthServer__Audience="HCS"
export AuthServer__RequireHttpsMetadata="true"
export AuthServer__AllowUntrustedBackchannelCertificate="true"
export Bff__PublicOrigin="https://localhost:44402"
export Identity__AdminPassword="${HCS_ADMIN_PASSWORD}"
export OpenIddict__Applications__HCS_App__ClientId="HCS_App"
export OpenIddict__Applications__HCS_App__ClientSecret="${HCS_GATEWAY_CLIENT_SECRET}"
export OpenIddict__Applications__HCS_App__RootUrl="https://localhost:44402/"
export OpenIddict__Applications__HCS_App__PostLogoutRootUrl="https://localhost:44403"
export Minio__EndPoint="localhost:9000"
export Minio__AccessKey="${HCS_MINIO_ROOT_USER}"
export Minio__SecretKey="${HCS_MINIO_ROOT_PASSWORD}"
export Minio__WithSSL="false"
export Minio__CreateBucketIfNotExists="true"
export RabbitMQ__Connections__Default__HostName="localhost"
export RabbitMQ__Connections__Default__UserName="${HCS_RABBITMQ_USER}"
export RabbitMQ__Connections__Default__Password="${HCS_RABBITMQ_PASSWORD}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

unset _pg _hcs_root
unset -f _port_in_use

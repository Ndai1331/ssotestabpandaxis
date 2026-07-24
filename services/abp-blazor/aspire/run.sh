#!/usr/bin/env bash
# Start ABP local stack via Aspire AppHost (no ABP Studio required).
# Usage: ./aspire/run.sh [light|full]
set -euo pipefail

PROFILE="${1:-light}"
PROFILE="$(echo "$PROFILE" | tr '[:upper:]' '[:lower:]')"

if [[ "$PROFILE" != "light" && "$PROFILE" != "full" ]]; then
  echo "Usage: $0 [light|full]" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ABP_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DOCKER_DIR="$ABP_ROOT/etc/docker"
APPHOST_PROJ="$SCRIPT_DIR/hanhchinhso.AppHost/hanhchinhso.AppHost.csproj"

ensure_network() {
  docker network inspect hanhchinhso >/dev/null 2>&1 || \
    docker network create hanhchinhso --label=hanhchinhso
}

compose_up() {
  local file="$1"
  docker compose -f "$DOCKER_DIR/containers/$file" up -d
}

ensure_infra_light() {
  echo "[run.sh] Ensuring light infra (postgres, redis, rabbitmq)..."
  ensure_network
  compose_up postgresql.yml
  compose_up redis.yml
  compose_up rabbitmq.yml
}

ensure_infra_full() {
  echo "[run.sh] Ensuring full infra..."
  if command -v pwsh >/dev/null 2>&1; then
    (cd "$DOCKER_DIR" && pwsh -File ./up.ps1)
  else
    echo "[run.sh] pwsh not found — falling back to docker compose (all containers)."
    ensure_network
    for f in elasticsearch.yml grafana.yml kibana.yml prometheus.yml rabbitmq.yml redis.yml ollama.yml pgvector.yml postgresql.yml minio.yml; do
      compose_up "$f"
    done
  fi
}

echo "[run.sh] Profile=$PROFILE"
echo "[run.sh] Reminder: Keycloak (SSO) is separate — cd services/directus-main && docker compose up -d keycloak → http://localhost:5110"

if [[ "$PROFILE" == "full" ]]; then
  ensure_infra_full
else
  ensure_infra_light
fi

export HCS_RUN_PROFILE="$PROFILE"
export ASPIRE_ALLOW_UNSECURED_TRANSPORT="${ASPIRE_ALLOW_UNSECURED_TRANSPORT:-true}"

echo "[run.sh] Starting AppHost (Aspire Dashboard will open)..."
cd "$ABP_ROOT"
exec dotnet run --project "$APPHOST_PROJ" --launch-profile http -- --profile "$PROFILE"

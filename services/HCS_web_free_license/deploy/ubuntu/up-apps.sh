#!/usr/bin/env bash
set -euo pipefail

root_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
env_file="$root_dir/deploy/ubuntu/.env"
compose_file="$root_dir/deploy/ubuntu/docker-compose.apps.yml"

[[ -f "$env_file" ]] || {
  echo "Create $env_file from deploy/ubuntu/.env.example and set every value." >&2
  exit 1
}

# shellcheck disable=SC1090
HCS_DATA_HOST=$(grep -E '^HCS_DATA_HOST=' "$env_file" | tail -n1 | cut -d= -f2- | tr -d '"' | tr -d "'")
: "${HCS_DATA_HOST:?set HCS_DATA_HOST in .env}"

wait_port() {
  local host=$1 port=$2
  echo "Waiting for ${host}:${port} ..."
  for _ in $(seq 1 60); do
    if (echo >/dev/tcp/"$host"/"$port") >/dev/null 2>&1; then
      echo "${host}:${port} is reachable"
      return 0
    fi
    sleep 2
  done
  echo "Timed out waiting for ${host}:${port}" >&2
  return 1
}

wait_port "$HCS_DATA_HOST" 5432
wait_port "$HCS_DATA_HOST" 6379
wait_port "$HCS_DATA_HOST" 5672
wait_port "$HCS_DATA_HOST" 9000

cd "$root_dir"
docker compose --env-file "$env_file" -f "$compose_file" build
docker compose --env-file "$env_file" -f "$compose_file" up -d
docker compose --env-file "$env_file" -f "$compose_file" ps

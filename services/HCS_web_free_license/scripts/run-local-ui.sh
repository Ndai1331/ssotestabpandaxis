#!/usr/bin/env bash
# Local UI loop: infra + Auth/Gateway/services on localhost, Blazor with hot reload.
#   ./scripts/run-local-ui.sh           infra, migrate, backends, then watch Blazor
#   ./scripts/run-local-ui.sh infra     Postgres/Redis/RabbitMQ/MinIO only
#   ./scripts/run-local-ui.sh migrate   infra + DbMigrator seed
#   ./scripts/run-local-ui.sh backend   infra + Auth/Gateway/services
#   ./scripts/run-local-ui.sh stop      stop host processes started by this script
set -euo pipefail

root_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
cd "$root_dir"
# shellcheck disable=SC1091
source "$root_dir/scripts/dev-local-env.sh"

compose=(docker compose --env-file .env -f etc/docker-compose/local-infra.yml)
log_dir="$root_dir/Logs/local-ui"
pid_dir="$log_dir"

port_in_use() {
  lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1
}

wait_port() {
  local port=$1 name=$2
  local i=0
  until port_in_use "$port"; do
    i=$((i + 1))
    if (( i > 180 )); then
      echo "$name did not listen on $port. See $log_dir/$name.log" >&2
      return 1
    fi
    sleep 2
  done
}

start_infra() {
  echo "Starting local infra (Postgres host port ${HCS_POSTGRES_PORT})..."
  "${compose[@]}" up -d postgres redis rabbitmq minio
  echo "Waiting for healthy containers..."
  local svc
  for svc in postgres redis rabbitmq minio; do
    local i=0
    until "${compose[@]}" exec -T "$svc" true >/dev/null 2>&1; do
      i=$((i + 1))
      if (( i > 60 )); then
        echo "Container $svc did not start." >&2
        "${compose[@]}" ps
        exit 1
      fi
      sleep 2
    done
  done
  until "${compose[@]}" exec -T postgres pg_isready -U hcs -d postgres >/dev/null 2>&1; do sleep 2; done
  until "${compose[@]}" exec -T redis redis-cli ping >/dev/null 2>&1; do sleep 2; done
  echo "Infra is up."
}

start_host() {
  local name=$1 port=$2 project=$3
  mkdir -p "$log_dir"
  if port_in_use "$port"; then
    echo "$name already listening on $port"
    return 0
  fi
  echo "Starting $name..."
  python3 - "$root_dir" "$project" "$log_dir/$name.log" "$pid_dir/$name.pid" <<'PY'
import os, sys, subprocess
root, project, log_path, pid_path = sys.argv[1:]
if os.fork() != 0:
    sys.exit(0)
os.setsid()
if os.fork() != 0:
    sys.exit(0)
os.chdir(root)
os.environ.setdefault("DOTNET_CLI_UI_LANGUAGE", "en")
log = open(log_path, "ab", buffering=0)
os.dup2(log.fileno(), 1)
os.dup2(log.fileno(), 2)
os.dup2(os.open("/dev/null", os.O_RDONLY), 0)
proc = subprocess.Popen(["dotnet", "run", "--project", project], cwd=root)
with open(pid_path, "w", encoding="utf-8") as handle:
    handle.write(str(proc.pid))
os._exit(0)
PY
}

stop_hosts() {
  local pidfile name
  if [[ ! -d "$pid_dir" ]]; then
    echo "No local UI host pid files."
    return 0
  fi
  for pidfile in "$pid_dir"/*.pid; do
    [[ -f "$pidfile" ]] || continue
    name=$(basename "$pidfile" .pid)
    if kill -0 "$(cat "$pidfile")" 2>/dev/null; then
      echo "Stopping $name ($(cat "$pidfile"))"
      kill "$(cat "$pidfile")" 2>/dev/null || true
    fi
    rm -f "$pidfile"
  done
}

migrate() {
  echo "Running DbMigrator..."
  (
    cd "$root_dir/src/HCS.DbMigrator"
    dotnet run --no-launch-profile
  )
}

start_backends() {
  mkdir -p "$log_dir"
  start_host auth-server 44401 apps/auth-server/HCS.AuthServer/HCS.AuthServer.csproj
  start_host platform 44411 services/platform/HCS.PlatformService/HCS.PlatformService.csproj
  start_host organization 44412 services/organization/HCS.OrganizationService.Host/HCS.OrganizationService.Host.csproj
  start_host document 44413 services/document/HCS.DocumentService/HCS.DocumentService.csproj
  start_host work-management 44414 services/work-management/HCS.WorkManagementService/HCS.WorkManagementService.csproj
  start_host collaboration 44415 services/collaboration/HCS.CollaborationService/HCS.CollaborationService.csproj
  echo "Waiting for AuthServer..."
  wait_port 44401 auth-server
  start_host web-gateway 44402 gateways/web/HCS.WebGateway/HCS.WebGateway.csproj
  echo "Waiting for Gateway..."
  wait_port 44402 web-gateway
  echo "Backends listening. Logs: $log_dir"
}

cmd=${1:-all}
case "$cmd" in
  stop)
    stop_hosts
    ;;
  infra)
    start_infra
    echo "Next: ./scripts/run-local-ui.sh"
    ;;
  backend)
    start_infra
    start_backends
    ;;
  migrate)
    start_infra
    migrate
    ;;
  all)
    start_infra
    migrate
    start_backends
    echo
    echo "Blazor UI: https://localhost:44403  (hot reload)"
    echo "Gateway:   https://localhost:44402"
    echo "Auth:      https://localhost:44401"
    echo "Admin user is the Identity seed from .env (Identity__AdminPassword)."
    echo "Stop hosts: ./scripts/run-local-ui.sh stop"
    echo
    mkdir -p "$log_dir"
    exec dotnet watch --project src/HCS.Blazor/HCS.Blazor.csproj
    ;;
  *)
    echo "Usage: $0 [all|infra|migrate|backend|stop]" >&2
    exit 1
    ;;
esac

#!/usr/bin/env bash
# Start Task9 local dev servers (UI + API) regardless of whether the current
# directory is the workspace root or an app-created git worktree.
#
# Why this script exists instead of preview_start:
#   The workspace is a git-submodule monorepo. App-created worktrees of the
#   META repo do NOT check out submodule code (services/ui, services/api are
#   empty there), so preview_start — which requires a cwd relative to the
#   worktree root — cannot host the .NET apps. This script resolves the REAL
#   workspace root (where the submodules actually live) and launches from there.
#
# Usage:
#   bash start-local.sh            # start UI(:5053) + API(:7093) if not already up
#   bash start-local.sh --restart  # kill existing listeners first, then start
#   bash start-local.sh ui         # start only UI
#   bash start-local.sh api        # start only API
set -uo pipefail

RESTART=0
WANT="all"
for a in "$@"; do
  case "$a" in
    --restart) RESTART=1 ;;
    ui|api|all) WANT="$a" ;;
  esac
done

# ── Resolve the real workspace root (the dir that actually contains submodules) ──
resolve_root() {
  local d="$PWD"
  # App worktrees live under <root>/.claude/worktrees/<name>
  case "$d" in
    */.claude/worktrees/*) echo "${d%%/.claude/worktrees/*}"; return ;;
  esac
  # Otherwise walk up until we find populated submodules
  while [ "$d" != "/" ]; do
    if [ -d "$d/services/ui/src" ]; then echo "$d"; return; fi
    d="$(dirname "$d")"
  done
  echo ""  # not found
}

ROOT="$(resolve_root)"
if [ -z "$ROOT" ] || [ ! -d "$ROOT/services/ui/src" ]; then
  echo "❌ Could not resolve workspace root with checked-out submodules (services/ui/src)." >&2
  echo "   Run this from inside the task9-workspace tree." >&2
  exit 1
fi
echo "📂 Workspace root: $ROOT"

# ── If we're inside an app worktree, link empty submodule placeholders to the
#    real code so preview_start (relative cwd within the worktree) also works ──
if [ "$PWD" != "$ROOT" ]; then
  for sm in ui api agent; do
    here="$PWD/services/$sm"
    if [ -d "$here" ] && [ ! -L "$here" ] && [ -z "$(ls -A "$here" 2>/dev/null)" ]; then
      rmdir "$here" 2>/dev/null && ln -s "$ROOT/services/$sm" "$here" \
        && echo "🔗 linked worktree services/$sm → real code"
    fi
  done
fi

UI_DIR="$ROOT/services/ui/src/BootstrapBlazor.Server"
API_DIR="$ROOT/services/api/WebApi"
LOG_DIR="${TMPDIR:-/tmp}/task9-local"
mkdir -p "$LOG_DIR"

# ── Pull the TEST DB connection string from the API's own committed config ──
# (kept only where it already lives — never hardcode the secret in this skill)
APP_DEV="$API_DIR/appsettings.Development.json"
TEST_CONN="$(grep 'qcadmin_test' "$APP_DEV" 2>/dev/null | grep -i 'MyApp' | grep -oE '"Server=[^"]+"' | tr -d '"' | head -1)"

is_up() { curl -s -o /dev/null -w "%{http_code}" "$1" 2>/dev/null | grep -qE "200|301|302|404"; }

free_port() {
  local port="$1"
  local pids
  pids="$(lsof -ti tcp:"$port" 2>/dev/null)"
  if [ -n "$pids" ]; then
    echo "   ↳ killing existing listener(s) on :$port ($pids)"
    kill $pids 2>/dev/null; sleep 2
    pids="$(lsof -ti tcp:"$port" 2>/dev/null)"
    [ -n "$pids" ] && kill -9 $pids 2>/dev/null
  fi
}

start_api() {
  if [ "$RESTART" -eq 1 ]; then free_port 7093; fi
  if is_up "http://localhost:7093/"; then echo "✅ API already up on :7093"; return; fi
  if [ -z "$TEST_CONN" ]; then
    echo "⚠️  API: could not find qcadmin_test connection in $APP_DEV." >&2
    echo "    Refusing to start API against PROD default (qcadmin). Fix config or start API manually." >&2
    return 1
  fi
  echo "🚀 Starting API on :7093 → test DB qcadmin_test"
  ( cd "$API_DIR" && \
    ASPNETCORE_ENVIRONMENT=Development \
    ConnectionStrings__MyApp="$TEST_CONN" \
    ConnectionStrings__MyAppDbName="qcadmin_test" \
    nohup dotnet run --urls "http://localhost:7093" >"$LOG_DIR/api.log" 2>&1 & )
}

start_ui() {
  if [ "$RESTART" -eq 1 ]; then free_port 5053; fi
  if is_up "http://localhost:5053/"; then echo "✅ UI already up on :5053"; return; fi
  echo "🚀 Starting UI on :5053"
  ( cd "$UI_DIR" && \
    ASPNETCORE_ENVIRONMENT=Development \
    nohup dotnet run --urls "http://localhost:5053" --launch-profile console >"$LOG_DIR/ui.log" 2>&1 & )
}

[ "$WANT" = "all" ] || [ "$WANT" = "api" ] && start_api
[ "$WANT" = "all" ] || [ "$WANT" = "ui" ]  && start_ui

# ── Wait for health (first build can take 1-2 min) ──
wait_up() {
  local url="$1" name="$2" tries=60
  printf "⏳ Waiting for %s" "$name"
  while [ $tries -gt 0 ]; do
    if is_up "$url"; then echo " — ready"; return 0; fi
    printf "."; sleep 3; tries=$((tries-1))
  done
  echo " — TIMEOUT (check $LOG_DIR)"; return 1
}

[ "$WANT" = "all" ] || [ "$WANT" = "api" ] && wait_up "http://localhost:7093/" "API :7093"
[ "$WANT" = "all" ] || [ "$WANT" = "ui" ]  && wait_up "http://localhost:5053/" "UI :5053"

echo ""
echo "────────────────────────────────────────────"
echo " UI   → http://localhost:5053"
echo " API  → http://localhost:7093  (DB: qcadmin_test)"
echo " Login: testAdmin / testAdmin@12345"
echo " Logs : $LOG_DIR/{ui,api}.log"
echo "────────────────────────────────────────────"

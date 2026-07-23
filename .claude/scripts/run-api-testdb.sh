#!/usr/bin/env bash
# Launch the Task9 API for preview, forcing the TEST database (qcadmin_test).
#
# Used as the runtimeExecutable wrapper in generated worktree launch.json
# configs: preview_start cannot inject env vars, and the API's default
# Development config falls back to PROD qcadmin — which must never happen
# from an agent session. The connection string is read at runtime from the
# API's own committed appsettings.Development.json (never hardcoded here).
#
# Usage: run-api-testdb.sh <port>   (cwd must be the WebApi project dir)
set -euo pipefail

PORT="${1:?usage: run-api-testdb.sh <port>}"

APP_DEV="$PWD/appsettings.Development.json"
if [ ! -f "$APP_DEV" ]; then
  echo "❌ $APP_DEV not found — cwd must be services/api/WebApi" >&2
  exit 1
fi

TEST_CONN="$(grep 'qcadmin_test' "$APP_DEV" | grep -i 'MyApp' | grep -oE '"Server=[^"]+"' | tr -d '"' | head -1)"
if [ -z "$TEST_CONN" ]; then
  echo "❌ No qcadmin_test connection string in appsettings.Development.json." >&2
  echo "   Refusing to start API against the PROD default (qcadmin)." >&2
  exit 1
fi

# Preview's spawn env may not include homebrew in PATH — resolve dotnet explicitly.
DOTNET="$(command -v dotnet || true)"
for c in /opt/homebrew/opt/dotnet@9/bin/dotnet /opt/homebrew/bin/dotnet /usr/local/bin/dotnet; do
  [ -z "$DOTNET" ] && [ -x "$c" ] && DOTNET="$c"
done
if [ -z "$DOTNET" ]; then
  echo "❌ dotnet not found" >&2
  exit 1
fi

echo "🚀 API on :$PORT → DB qcadmin_test"
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__MyApp="$TEST_CONN"
export ConnectionStrings__MyAppDbName="qcadmin_test"
exec "$DOTNET" run --urls "http://localhost:$PORT"

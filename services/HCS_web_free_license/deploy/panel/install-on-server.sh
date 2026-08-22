#!/usr/bin/env bash
# Sync HCS panel bundle to Baota compose directory on a remote server.
# Usage: ./deploy/panel/install-on-server.sh [user@host]
set -euo pipefail

remote=${1:?Usage: $0 user@host}
panel_root=/www/server/panel/data/compose

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

echo "Installing to ${remote}:${panel_root}/hcs-data and .../hcs"

ssh "$remote" "mkdir -p ${panel_root}/hcs-data/postgres ${panel_root}/hcs/config"

rsync -av --delete \
  --exclude '.env' \
  "$script_dir/hcs-data/" "${remote}:${panel_root}/hcs-data/"

rsync -av --delete \
  --exclude '.env' \
  "$script_dir/hcs/" "${remote}:${panel_root}/hcs/"

ssh "$remote" "chmod +x ${panel_root}/hcs-data/up.sh ${panel_root}/hcs/up.sh"

echo ""
echo "Done. On server:"
echo "  cp ${panel_root}/hcs-data/.env.example ${panel_root}/hcs-data/.env"
echo "  cp ${panel_root}/hcs/.env.example ${panel_root}/hcs/.env"
echo "  # edit both .env files, then:"
echo "  ${panel_root}/hcs-data/up.sh"
echo "  ${panel_root}/hcs/up.sh"
echo ""
echo "See deploy/panel/README.md for Nginx, Keycloak bootstrap, and SSL."

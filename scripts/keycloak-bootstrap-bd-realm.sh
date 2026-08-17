#!/usr/bin/env bash
# Wrapper: bootstrap Keycloak realm bd
set -euo pipefail
exec python3 "$(dirname "$0")/keycloak_bootstrap_bd_realm.py" "$@"

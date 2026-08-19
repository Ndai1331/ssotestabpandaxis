# Phase 3 — Rebuild and validate the Docker login flow

## Overview

Priority: P1. Confirm the compiled WASM/client reaches the public Caddy URL and consumes the live BFF session.

## Steps

1. Run the license/secret audit, restore/build, and targeted client/Gateway tests from `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license`.
2. Rebuild the Compose images, then recreate at minimum `web-gateway`, `blazor`, and `caddy` with the normal `scripts/docker-up.sh` flow (or its documented equivalent); wait for healthy/running status and inspect recent logs for authentication/data-protection errors.
3. In a new incognito window, visit `https://hcs.localhost`, login once with `admin`, and confirm one callback reaches `/` with left navigation and authenticated header state.
4. Hard-refresh and confirm `/bff/user` remains `200` with the same visible UI. Log out and confirm anonymous UI/login redirect; reopen a protected deep link and confirm the safe BFF login return path.
5. Record redacted evidence: container image/restart time, `docker compose ps`, relevant Gateway/Blazor log window, browser network status for `/bff/user`, and test command results. Never record cookies, passwords, client secrets, or tokens.

## Success criteria

- The first login no longer bounces to a contradictory anonymous page.
- Repeated login is unnecessary; a single callback establishes the UI state.
- Refresh and logout do not leave stale authorized navigation.

## Rollback

If provider integration prevents app render, redeploy the preceding `blazor` image/configuration; leave Gateway cookies, AuthServer, Keycloak, and database unchanged because this fix makes no migration or identity-provider change.

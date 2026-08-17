---
phase: 3
title: "Smoke SSO + app gate"
status: completed
effort: 1h
dependsOn: [2]
---

# Phase 03 — Smoke SSO + app gate

## Context

- Lab users (bootstrap): `*@benhvien.vn` / `Passw0rd!` with role + app groups
- Gate: extension rejects missing `bd-app-axis`
- Callback: `http://localhost:8055/auth/login/keycloak/callback`
- ABP unchanged — optional quick check same KC still serves `abp-auth`

## Overview

Verify OpenID login maps roles; verify app gate deny; confirm local admin still works; note any UX/log issues.

## Test matrix

| Case | Expect |
|------|--------|
| Local admin `admin@local.dev` | OK Studio |
| `admin@benhvien.vn` (bd-admin + bd-app-axis) | Studio role Admin |
| `bacsi@` / `lanhdao@` / `nhanvien@` | Correct mapped roles |
| User with role group but **remove** `bd-app-axis` in KC | Login **denied** (hook error) |
| User with `bd-app-axis` only (no role group) | Default NhanVien |

## Implementation steps

1. Hard-refresh Studio login (`Ctrl+Shift+R`).
2. Click SSO / Keycloak → login each lab user → assert role in Studio/settings.
3. KC Admin: remove `bd-app-axis` from one test user → retry login → expect deny; restore group after.
4. Check Directus logs for `[BD] Keycloak login denied` on deny case.
5. Optional: hit ABP login once — same KC session/prompt behavior (no regression expected if KC unchanged).
6. Document any failure + root cause in phase notes or journal (do not silent-skip).

## Todo

- [x] 4 role mappings OK
- [x] Gate deny OK
- [x] Local admin OK
- [x] Callback URI aligned (no redirect_uri mismatch)

## Success criteria

- [x] All matrix rows pass (or documented waiver with reason)
- [x] No license / SSO entitlement errors in logs (v11 must not mention license SSO lock)

## Risks

| Risk | Mitigation |
|------|------------|
| Hook not loaded | Confirm volume mount + extension folder structure; restart Directus |
| Groups claim missing | Re-run bootstrap; check client mapper |
| `prompt=login` UX | Expected for lab; not a failure |
| Flat `groups.` keys | Extension already handles flatten — if fail, inspect userInfo in log |

## Next

Phase 04 — docs SoT + archive v12 pointers.

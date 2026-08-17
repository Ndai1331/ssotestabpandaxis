# Brainstorm — Directus v11 SSO Keycloak (replace v12 lab)

**Date:** 2026-07-25  
**Status:** Approved  
**Project:** BD SSO Lab (local)

---

## Problem

Lab đang dùng `services/directus-main` (**v12.1.1**, MSCL + license gate SSO). User muốn chuyển sang `services/directus-main-v11` (**11.13.4**, không runtime license SSO) và giữ parity Keycloak SSO + app gate với ABP.

## Requirements (user approved)

| # | Quyết định | Giá trị |
|---|------------|---------|
| D1 | Lab Directus SoT | **Thay v12 bằng v11** (`directus-main-v11`) |
| D2 | App gate `bd-app-axis` | **Bắt buộc** parity ABP (`bd-app-hcs`) |
| D3 | Runtime | **Docker compose lab** (PG + Redis + KC + Axis) |

## Feasibility

| Check | v11 result |
|-------|------------|
| OpenID driver built-in | Yes — `api/src/auth/drivers/openid.ts` |
| Env `AUTH_*` / ROLE_MAPPING / GROUP_CLAIM | Same pattern as v12 lab |
| License `sso_enabled` gate | **Không có** — không cần `BD_LAB_ALLOW_SSO` |
| Hook `auth.create` / `auth.update` | Có — gate extension port được |
| Extension host | `directus-extension-bd-app-gate` đã `host: ^11.0.0` |
| Rename `axis_*` | Đã có trên cả hai fork |
| Keycloak trong compose | Có `:5110` (debug compose); cần port `docker-compose.bd-lab.yml` |

**Verdict:** SSO Keycloak trên v11 **khả thi và đơn giản hơn v12** (bỏ license bypass).

## Approaches evaluated

| | Approach | Verdict |
|--|----------|---------|
| A | Config-only (env + KC reuse) | Đủ login; **thiếu** gate + compose lab |
| B | Parity lab (compose + gate + env) | Cần cho D2+D3 |
| C | B + đổi SoT docs sang v11, archive v12 | **Chọn** — khớp D1 |

## Final design

```
Browser → Axis Studio :8055
              │ OpenID client "directus"
              ▼
         Keycloak :5110 /realms/bd
              │ claim groups
              ▼
    OpenID driver (v11 built-in)
    + hook bd-app-gate → require bd-app-axis
    + ROLE_MAPPING → axis_roles UUID
```

### Port từ v12 → v11

| Artifact | Action |
|----------|--------|
| `docker-compose.bd-lab.yml` | Copy; drop `BD_LAB_ALLOW_SSO`; volumes `bd_axis_v11_*`; image `bd-axis-v11:local` |
| `bd-lab-extensions/directus-extension-bd-app-gate/` | Copy as-is |
| `.env.sso.example` | Copy; no license bypass |
| `scripts/keycloak_bootstrap_bd_realm.py` | Reuse (client/secret/groups unchanged) |
| Role UUID trong compose | **Không** copy từ v12 — seed sau first boot |
| `api/src/license/*` / `BD_LAB_ALLOW_SSO` | **Không** mang sang |

### Docs SoT update

- `docs/runbooks/local-sso-lab.md`
- `docs/workspace-architecture.md`
- `docs/handoff/phase1-sso-context.md`
- `CLAUDE.md` / `AGENTS.md` / wiki hot + cheatsheet
- v12 path: giữ repo, đánh dấu **archive / không lab**

### Out of scope

- Zimbra LDAP
- Đổi ABP clients/secrets
- Implement trong session brainstorm (hard-gate)

## Risks

| Risk | Mitigation |
|------|------------|
| Docker build v11 lâu/fail | Verify Dockerfile; build trước smoke |
| UUID roles lệch | Doc bước paste UUID sau first boot |
| Port conflict stack v12 | `compose down` v12; volume tên riêng |
| Comment stale `bd-app-directus` | Fix → `bd-app-axis` khi port |

## Success metrics

- [ ] Compose lab up từ `services/directus-main-v11`
- [ ] KC bootstrap → login `*@benhvien.vn` đúng role
- [ ] Thiếu `bd-app-axis` → deny
- [ ] ABP cùng KC không đổi
- [ ] Runbook/docs chỉ path v11

## Next steps

1. User approve → **done** (2026-07-25)
2. Hỏi `/ck:plan` để phase implement
3. Cook theo plan (compose + gate + docs SoT)

## References

- Prior SSO design: `plans/reports/brainstorm-260723-1415-bd-sso-login-flow.md`
- Phase OpenID: `plans/260723-1419-bd-sso-phase1/phase-02-directus-openid.md`
- Runbook: `docs/runbooks/local-sso-lab.md`
- v12 compose: `services/directus-main/docker-compose.bd-lab.yml`
- Gate: `services/directus-main/bd-lab-extensions/directus-extension-bd-app-gate/`
